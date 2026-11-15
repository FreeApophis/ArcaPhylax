using System.Collections.Immutable;
using System.Reflection;
using ArcaPhylax.Abstractions;
using ArcaPhylax.PluginPackaging;
using Autofac;
using Autofac.Features.Metadata;

namespace ArcaPhylax.PluginSystem;

public sealed class PluginManager
{
    private readonly ILifetimeScope _hostLifeTimeScope;
    private readonly PluginStore _store;
    private readonly HashSet<string> _sharedAssemblyNames;
    private readonly List<LoadedPlugin> _loadedPlugins = [];
    private readonly List<PluginLoadFailure> _failures = [];

    public PluginManager(ILifetimeScope hostLifetimeScope, PluginStore store)
    {
        _hostLifeTimeScope = hostLifetimeScope;
        _store = store;

        _sharedAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            typeof(IPlugin).Assembly.GetName().Name!,          // Shared Abstractions
            typeof(ContainerBuilder).Assembly.GetName().Name!, // Autofac
        };
    }

    /// <summary>Raised (on the caller's thread) when a plugin is loaded while the app runs.</summary>
    public event EventHandler<LoadedPlugin>? PluginLoaded;

    public IEnumerable<ITabContentViewModel> AllTabs => FindAll<ITabContentViewModel>();

    public IEnumerable<IConfigurationSection> AllConfigurationSections => FindAll<IConfigurationSection>();

    public IReadOnlyList<LoadedPlugin> LoadedPlugins => _loadedPlugins;

    public IReadOnlyList<PluginLoadFailure> Failures => _failures;

    public PluginStore Store => _store;

    /// <summary>The tabs contributed by one plugin (not the host tabs its scope can also see).</summary>
    public static IEnumerable<ITabContentViewModel> TabsOf(LoadedPlugin plugin) => ContributionsOf<ITabContentViewModel>(plugin);

    public async Task<LoadedPlugin> LoadAsync(string pluginMainAssemblyPath, PluginOrigin origin, CancellationToken cancellationToken = default)
    {
        string fullPath = Path.GetFullPath(pluginMainAssemblyPath);

        var loadContext = new PluginLoadContext(fullPath, _sharedAssemblyNames);
        Assembly pluginAssembly = loadContext.LoadFromAssemblyPath(fullPath);

        Type pluginType = pluginAssembly.GetTypes().Single(t => typeof(IPlugin).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false });

        // Plugin-Instanz zuerst ohne DI erzeugen, nur um registrieren zu können.
        var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;

        ILifetimeScope scope = _hostLifeTimeScope.BeginLifetimeScope(
            $"plugin:{pluginType.FullName}",
            builder =>
            {
                plugin.Configure(builder);
                builder.RegisterInstance(plugin).As<IPlugin>().SingleInstance();
            });

        await plugin.StartAsync(cancellationToken);

        return new LoadedPlugin(pluginType.Name, origin, loadContext, scope, plugin);
    }

    /// <summary>
    /// Installs a <c>.arcaplugin</c> file. A plugin that is new to this app is loaded right away; one
    /// that replaces a loaded plugin (update, or an installed copy of a bundled plugin) waits for a restart.
    /// </summary>
    public Task<InstallReport> InstallFromFileAsync(string packagePath, CancellationToken cancellationToken = default)
        => InstallAsync(() => File.OpenRead(packagePath), cancellationToken);

    /// <summary>Installs a downloaded package; same rules as <see cref="InstallFromFileAsync"/>.</summary>
    public Task<InstallReport> InstallAsync(byte[] package, CancellationToken cancellationToken = default)
        => InstallAsync(() => new MemoryStream(package, writable: false), cancellationToken);

    /// <summary>Schedules an installed plugin for removal at the next start.</summary>
    public Either<InstallError, Unit> ScheduleRemoval(string pluginId) => _store.ScheduleRemoval(pluginId);

    public static async Task<bool> UnloadAsync(LoadedPlugin plugin)
    {
        WeakReference weakRef = plugin.LoadContextWeakReference;

        // Unloading a plugin is exactly this method's job, so disposing the passed-in plugin is the
        // intended contract rather than an accidental dispose of an injected dependency.
#pragma warning disable IDISP007 // Don't dispose injected
        await plugin.DisposeAsync();
#pragma warning restore IDISP007 // Don't dispose injected

        for (int i = 0; weakRef.IsAlive && i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Delay(50);
        }

        return !weakRef.IsAlive;
    }

    public object Resolve(Type serviceType)
    {
        if (_hostLifeTimeScope.TryResolve(serviceType, out var hostInstance))
        {
            return hostInstance;
        }

        foreach (var loadedPlugin in _loadedPlugins)
        {
            if (loadedPlugin.Scope.TryResolve(serviceType, out var pluginInstance))
            {
                return pluginInstance;
            }
        }

        throw new InvalidOperationException($"Neither the host nor any loaded plugin can resolve {serviceType.FullName}.");
    }

    /// <summary>Applies staged store changes, then loads installed and bundled plugins. Call once at startup.</summary>
    internal async Task LoadAllPluginsAsync()
    {
        _store.ApplyPending();

        var results = await Task.WhenAll(StartupCandidates(_store.Installed()).Select(candidate => TryLoadAsync(candidate)));

        _loadedPlugins.AddRange(results.WhereSelect(result => result.Match(left: _ => Option<LoadedPlugin>.None, right: Option.Some)));
        _failures.AddRange(results.WhereSelect(result => result.Match(left: Option.Some, right: _ => Option<PluginLoadFailure>.None)));
    }

    // Yields every contribution of <typeparamref name="T"/> from the host scope plus each plugin scope.
    private IEnumerable<T> FindAll<T>()
        where T : notnull
        => _hostLifeTimeScope.Resolve<IEnumerable<T>>()
            .Concat(_loadedPlugins.SelectMany(ContributionsOf<T>));

    // Plugin scopes are children of the host scope, so they also see host registrations; the "Host"
    // metadata marks those so they are not yielded twice.
    private static IEnumerable<T> ContributionsOf<T>(LoadedPlugin plugin)
        where T : notnull
        => plugin.Scope.Resolve<IEnumerable<Meta<T>>>()
            .Where(meta => !(meta.Metadata.TryGetValue("Source", out var source) && source is "Host"))
            .Select(meta => meta.Value);

    /// <summary>Installed plugins first; a bundled plugin is skipped when an installed one replaces it.</summary>
    private static ImmutableList<PluginCandidate> StartupCandidates(ImmutableList<InstalledPlugin> installed)
        => installed
            .Select(plugin => new PluginCandidate(plugin.Manifest.Name, plugin.EntryAssemblyPath, new FromStore(plugin)))
            .Concat(BundledCandidates().Where(bundled => !installed.Any(plugin => IsSameAssembly(plugin, bundled.EntryAssemblyPath))))
            .ToImmutableList();

    private static IEnumerable<PluginCandidate> BundledCandidates()
        => Directory.Exists(PluginHost.BundledRoot)
            ? Directory.GetDirectories(PluginHost.BundledRoot).WhereSelect(BundledCandidate)
            : [];

    private static Option<PluginCandidate> BundledCandidate(string directory)
    {
        var name = new DirectoryInfo(directory).Name;
        var entryAssembly = Path.Combine(directory, $"{name}.dll");
        return File.Exists(entryAssembly)
            ? new PluginCandidate(name, entryAssembly, new Bundled(directory))
            : Option<PluginCandidate>.None;
    }

    private static bool IsSameAssembly(InstalledPlugin plugin, string assemblyPath)
        => string.Equals(plugin.Manifest.EntryAssembly, Path.GetFileName(assemblyPath), StringComparison.OrdinalIgnoreCase);

    private async Task<InstallReport> InstallAsync(Func<Stream> openPackage, CancellationToken cancellationToken)
    {
        var outcome = await Task.Run(() => InstalledFrom(openPackage), cancellationToken);

        return await outcome.Match(
            left: error => Task.FromResult<InstallReport>(new InstallRejected(error)),
            right: installed => Activated(installed, cancellationToken));
    }

    private Either<InstallError, InstallOutcome> InstalledFrom(Func<Stream> openPackage)
    {
        using var package = openPackage();
        return _store.Install(package);
    }

    private async Task<InstallReport> Activated(InstallOutcome outcome, CancellationToken cancellationToken)
        => outcome switch
        {
            UpdateStaged staged => new RestartRequired(staged.Plugin.Manifest),
            InstalledNow now when IsLoaded(now.Plugin) => new RestartRequired(now.Plugin.Manifest),
            InstalledNow now => await LoadedLive(now.Plugin, cancellationToken),
        };

    private bool IsLoaded(InstalledPlugin plugin)
        => _loadedPlugins.Any(loaded => loaded.Origin switch
        {
            Bundled bundled => IsSameAssembly(plugin, Path.Combine(bundled.Directory, plugin.Manifest.EntryAssembly)),
            FromStore store => store.Plugin.Manifest.Id == plugin.Manifest.Id,
        });

    private async Task<InstallReport> LoadedLive(InstalledPlugin plugin, CancellationToken cancellationToken)
    {
        var result = await TryLoadAsync(new PluginCandidate(plugin.Manifest.Name, plugin.EntryAssemblyPath, new FromStore(plugin)), cancellationToken);

        return result.Match<InstallReport>(
            left: failure => new LoadFailed(plugin.Manifest, failure.Message),
            right: loaded =>
            {
                _loadedPlugins.Add(loaded);
                PluginLoaded?.Invoke(this, loaded);
                return new PluginActivated(plugin.Manifest);
            });
    }

    private async Task<Either<PluginLoadFailure, LoadedPlugin>> TryLoadAsync(PluginCandidate candidate, CancellationToken cancellationToken = default)
    {
        // A plugin is third-party code: whatever it throws while loading must not take the host down.
        try
        {
            return Either<PluginLoadFailure, LoadedPlugin>.Right(await LoadAsync(candidate.EntryAssemblyPath, candidate.Origin, cancellationToken));
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Failed to load plugin '{candidate.Name}': {exception}");
            return Either<PluginLoadFailure, LoadedPlugin>.Left(new PluginLoadFailure(candidate.Name, candidate.Origin, exception.Message));
        }
    }

    private sealed record PluginCandidate(string Name, string EntryAssemblyPath, PluginOrigin Origin);
}

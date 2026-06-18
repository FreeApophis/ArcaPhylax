using System.Reflection;
using ArcaPhylax.Abstractions;
using Autofac;
using Autofac.Features.Metadata;
using Funcky.Extensions;
using Funcky.Monads;
using static Funcky.Functional;

namespace ArcaPhylax.PluginSystem;

public sealed class PluginManager
{
    private readonly ILifetimeScope _hostLifeTimeScope;
    private readonly HashSet<string> _sharedAssemblyNames;
    private readonly List<LoadedPlugin> _loadedPlugins = [];
    private readonly Func<string, Option<string>> _toPluginDllComposition = Fn(ToPluginDllOrNone).Compose(Fn(PluginDllFromName)).Compose(Fn(PluginName));

    public PluginManager(ILifetimeScope hostLifetimeScope)
    {
        _hostLifeTimeScope = hostLifetimeScope;

        _sharedAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            typeof(IPlugin).Assembly.GetName().Name!,          // Shared Abstractions
            typeof(ContainerBuilder).Assembly.GetName().Name!, // Autofac
        };
    }

    public IEnumerable<ITabContentViewModel> AllTabs => FindAllTab();

    public IEnumerable<IConfigurationSection> AllConfigurationSections => FindAll<IConfigurationSection>();

    public async Task<LoadedPlugin> LoadAsync(string pluginMainAssemblyPath, CancellationToken cancellationToken = default)
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

        return new LoadedPlugin(pluginType.Name, loadContext, scope, plugin);
    }

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

        throw new Exception("yeah thats bad... really really bad");
    }

    internal async Task LoadAllPluginsAsync()
    {
        _loadedPlugins.AddRange(await Task.WhenAll(LoadPlugins()));
    }

    private IEnumerable<ITabContentViewModel> FindAllTab() => FindAll<ITabContentViewModel>();

    // Yields every contribution of <typeparamref name="T"/> from the host scope plus each plugin
    // scope. Plugin scopes are children of the host scope, so they also see host registrations;
    // the "Host" metadata marks those so they are not yielded twice.
    private IEnumerable<T> FindAll<T>()
        where T : notnull
    {
        foreach (var contribution in _hostLifeTimeScope.Resolve<IEnumerable<T>>())
        {
            yield return contribution;
        }

        foreach (var loadedPlugin in _loadedPlugins)
        {
            foreach (var meta in loadedPlugin.Scope.Resolve<IEnumerable<Meta<T>>>())
            {
                if (!meta.Metadata.TryGetValue("Source", out var source) || source is not "Host")
                {
                    yield return meta.Value;
                }
            }
        }
    }

    private IEnumerable<Task<LoadedPlugin>> LoadPlugins()
        => Directory.GetDirectories(GetPluginsPath())
            .WhereSelect(_toPluginDllComposition)
            .Select(pluginDll => LoadAsync(pluginDll));

    private static string GetPluginsPath()
        => Path.Combine(AppContext.BaseDirectory, "plugins");

    private static Option<string> ToPluginDllOrNone(string pluginDll)
        => Option.FromBoolean(File.Exists(pluginDll), pluginDll);

    private static string PluginDllFromName(string pluginName)
        => Path.Combine(GetPluginsPath(), pluginName, $"{pluginName}.dll");

    private static string PluginName(string dir)
        => new DirectoryInfo(dir).Name;
}
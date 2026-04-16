using ArcaPhylax.Abstractions;
using Autofac;
using Autofac.Core.Activators;
using Autofac.Features.Metadata;
using Funcky.Extensions;
using Funcky.Monads;
using HarfBuzzSharp;
using System.Reflection;
using static Funcky.Functional;

namespace ArcaPhylax.PluginSystem;

public sealed class PluginManager
{
    private readonly ILifetimeScope _hostLifeTimeScope;
    private readonly HashSet<string> _sharedAssemblyNames;
    private readonly List<LoadedPlugin> _loadedPlugins = new();
    private readonly Func<string, Option<string>> _toPluginDllComposition = Fn(ToPluginDllOrNone).Compose(Fn(PluginDllFromName)).Compose(Fn(PluginName));

    public IEnumerable<ITabContentViewModel> AllTabs => FindAllTab();

    private IEnumerable<ITabContentViewModel> FindAllTab()
    {
        foreach (var tab in _hostLifeTimeScope.Resolve<IEnumerable<ITabContentViewModel>>())
        {
            yield return tab;
        }

        foreach (var loadedPlugin in _loadedPlugins)
        {
            foreach (var meta in loadedPlugin.Scope.Resolve<IEnumerable<Meta<ITabContentViewModel>>>())
            {
                if (!meta.Metadata.TryGetValue("Source", out var x) || x is not "Host")
                {
                    yield return meta.Value;
                }
            }
        }
    }

    public PluginManager(ILifetimeScope hostLifetimeScope)
    {
        _hostLifeTimeScope = hostLifetimeScope;

        _sharedAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            typeof(IPlugin).Assembly.GetName().Name!,          // Shared Abstractions
            typeof(ContainerBuilder).Assembly.GetName().Name! // Autofac
        };
    }

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

        await plugin.DisposeAsync();

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
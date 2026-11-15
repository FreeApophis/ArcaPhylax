using System.Reflection;
using System.Runtime.Loader;

namespace ArcaPhylax.PluginSystem;

public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly HashSet<string> _sharedAssemblyNames;

    public PluginLoadContext(
        string pluginMainAssemblyPath,
        IEnumerable<string> sharedAssemblyNames)
        : base(
            name: Path.GetFileNameWithoutExtension(pluginMainAssemblyPath),
            isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginMainAssemblyPath);
        _sharedAssemblyNames = new HashSet<string>(
            sharedAssemblyNames,
            StringComparer.OrdinalIgnoreCase);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Shared Assemblies sollen aus dem Default Context kommen.
        if (IsShared(assemblyName.Name))
        {
            return null;
        }

        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath is not null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath is not null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return nint.Zero;
    }

    private bool IsShared(string? assemblyName)
        => assemblyName is not null
            && (_sharedAssemblyNames.Contains(assemblyName)
            || assemblyName == "ArcaPhylax.Abstractions"
            || assemblyName == "ArcaPhylax.Plugin.Sdk"
            || assemblyName == "CommunityToolkit.Mvvm" // CommunityToolkit.Mvvm MUST be shared: its source-generated setters bind to whichever ObservableObject the plugin compiled against. If the plugin loaded its own copy, a ViewModelBase-derived VM using [ObservableProperty] would hit MethodAccessException across the ALC boundary. One shared copy gives a single ObservableObject identity.
            || assemblyName == "Autofac"
            || assemblyName.StartsWith("Avalonia", StringComparison.OrdinalIgnoreCase));
}
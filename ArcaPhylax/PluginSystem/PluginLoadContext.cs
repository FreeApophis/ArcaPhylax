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

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath is not null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }

    private bool IsShared(string? assemblyName)
        => assemblyName is not null
            && (_sharedAssemblyNames.Contains(assemblyName)
            || assemblyName == "ArcaPhylax.Abstractions"
            || assemblyName == "Autofac"
            || assemblyName.StartsWith("Avalonia", StringComparison.OrdinalIgnoreCase));
}
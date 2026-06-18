using System.Reflection;
using System.Runtime.Loader;

namespace ArcaPhylax.Composition;

internal class PluginLoadContext(string pluginPath) : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver = new(pluginPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = assemblyName.Name is "NAPS2.Sdk"
            ? "C:\\_Repositories\\ArcaPhylax\\Plugins\\ScannerPlugin\\bin\\Release\\net10.0\\publish\\NAPS2.Sdk.dll"
            : _resolver.ResolveAssemblyToPath(assemblyName);

        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}

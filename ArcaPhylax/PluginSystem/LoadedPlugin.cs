using ArcaPhylax.Abstractions;
using Autofac;

namespace ArcaPhylax.PluginSystem;

public sealed class LoadedPlugin(string name, PluginOrigin origin, PluginLoadContext loadContext, ILifetimeScope scope, IPlugin instance) : IAsyncDisposable
{
    public string Name { get; } = name;

    public PluginOrigin Origin { get; } = origin;

    public PluginLoadContext LoadContext { get; } = loadContext;

    public WeakReference LoadContextWeakReference { get; } = new WeakReference(loadContext);

    public ILifetimeScope Scope { get; } = scope;

    public IPlugin Instance { get; } = instance;

    public async ValueTask DisposeAsync()
    {
        await Instance.StopAsync(CancellationToken.None);

        // The scope is created by PluginManager.LoadAsync purely to back this plugin; ownership is
        // transferred here, so disposing it is correct despite IDISP007 seeing it as injected.
#pragma warning disable IDISP007 // Don't dispose injected
        Scope.Dispose();
#pragma warning restore IDISP007 // Don't dispose injected
        LoadContext.Unload();
    }
}

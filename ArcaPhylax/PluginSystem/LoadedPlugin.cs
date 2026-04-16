using ArcaPhylax.Abstractions;
using Autofac;
using System.Runtime.Loader;

namespace ArcaPhylax.PluginSystem
{
    public sealed class LoadedPlugin : IAsyncDisposable
    {
        public string Name { get; }
        public PluginLoadContext LoadContext { get; }
        public WeakReference LoadContextWeakReference { get; }
        public ILifetimeScope Scope { get; }
        public IPlugin Instance { get; }

        public LoadedPlugin(
            string name,
            PluginLoadContext loadContext,
            ILifetimeScope scope,
            IPlugin instance)
        {
            Name = name;
            LoadContext = loadContext;
            LoadContextWeakReference = new WeakReference(loadContext);
            Scope = scope;
            Instance = instance;
        }

        public async ValueTask DisposeAsync()
        {
            await Instance.StopAsync(CancellationToken.None);
            Scope.Dispose();
            LoadContext.Unload();
        }
    }
}
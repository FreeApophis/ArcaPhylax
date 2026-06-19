using ArcaPhylax.Abstractions;
using Autofac;

namespace DemiurgPlaygroundPlugin;

internal sealed class DemiurgPlaygroundPlugin : IPlugin
{
    public void Configure(ContainerBuilder builder)
    {
        builder.RegisterModule<DemiurgPlaygroundPluginModule>();
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

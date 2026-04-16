using ArcaPhylax.Abstractions;
using Autofac;

namespace FractalPlugin;

internal class FractalPlugin : IPlugin
{
    public void Configure(ContainerBuilder builder)
    {
        builder.RegisterModule<FactalPluginModule>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

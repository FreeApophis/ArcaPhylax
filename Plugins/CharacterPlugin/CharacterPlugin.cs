using ArcaPhylax.Abstractions;
using Autofac;

namespace CharacterPlugin;

internal sealed class CharacterPlugin : IPlugin
{
    public void Configure(ContainerBuilder builder)
    {
        builder.RegisterModule<CharacterPluginModule>();
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

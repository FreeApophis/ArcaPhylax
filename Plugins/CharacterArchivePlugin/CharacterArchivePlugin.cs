using ArcaPhylax.Abstractions;
using Autofac;

namespace CharacterArchivePlugin;

internal sealed class CharacterArchivePlugin : IPlugin
{
    public void Configure(ContainerBuilder builder)
    {
        builder.RegisterModule<CharacterArchivePluginModule>();
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

using ArcaPhylax.Abstractions;
using Autofac;

namespace Company.HelloPlugin;

/// <summary>
/// The plugin's entry point. ArcaPhylax finds the one <see cref="IPlugin"/> in the assembly, lets it register
/// its services in a child scope of the host container, then starts it.
/// </summary>
public sealed class Plugin : IPlugin
{
    public void Configure(ContainerBuilder builder) => builder.RegisterModule<PluginModule>();

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

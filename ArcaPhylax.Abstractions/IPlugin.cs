using Autofac;

namespace ArcaPhylax.Abstractions;

public interface IPlugin
{
    void Configure(ContainerBuilder builder);

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

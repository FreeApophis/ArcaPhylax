using ArcaPhylax.Abstractions;
using Autofac;

namespace NutritionPlugin;

internal sealed class NutritionPlugin : IPlugin
{
    public void Configure(ContainerBuilder builder)
    {
        builder.RegisterModule<NutritionPluginModule>();
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

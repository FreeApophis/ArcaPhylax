using ArcaPhylax.Abstractions;
using Autofac;

namespace CharacterPlugin
{
    internal class CharacterPlugin : IPlugin
    {
        public void Configure(ContainerBuilder builder)
        {
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
}

using ArcaPhylax.Abstractions;
using Autofac;
using FractalPlugin.ViewModels;
using FractalPlugin.Views;

namespace FractalPlugin;

internal class FactalPluginModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<FractalViewModel>().As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<FractalView>();

        base.Load(builder);
    }
}

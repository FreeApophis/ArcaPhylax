using ArcaPhylax.Abstractions;
using Autofac;
using Company.HelloPlugin.ViewModels;
using Company.HelloPlugin.Views;

namespace Company.HelloPlugin;

/// <summary>
/// Registers the plugin's services. Every <see cref="ITabContentViewModel"/> becomes a page in the ArcaPhylax
/// navigation; its view is found by name (ViewModels.XViewModel → Views.XView) and must be registered too.
/// Host services such as the repositories in ArcaPhylax.Abstractions can be taken as constructor parameters.
/// </summary>
internal sealed class PluginModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MainViewModel>().As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<MainView>();
    }
}

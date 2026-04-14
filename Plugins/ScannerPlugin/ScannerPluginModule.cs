using ArcaPhylax.Abstractions;
using ArcaPhylax.ViewModels;
using ArcaPhylax.Views;
using Autofac;

namespace ScannerPlugin
{
    internal class ScannerPluginModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ScanPageViewModel>().SingleInstance();
            builder.RegisterType<ScanWorkflowViewModel>().As<ITabContentViewModel>().AsSelf().SingleInstance();

            builder.RegisterType<ScanWorkflowView>();

            base.Load(builder);
        }
    }
}

using Autofac;
using FractalPlugin.Views;

namespace FractalPlugin
{
    internal class FactalPluginModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            ////builder.RegisterType<ScanPageViewModel>().SingleInstance();
            ////builder.RegisterType<ScanWorkflowViewModel>().As<ITabContentViewModel>().AsSelf().SingleInstance();

            ////builder.RegisterType<ScanWorkflowView>();

            builder.RegisterType<FractalView>();

            base.Load(builder);
        }
    }
}

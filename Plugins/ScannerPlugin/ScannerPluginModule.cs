using ArcaPhylax.Abstractions;
using ArcaPhylax.ViewModels;
using ArcaPhylax.Views;
using Autofac;
using ScannerPlugin.Services;

namespace ScannerPlugin
{
    internal class ScannerPluginModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ScanPageViewModel>().SingleInstance();
            builder.RegisterType<ScanWorkflowViewModel>().As<ITabContentViewModel>().AsSelf().SingleInstance();

            builder.RegisterType<ScanWorkflowView>();

            // Session-wide scanner state shared with the configuration section (and later the scan page).
            builder.RegisterType<ScannerSettings>().AsSelf().SingleInstance();

            // Scanner configuration section, contributed to the Configure page on plugin load.
            builder.RegisterType<ScannerConfigurationViewModel>().As<IConfigurationSection>().AsSelf().SingleInstance();
            builder.RegisterType<ScannerConfigurationView>();

            base.Load(builder);
        }
    }
}

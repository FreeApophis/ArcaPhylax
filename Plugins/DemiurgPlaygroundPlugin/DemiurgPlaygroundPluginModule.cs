using ArcaPhylax.Abstractions;
using Autofac;
using DemiurgPlaygroundPlugin.Configuration;
using DemiurgPlaygroundPlugin.Services;
using DemiurgPlaygroundPlugin.ViewModels;
using DemiurgPlaygroundPlugin.Views;

namespace DemiurgPlaygroundPlugin;

internal sealed class DemiurgPlaygroundPluginModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Shared session state + persistence.
        builder.RegisterType<DemiurgSettings>().AsSelf().SingleInstance();
        builder.RegisterType<DemiurgSettingsStore>().AsSelf().SingleInstance();

        // LLamaSharp model + chat session, shared between the tab and the Configure-page loader.
        builder.RegisterType<LlamaChatService>().As<ILlamaChatService>().SingleInstance();

        // Tab.
        builder.RegisterType<DemiurgPlaygroundViewModel>().As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<DemiurgPlaygroundView>();

        // Configure-page section.
        builder.RegisterType<DemiurgConfigurationViewModel>().As<IConfigurationSection>().AsSelf().SingleInstance();
        builder.RegisterType<DemiurgConfigurationView>();

        base.Load(builder);
    }
}

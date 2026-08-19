using ArcaPhylax.Abstractions;
using Autofac;
using CharacterPlugin.Configuration;
using CharacterPlugin.ViewModels;
using CharacterPlugin.Views;

namespace CharacterPlugin;

internal sealed class CharacterPluginModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Shared session state + persistence.
        builder.RegisterType<CharacterEditorSettings>().AsSelf().SingleInstance();
        builder.RegisterType<CharacterEditorSettingsStore>().AsSelf().SingleInstance();

        // Editor tab.
        builder.RegisterType<CharacterEditorViewModel>().As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<CharacterEditorView>();

        // Configure-page section.
        builder.RegisterType<CharacterEditorConfigurationViewModel>().As<IConfigurationSection>().AsSelf().SingleInstance();
        builder.RegisterType<CharacterEditorConfigurationView>();

        base.Load(builder);
    }
}

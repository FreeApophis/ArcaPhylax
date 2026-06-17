using ArcaPhylax.Abstractions;
using Autofac;
using CharacterArchivePlugin.Configuration;
using CharacterArchivePlugin.Data;
using CharacterArchivePlugin.Services;
using CharacterArchivePlugin.ViewModels;
using CharacterArchivePlugin.Views;

namespace CharacterArchivePlugin;

internal sealed class CharacterArchivePluginModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Shared session state + persistence.
        builder.RegisterType<CharacterArchiveSettings>().AsSelf().SingleInstance();
        builder.RegisterType<CharacterArchiveSettingsStore>().AsSelf().SingleInstance();

        // Data + image access.
        builder.RegisterType<NpgsqlConnectionFactory>().As<INpgsqlConnectionFactory>().SingleInstance();
        builder.RegisterType<PostgresCharacterRepository>().As<ICharacterRepository>().SingleInstance();
        builder.RegisterType<HashedImageStore>().As<IImageStore>().SingleInstance();

        // Tab.
        builder.RegisterType<CharacterArchiveViewModel>().As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<CharacterArchiveView>();

        // Configure-page section.
        builder.RegisterType<CharacterArchiveConfigurationViewModel>().As<IConfigurationSection>().AsSelf().SingleInstance();
        builder.RegisterType<CharacterArchiveConfigurationView>();

        base.Load(builder);
    }
}

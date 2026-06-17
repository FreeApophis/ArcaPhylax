using ArcaPhylax.Abstractions;
using ArcaPhylax.Data;
using ArcaPhylax.Data.Content;
using ArcaPhylax.Data.Postgres.Provisioning;
using ArcaPhylax.PluginSystem;
using ArcaPhylax.Services;
using ArcaPhylax.ViewModels;
using ArcaPhylax.Views;
using Autofac;
using System.Reflection;

namespace ArcaPhylax.Composition;

public static class CompositionRoot
{
    public static IContainer BuildContainer()
    {
        var builder = new ContainerBuilder();

        // The Appplication
        builder.RegisterType<App.App>().SingleInstance();

        builder.RegisterType<PluginManager>().SingleInstance();

        // View factory: VM -> View (resolved via Autofac)
        builder.RegisterType<ViewFactory>().As<IViewFactory>().SingleInstance();

        // SQLite + repositories
        builder.RegisterType<SqliteConnectionFactory>().As<ISqliteConnectionFactory>().SingleInstance();
        builder.RegisterType<DbInitializer>().As<IDbInitializer>().SingleInstance();
        builder.RegisterType<DocumentRepository>().As<IDocumentRepository>().SingleInstance();

        builder.RegisterType<DroppedItemsRepository>().As<IDroppedItemsRepository>().SingleInstance();

        // Content store
        builder.RegisterType<ItemRepository>().As<IItemRepository>().SingleInstance();
        builder.RegisterType<BlobRepository>().As<IBlobRepository>().SingleInstance();
        builder.RegisterType<TagRepository>().As<ITagRepository>().SingleInstance();
        builder.RegisterType<AttributeRepository>().As<IAttributeRepository>().SingleInstance();
        builder.RegisterType<SetRepository>().As<ISetRepository>().SingleInstance();
        builder.RegisterType<SearchService>().As<ISearchService>().SingleInstance();

        builder.RegisterType<IndexingQueue>().AsSelf().SingleInstance();
        builder.RegisterType<IndexingQueue>().As<IIndexingQueue>().SingleInstance();
        builder.RegisterType<IndexingWorker>().As<IIndexingWorker>().SingleInstance();

        // ViewModels
        builder.RegisterType<MainWindowViewModel>().SingleInstance();
        builder.RegisterType<ImagesViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<VideosViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<DocumentsViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<TagsViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<ChatViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<ConfigureViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<AboutViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();

        // Views
        builder.RegisterType<MainWindow>().SingleInstance();
        builder.RegisterType<ImagesView>();
        builder.RegisterType<VideosView>();
        builder.RegisterType<DocumentsView>();
        builder.RegisterType<TagsView>();
        builder.RegisterType<ChatView>();
        builder.RegisterType<AboutView>();
        builder.RegisterType<ConfigureView>();

        // Postgres Provisioning
        builder.RegisterType<PostgresProvisioningOptions>().AsSelf().SingleInstance();
        builder.RegisterType<RuntimeOperatingSystemInfo>().As<IOperatingSystemInfo>().SingleInstance();
        builder.RegisterType<ProcessCommandRunner>().As<ICommandRunner>().SingleInstance();
        builder.RegisterType<LinuxDistributionDetector>().As<ILinuxDistributionDetector>().SingleInstance();
        builder.RegisterType<PostgresDetector>().As<IPostgresDetector>().SingleInstance();
        builder.RegisterType<CompositePostgresInstaller>().As<IPostgresInstaller>().SingleInstance();
        builder.RegisterType<PostgresProvisioner>().As<IPostgresProvisioner>().SingleInstance();

        builder.RegisterType<AppSettings>().SingleInstance();

        return builder.Build();
    }
}

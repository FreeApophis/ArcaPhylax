using ArcaPhylax.Abstractions;
using ArcaPhylax.Data;
using ArcaPhylax.PluginSystem;
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
        builder.RegisterType<SettingsViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();

        // Views
        builder.RegisterType<MainWindow>().SingleInstance();
        builder.RegisterType<ImagesView>();
        builder.RegisterType<VideosView>();
        builder.RegisterType<DocumentsView>();
        builder.RegisterType<TagsView>();
        builder.RegisterType<ChatView>();
        builder.RegisterType<SettingsView>();

        return builder.Build();
    }
}

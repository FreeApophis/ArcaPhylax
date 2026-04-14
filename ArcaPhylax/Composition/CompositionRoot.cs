using ArcaPhylax.Abstractions;
using ArcaPhylax.Data;
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
        builder.RegisterType<ImagesViewModel>().As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<VideosViewModel>().As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<DocumentsViewModel>().As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<TagsViewModel>().As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<ChatViewModel>().As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<SettingsViewModel>().As<ITabContentViewModel>().AsSelf().SingleInstance();

        // Views
        builder.RegisterType<MainWindow>().SingleInstance();
        builder.RegisterType<ImagesView>();
        builder.RegisterType<VideosView>();
        builder.RegisterType<DocumentsView>();
        builder.RegisterType<TagsView>();
        builder.RegisterType<ChatView>();
        builder.RegisterType<SettingsView>();

        //// builder.FindAndRegisterPlugins();

        return builder.Build();
    }

    private static void FindAndRegisterPlugins(this ContainerBuilder builder)
    {
        builder.RegisterAssemblyModules(FindPluginAssemblies());
    }

    private static Assembly[] FindPluginAssemblies()
    {
        var pluginLocation = "C:\\_Repositories\\ArcaPhylax\\Plugins\\FractalPlugin\\bin\\Release\\net10.0\\publish\\";
        PluginLoadContext loadContext = new(pluginLocation);
        var assembly = loadContext.LoadFromAssemblyName(new AssemblyName("ScannerPlugin"));

        return [assembly];
    }
}

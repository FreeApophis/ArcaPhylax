using ArcaPhylax.Abstractions;
using ArcaPhylax.Infrastructure;
using ArcaPhylax.PluginSystem;
using ArcaPhylax.Services;
using ArcaPhylax.ViewModels;
using ArcaPhylax.Views;
using Autofac;

namespace ArcaPhylax.Composition;

public static class CompositionRoot
{
    public static IContainer BuildContainer()
    {
        var builder = new ContainerBuilder();

        // The Appplication
        builder.RegisterType<App.App>().SingleInstance();

        builder.Register(_ => PluginHost.CreateStore()).SingleInstance();
        builder.Register(_ => PluginHost.CreateTrust()).SingleInstance();
        builder.RegisterType<PluginManager>().SingleInstance();
        builder.Register(_ => new PluginCatalogClient(PluginHost.CatalogUri)).SingleInstance();

        // View factory: VM -> View (resolved via Autofac)
        builder.RegisterType<ViewFactory>().As<IViewFactory>().SingleInstance();

        // Data template that renders configuration-section view models as their views.
        builder.RegisterType<ViewLocator>().AsSelf().SingleInstance();

        // Persistence + provisioning live in ArcaPhylax.Infrastructure.
        builder.RegisterModule<InfrastructureModule>();

        // ViewModels
        builder.RegisterType<MainWindowViewModel>().SingleInstance();
        builder.RegisterType<ImagesViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<VideosViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<DocumentsViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<TagsViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<ChatViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<PluginsViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<ConfigureViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<AboutViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<DashboardViewModel>().WithMetadata("Source", "Host").As<ITabContentViewModel>().AsSelf().SingleInstance();

        // Views
        builder.RegisterType<MainWindow>().SingleInstance();
        builder.RegisterType<ImagesView>();
        builder.RegisterType<VideosView>();
        builder.RegisterType<DocumentsView>();
        builder.RegisterType<TagsView>();
        builder.RegisterType<ChatView>();
        builder.RegisterType<PluginsView>();
        builder.RegisterType<ConfigureView>();
        builder.RegisterType<AboutView>();
        builder.RegisterType<DashboardView>();

        builder.RegisterType<AppSettings>().SingleInstance();

        return builder.Build();
    }
}

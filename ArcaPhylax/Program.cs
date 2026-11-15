using ArcaPhylax.Composition;
using ArcaPhylax.PluginSystem;
using Autofac;
using Avalonia;

namespace ArcaPhylax;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        MainAsync(args).GetAwaiter().GetResult();
    }

    public static async Task MainAsync(string[] args)
    {
        using var container = CompositionRoot.BuildContainer();

        var pluginManager = container.Resolve<PluginManager>();

        await pluginManager.LoadAllPluginsAsync();

        AppBuilder.Configure(container.Resolve<Func<App.App>>())
            .UsePlatformDetect()
            .LogToTrace()
            .StartWithClassicDesktopLifetime(args);
    }
}

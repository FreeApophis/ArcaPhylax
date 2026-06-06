using ArcaPhylax.Composition;
using ArcaPhylax.PluginSystem;
using Autofac;
using Avalonia;
using Funcky;
using Funcky.Extensions;
using Funcky.Monads;
using SkiaSharp;
using static Funcky.Functional;

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
        var container = CompositionRoot.BuildContainer();

        var pluginManager = container.Resolve<PluginManager>();

        await pluginManager.LoadAllPluginsAsync();

        //bool unloaded = await PluginManager.UnloadAsync(plugin);
        //Console.WriteLine($"Unloaded: {unloaded}");

        AppBuilder.Configure(container.Resolve<Func<App.App>>())
            .UsePlatformDetect()
            .LogToTrace()
            .StartWithClassicDesktopLifetime(args);
    }
}

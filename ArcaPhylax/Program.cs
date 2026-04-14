using ArcaPhylax.Composition;
using Autofac;
using Avalonia;

namespace ArcaPhylax;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var container = CompositionRoot.BuildContainer();

        AppBuilder.Configure(container.Resolve<Func<App.App>>())
            .UsePlatformDetect()
            .LogToTrace()
            .StartWithClassicDesktopLifetime(args);
    }
}

using ArcaPhylax.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ArcaPhylax.Data;

namespace ArcaPhylax.App;

public partial class App : Application
{
    private readonly IDbInitializer dbInitializer = null!;
    private readonly IIndexingWorker indexingWorker = null!;
    private readonly Func<MainWindow> mainWindowFactory = null!;
    private readonly CancellationTokenSource cancellationTokenSource = new();

    /// <summary>Used by the runtime XAML loader and the designer; the app proper is resolved
    /// through the dependency-injected constructor below.</summary>
    public App()
    {
    }

    public App(
        IDbInitializer dbInitializer,
        IIndexingWorker indexingWorker,
        Func<MainWindow> mainWindowFactory)
    {
        this.dbInitializer = dbInitializer;
        this.indexingWorker = indexingWorker;
        this.mainWindowFactory = mainWindowFactory;
    }

    public override void Initialize()
        => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Ensure schema exists FIRST
            dbInitializer.EnsureCreatedAndSeed();

            // Resolve shell window from Autofac
            desktop.MainWindow = mainWindowFactory();

            SetupCancellationOnExit(desktop);

            SetupBackgroundProcesses();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupBackgroundProcesses()
    {
        _ = Task.Run(() => indexingWorker.StartAsync(cancellationTokenSource.Token));
    }

    private void SetupCancellationOnExit(IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.Exit += (_, _) => cancellationTokenSource.Cancel();
    }

}

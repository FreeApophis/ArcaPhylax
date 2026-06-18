using ArcaPhylax.Composition;
using ArcaPhylax.Data;
using ArcaPhylax.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace ArcaPhylax.App;

public partial class App : Application
{
    private readonly IDbInitializer _dbInitializer = null!;
    private readonly IIndexingWorker _indexingWorker = null!;
    private readonly Func<MainWindow> _mainWindowFactory = null!;
    private readonly ViewLocator _viewLocator = null!;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>Used by the runtime XAML loader and the designer; the app proper is resolved
    /// through the dependency-injected constructor below.</summary>
    public App()
    {
    }

    public App(
        IDbInitializer dbInitializer,
        IIndexingWorker indexingWorker,
        Func<MainWindow> mainWindowFactory,
        ViewLocator viewLocator)
    {
        _dbInitializer = dbInitializer;
        _indexingWorker = indexingWorker;
        _mainWindowFactory = mainWindowFactory;
        _viewLocator = viewLocator;
    }

    public override void Initialize()
        => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Resolve view-model -> view for content presented by data (e.g. configuration sections).
            DataTemplates.Add(_viewLocator);

            // Ensure schema exists FIRST
            _dbInitializer.EnsureCreatedAndSeed();

            // Resolve shell window from Autofac
            desktop.MainWindow = _mainWindowFactory();

            SetupCancellationOnExit(desktop);

            SetupBackgroundProcesses();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupBackgroundProcesses()
    {
        _ = Task.Run(() => _indexingWorker.StartAsync(_cancellationTokenSource.Token));
    }

    private void SetupCancellationOnExit(IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.Exit += (_, _) => _cancellationTokenSource.Cancel();
    }
}

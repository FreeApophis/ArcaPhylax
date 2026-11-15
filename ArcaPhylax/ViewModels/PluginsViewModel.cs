using System.Collections.Immutable;
using System.Collections.ObjectModel;
using ArcaPhylax.Abstractions;
using ArcaPhylax.PluginPackaging;
using ArcaPhylax.PluginSystem;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArcaPhylax.ViewModels;

/// <summary>The Plugins page: what is loaded, what waits for a restart, the online catalog, and installing from a file.</summary>
public sealed partial class PluginsViewModel : ViewModelBase, ITabContentViewModel
{
    private readonly PluginManager _plugins;
    private readonly PluginCatalogClient _catalogClient;
    private Option<PluginCatalog> _catalog;
    private bool _catalogRequested;

    public PluginsViewModel(PluginManager plugins, PluginCatalogClient catalogClient)
    {
        _plugins = plugins;
        _catalogClient = catalogClient;
        Refresh();
    }

    public string Title => "Plugins";

    public string Icon => NavigationIcons.Plugins;

    public string StoreLocation => PluginHost.StoreRoot;

    public string CatalogLocation => _catalogClient.CatalogUri.ToString();

    public ObservableCollection<PluginRowVm> Rows { get; } = [];

    public ObservableCollection<CatalogRowVm> Available { get; } = [];

    /// <summary>Set by the view so the file picker has a parent window.</summary>
    public TopLevel? HostTopLevel { get; set; }

    [ObservableProperty]
    public partial string? Status { get; set; }

    [ObservableProperty]
    public partial string? CatalogStatus { get; set; }

    [ObservableProperty]
    public partial bool IsRestartPending { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallFromFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallFromCatalogCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadCatalogCommand))]
    public partial bool IsBusy { get; set; }

    private bool CanInstall => !IsBusy;

    /// <summary>Called when the page is shown: the catalog is fetched the first time only, not at startup.</summary>
    public void OnShown()
    {
        if (!_catalogRequested)
        {
            _catalogRequested = true;
            LoadCatalogCommand.Execute(null);
        }
    }

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task LoadCatalog()
    {
        IsBusy = true;
        CatalogStatus = "Loading the plugin catalog…";
        var catalog = await _catalogClient.CatalogAsync();
        _catalog = catalog.Match(left: _ => Option<PluginCatalog>.None, right: Option.Some);
        CatalogStatus = catalog.Match<string?>(left: error => $"The plugin catalog is unavailable: {error}", right: _ => null);
        RefreshCatalog();
        IsBusy = false;
    }

    private bool CanInstallFromCatalog(CatalogRowVm? row) => !IsBusy && row is { CanInstall: true };

    [RelayCommand(CanExecute = nameof(CanInstallFromCatalog))]
    private async Task InstallFromCatalog(CatalogRowVm row)
        => await row.Package.Match(none: Task.CompletedTask, some: package => DownloadAndInstallAsync(row.Name, package));

    private async Task DownloadAndInstallAsync(string name, CatalogPackage package)
    {
        IsBusy = true;
        Status = $"Downloading {name} {package.Version}…";
        var downloaded = await _catalogClient.DownloadAsync(package);
        Status = await downloaded.Match(
            left: error => Task.FromResult($"{name} was not installed: {error}"),
            right: async bytes => Message(await _plugins.InstallAsync(bytes)));
        IsBusy = false;
        Refresh();
    }

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallFromFile()
    {
        if (HostTopLevel?.StorageProvider is not { } storage)
        {
            return;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Install plugin",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("ArcaPhylax plugin") { Patterns = [$"*{PluginPackage.Extension}"] }],
        });

        await files.FirstOrNone()
            .SelectMany(file => Option.FromNullable(file.TryGetLocalPath()))
            .Match(none: Task.CompletedTask, some: InstallAsync);
    }

    private async Task InstallAsync(string path)
    {
        IsBusy = true;
        Status = "Installing…";
        Status = Message(await _plugins.InstallFromFileAsync(path));
        IsBusy = false;
        Refresh();
    }

    [RelayCommand]
    private void Remove(PluginRowVm row)
        => row.RemovableId.AndThen(id =>
        {
            Status = _plugins.ScheduleRemoval(id).Match(
                left: Message,
                right: _ => $"{row.Name} will be removed when ArcaPhylax restarts.");
            Refresh();
        });

    private void Refresh()
    {
        var pending = _plugins.Store.Pending();
        var waiting = InstalledButNotLoaded();

        Rows.Clear();
        foreach (var row in LoadedRows(pending).Concat(WaitingRows(waiting, pending)).Concat(FailedRows()))
        {
            Rows.Add(row);
        }

        IsRestartPending = !pending.IsEmpty || !waiting.IsEmpty;
        RefreshCatalog();
    }

    private void RefreshCatalog()
    {
        Available.Clear();
        foreach (var row in _catalog.Match(none: [], some: CatalogRows))
        {
            Available.Add(row);
        }

        CatalogStatus = _catalog.Match(
            none: CatalogStatus,
            some: catalog => catalog.Plugins.IsEmpty ? "The catalog has no plugins yet." : null);
    }

    private IEnumerable<CatalogRowVm> CatalogRows(PluginCatalog catalog)
        => catalog.Plugins.Select(plugin => CatalogRowVm.Of(plugin, PluginOffer.For(plugin, PluginHost.Environment, InstalledVersion(plugin))));

    /// <summary>The version this app runs of <paramref name="plugin"/>: an installed copy, else a loaded bundled one.</summary>
    private Option<string> InstalledVersion(CatalogPlugin plugin)
        => _plugins.Store.Installed()
            .FirstOrNone(installed => installed.Manifest.Id == plugin.Id)
            .Select(installed => installed.Manifest.Version)
            .OrElse(() => _plugins.LoadedPlugins
                .FirstOrNone(loaded => loaded.Origin is Bundled && IsEntryAssembly(loaded, plugin.EntryAssembly))
                .SelectMany(loaded => Option.FromNullable(loaded.Instance.GetType().Assembly.GetName().Version))
                .Select(version => version.ToString(3)));

    private static bool IsEntryAssembly(LoadedPlugin plugin, string entryAssembly)
        => string.Equals($"{plugin.Instance.GetType().Assembly.GetName().Name}.dll", entryAssembly, StringComparison.OrdinalIgnoreCase);

    private IEnumerable<PluginRowVm> LoadedRows(ImmutableList<PendingOperation> pending)
        => _plugins.LoadedPlugins.Select(plugin => plugin.Origin switch
        {
            Bundled => new PluginRowVm(plugin.Name, AssemblyVersion(plugin), "Bundled", "Loaded", Option<string>.None),
            FromStore store => StoreRow(store.Plugin.Manifest, StateOf(store.Plugin.Manifest.Id, pending, "Loaded")),
        });

    private static IEnumerable<PluginRowVm> WaitingRows(ImmutableList<InstalledPlugin> waiting, ImmutableList<PendingOperation> pending)
        => waiting.Select(plugin => StoreRow(plugin.Manifest, StateOf(plugin.Manifest.Id, pending, "Activates after restart")));

    private IEnumerable<PluginRowVm> FailedRows()
        => _plugins.Failures.Select(failure => new PluginRowVm(
            failure.Name,
            "–",
            SourceOf(failure.Origin),
            $"Failed to load: {failure.Message}",
            failure.Origin switch
            {
                Bundled => Option<string>.None,
                FromStore store => Option.Some(store.Plugin.Manifest.Id),
            }));

    private ImmutableList<InstalledPlugin> InstalledButNotLoaded()
        => _plugins.Store.Installed()
            .Where(installed => !_plugins.LoadedPlugins.Any(loaded => loaded.Origin is FromStore store && store.Plugin.Manifest.Id == installed.Manifest.Id))
            .Where(installed => !_plugins.Failures.Any(failure => failure.Origin is FromStore store && store.Plugin.Manifest.Id == installed.Manifest.Id))
            .ToImmutableList();

    private static PluginRowVm StoreRow(PluginManifest manifest, string state)
        => new(manifest.Name, manifest.Version, "Installed", state, Option.Some(manifest.Id));

    private static string StateOf(string id, ImmutableList<PendingOperation> pending, string otherwise)
        => pending.FirstOrNone(operation => operation.Id == id).Match(
            none: otherwise,
            some: operation => operation switch
            {
                Activate activate => $"Updates to {activate.Version} after restart",
                PluginPackaging.Remove => "Removed after restart",
            });

    private static string SourceOf(PluginOrigin origin) => origin switch
    {
        Bundled => "Bundled",
        FromStore => "Installed",
    };

    private static string AssemblyVersion(LoadedPlugin plugin)
        => plugin.Instance.GetType().Assembly.GetName().Version?.ToString(3) ?? "–";

    private static string Message(InstallReport report) => report switch
    {
        PluginActivated activated => $"{activated.Manifest.Name} {activated.Manifest.Version} was installed and is ready.",
        RestartRequired restart => $"{restart.Manifest.Name} {restart.Manifest.Version} was installed. Restart ArcaPhylax to use it.",
        InstallRejected rejected => $"Not installed: {Message(rejected.Error)}",
        LoadFailed failed => $"{failed.Manifest.Name} was installed but failed to load: {failed.Message}",
    };

    private static string Message(InstallError error) => error switch
    {
        InvalidPackage invalid => Message(invalid.Error),
        WrongPlatform platform => $"the package is for {platform.PluginPlatform}, but this computer needs {platform.HostPlatform}.",
        IncompatibleSdk sdk => $"it was built for plugin SDK {sdk.PluginSdkVersion}; this ArcaPhylax provides {sdk.HostSdkVersion}.",
        AlreadyInstalled installed => $"{installed.Id} {installed.Version} is already installed.",
        NotInstalled notInstalled => $"{notInstalled.Id} is not installed.",
    };

    private static string Message(PackageError error) => error switch
    {
        NotAPackage => "the file is not an ArcaPhylax plugin package.",
        MissingEntry missing when missing.Path == PluginPackage.SignatureFileName => "the package is not signed.",
        MissingEntry missing => $"the package is missing '{missing.Path}'.",
        UnsafeEntryPath unsafePath => $"the package contains an unsafe file path ('{unsafePath.Path}').",
        InvalidManifest manifest => $"its plugin.json is invalid: {manifest.Reason}",
        UntrustedKey key => $"it is signed by an unknown publisher (key {key.KeyId}).",
        InvalidSignature => "its signature does not match its contents; the file may have been tampered with.",
    };
}

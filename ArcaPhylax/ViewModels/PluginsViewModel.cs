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

/// <summary>
/// The Plugins page: what is loaded, what waits for a restart, the online catalog, installing from a file,
/// and which publishers are trusted.
/// </summary>
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

    public ObservableCollection<PublisherRowVm> Publishers { get; } = [];

    /// <summary>Set by the view so the file picker has a parent window.</summary>
    public TopLevel? HostTopLevel { get; set; }

    [ObservableProperty]
    public partial string? Status { get; set; }

    [ObservableProperty]
    public partial string? CatalogStatus { get; set; }

    [ObservableProperty]
    public partial bool IsRestartPending { get; set; }

    /// <summary>An install waiting for the user to decide about an unknown publisher; null when none.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTrustPromptVisible))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmTrustCommand))]
    public partial TrustRequest? PendingTrust { get; set; }

    /// <summary>The name the user gives the publisher they are about to trust.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmTrustCommand))]
    public partial string TrustName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallFromFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallFromCatalogCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadCatalogCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmTrustCommand))]
    public partial bool IsBusy { get; set; }

    public bool IsTrustPromptVisible => PendingTrust is not null;

    private bool CanInstall => !IsBusy;

    private bool CanConfirmTrust => !IsBusy && PendingTrust is not null && !string.IsNullOrWhiteSpace(TrustName);

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

        // The publisher list is optional (none is published until a third party is endorsed); a missing or
        // refused list keeps the one already known.
        (await _catalogClient.PublishersAsync()).SelectMany(_plugins.Trust.Accept);

        var catalog = await _catalogClient.CatalogAsync();
        _catalog = catalog.Match(left: _ => Option<PluginCatalog>.None, right: Option.Some);
        CatalogStatus = catalog.Match<string?>(left: error => $"The plugin catalog is unavailable: {error}", right: _ => null);
        IsBusy = false;
        Refresh();
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
            right: bytes => ReportedAsync(() => _plugins.InstallAsync(bytes)));
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
        Status = await ReportedAsync(() => _plugins.InstallFromFileAsync(path));
        IsBusy = false;
        Refresh();
    }

    /// <summary>Runs an install; one that stops at an unknown publisher becomes a trust prompt instead of an error.</summary>
    private async Task<string> ReportedAsync(Func<Task<InstallReport>> install)
        => await install() switch
        {
            InstallRejected { Error: Untrusted { Error: UnknownPublisher unknown } } => AskForTrust(new TrustRequest(unknown.Key, install)),
            var report => PluginMessages.Of(report),
        };

    private string AskForTrust(TrustRequest request)
    {
        TrustName = string.Empty;
        PendingTrust = request;
        return $"Not installed yet: the plugin is signed by an unknown publisher (key {request.KeyId}).";
    }

    [RelayCommand(CanExecute = nameof(CanConfirmTrust))]
    private async Task ConfirmTrust()
    {
        if (PendingTrust is not { } request)
        {
            return;
        }

        _plugins.Trust.TrustPublisher(TrustName.Trim(), request.Key);
        PendingTrust = null;
        IsBusy = true;
        Status = "Installing…";
        Status = PluginMessages.Of(await request.Retry());
        IsBusy = false;
        Refresh();
    }

    [RelayCommand]
    private void CancelTrust()
    {
        PendingTrust = null;
        Status = "Not installed. The publisher was not trusted.";
    }

    [RelayCommand]
    private void BlockPublisher(PublisherRowVm row)
    {
        _plugins.Trust.BlockPublisher(row.KeyId);
        Status = $"{row.Name} is blocked. Its plugins are no longer installed, and installed ones stop loading when ArcaPhylax restarts.";
        Refresh();
    }

    [RelayCommand]
    private void ForgetPublisher(PublisherRowVm row)
    {
        _plugins.Trust.ForgetPublisher(row.KeyId);
        Status = row.ForgetLabel == "Unblock"
            ? $"{row.Name} is no longer blocked. Its installed plugins load again after a restart."
            : $"{row.Name} is no longer trusted. Its installed plugins stop loading when ArcaPhylax restarts.";
        Refresh();
    }

    [RelayCommand]
    private void Remove(PluginRowVm row)
        => row.RemovableId.AndThen(id =>
        {
            Status = _plugins.ScheduleRemoval(id).Match(
                left: PluginMessages.Of,
                right: _ => $"{row.Name} will be removed when ArcaPhylax restarts.");
            Refresh();
        });

    private void Refresh()
    {
        var policy = _plugins.Trust.Policy;
        var pending = _plugins.Store.Pending();
        var waiting = InstalledButNotLoaded();

        Replace(Rows, LoadedRows(policy, pending).Concat(WaitingRows(policy, waiting, pending)).Concat(FailedRows(policy)));
        Replace(Publishers, policy.Standings().Select(PublisherRowVm.Of));
        IsRestartPending = !pending.IsEmpty || !waiting.IsEmpty || LoadedButNoLongerTrusted(policy);
        RefreshCatalog();
    }

    private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    private void RefreshCatalog()
    {
        Replace(Available, _catalog.Match(none: [], some: CatalogRows));
        CatalogStatus = _catalog.Match(
            none: CatalogStatus,
            some: catalog => catalog.Plugins.IsEmpty ? "The catalog has no plugins yet." : null);
    }

    private IEnumerable<CatalogRowVm> CatalogRows(PluginCatalog catalog)
        => catalog.Plugins.Select(plugin => CatalogRowVm.Of(plugin, OwnerOf(plugin.Id), PluginOffer.For(plugin, PluginHost.Environment, InstalledVersion(plugin))));

    /// <summary>The endorsed publisher that owns <paramref name="pluginId"/>; installing checks the actual signer.</summary>
    private Option<string> OwnerOf(string pluginId)
        => _plugins.Trust.Policy.Endorsed.FirstOrNone(publisher => publisher.Owns(pluginId)).Select(publisher => publisher.Name);

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

    private bool LoadedButNoLongerTrusted(TrustPolicy policy)
        => _plugins.LoadedPlugins.Any(loaded => loaded.Origin is FromStore store && TrustProblem(policy, store.Plugin).Match(none: false, some: True));

    private IEnumerable<PluginRowVm> LoadedRows(TrustPolicy policy, ImmutableList<PendingOperation> pending)
        => _plugins.LoadedPlugins.Select(plugin => plugin.Origin switch
        {
            Bundled => new PluginRowVm(plugin.Name, AssemblyVersion(plugin), "Bundled", "–", "Loaded", Option<string>.None),
            FromStore store => StoreRow(
                policy,
                store.Plugin,
                StateOf(store.Plugin.Manifest.Id, pending, TrustProblem(policy, store.Plugin).Match(
                    none: "Loaded",
                    some: problem => $"Stops loading after restart: {problem}"))),
        });

    private static IEnumerable<PluginRowVm> WaitingRows(TrustPolicy policy, ImmutableList<InstalledPlugin> waiting, ImmutableList<PendingOperation> pending)
        => waiting.Select(plugin => StoreRow(policy, plugin, StateOf(plugin.Manifest.Id, pending, "Activates after restart")));

    private IEnumerable<PluginRowVm> FailedRows(TrustPolicy policy)
        => _plugins.Failures.Select(failure => failure.Origin switch
        {
            Bundled => new PluginRowVm(failure.Name, "–", "Bundled", "–", $"Failed to load: {failure.Message}", Option<string>.None),
            FromStore store => StoreRow(policy, store.Plugin, $"Failed to load: {failure.Message}"),
        });

    private ImmutableList<InstalledPlugin> InstalledButNotLoaded()
        => _plugins.Store.Installed()
            .Where(installed => !_plugins.LoadedPlugins.Any(loaded => loaded.Origin is FromStore store && store.Plugin.Manifest.Id == installed.Manifest.Id))
            .Where(installed => !_plugins.Failures.Any(failure => failure.Origin is FromStore store && store.Plugin.Manifest.Id == installed.Manifest.Id))
            .ToImmutableList();

    private static PluginRowVm StoreRow(TrustPolicy policy, InstalledPlugin plugin, string state)
        => new(plugin.Manifest.Name, plugin.Manifest.Version, "Installed", PublisherOf(policy, plugin), state, Option.Some(plugin.Manifest.Id));

    private static string PublisherOf(TrustPolicy policy, InstalledPlugin plugin)
        => policy.Check(plugin.Manifest.Id, plugin.Signer).Match(
            left: _ => $"Untrusted ({plugin.Signer.KeyId})",
            right: signer => signer switch
            {
                EndorsedSigner endorsed => endorsed.Publisher.Name,
                UserTrustedSigner trusted => $"{trusted.Key.Name} (trusted by you)",
            });

    private static Option<string> TrustProblem(TrustPolicy policy, InstalledPlugin plugin)
        => policy.Check(plugin.Manifest.Id, plugin.Signer).Match(
            left: error => Option.Some(PluginMessages.Of(error)),
            right: _ => Option<string>.None);

    private static string StateOf(string id, ImmutableList<PendingOperation> pending, string otherwise)
        => pending.FirstOrNone(operation => operation.Id == id).Match(
            none: otherwise,
            some: operation => operation switch
            {
                Activate activate => $"Updates to {activate.Version} after restart",
                PluginPackaging.Remove => "Removed after restart",
            });

    private static string AssemblyVersion(LoadedPlugin plugin)
        => plugin.Instance.GetType().Assembly.GetName().Version?.ToString(3) ?? "–";
}

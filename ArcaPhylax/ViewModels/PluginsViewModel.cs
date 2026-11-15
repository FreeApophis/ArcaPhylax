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

/// <summary>The Plugins page: what is loaded, what waits for a restart, and installing from a file.</summary>
public sealed partial class PluginsViewModel : ViewModelBase, ITabContentViewModel
{
    private readonly PluginManager _plugins;

    public PluginsViewModel(PluginManager plugins)
    {
        _plugins = plugins;
        Refresh();
    }

    public string Title => "Plugins";

    public string Icon => NavigationIcons.Plugins;

    public string StoreLocation => PluginHost.StoreRoot;

    public ObservableCollection<PluginRowVm> Rows { get; } = [];

    /// <summary>Set by the view so the file picker has a parent window.</summary>
    public TopLevel? HostTopLevel { get; set; }

    [ObservableProperty]
    public partial string? Status { get; set; }

    [ObservableProperty]
    public partial bool IsRestartPending { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallFromFileCommand))]
    public partial bool IsBusy { get; set; }

    private bool CanInstall => !IsBusy;

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
    }

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

using System.Collections.ObjectModel;
using System.ComponentModel;
using ArcaPhylax.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAPS2.Images;
using NAPS2.Images.ImageSharp;
using NAPS2.Scan;
using ScannerPlugin.Models;
using ScannerPlugin.Services;

namespace ArcaPhylax.ViewModels;

/// <summary>
/// Scanner configuration section: pick a scanner (from NAPS2's device list) and manage scan
/// profiles. Selected device/profile live in the shared <see cref="ScannerSettings"/> so the
/// scan page can consume them later.
/// </summary>
public sealed partial class ScannerConfigurationViewModel : ObservableObject, IConfigurationSection, IDisposable
{
    private readonly ScanningContext _scanningContext;
    private readonly ScanController _controller;

    public ScannerConfigurationViewModel(ScannerSettings settings)
    {
        Settings = settings;

        _scanningContext = new ScanningContext(new ImageSharpImageContext());
        _controller = new ScanController(_scanningContext);

        Settings.PropertyChanged += OnSettingsChanged;
    }

    public string Title => "Scanner";

    public int Order => 100;

    public ScannerSettings Settings { get; }

    public ObservableCollection<ScanDevice> Devices { get; } = new();

    public int[] DpiOptions { get; } = [100, 150, 200, 300, 400, 600];

    public ScanPageSize[] PageSizes { get; } = Enum.GetValues<ScanPageSize>();

    public PaperSource[] PaperSources { get; } = Enum.GetValues<PaperSource>();

    public BitDepth[] BitDepths { get; } = Enum.GetValues<BitDepth>();

    [ObservableProperty]
    public partial string Status { get; set; } = "Click Refresh to list connected scanners.";

    public bool HasSelectedProfile => Settings.SelectedProfile is not null;

    public void Dispose()
    {
        Settings.PropertyChanged -= OnSettingsChanged;
        _scanningContext.Dispose();
    }

    [RelayCommand]
    private async Task RefreshDevices()
    {
        try
        {
            Status = "Searching for scanners...";
            Devices.Clear();

            var list = await _controller.GetDeviceList();
            foreach (var device in list.OrderBy(d => d.Name))
            {
                Devices.Add(device);
            }

            Settings.SelectedDevice ??= Devices.FirstOrDefault();
            Status = Devices.Count == 0 ? "No scanners found." : $"{Devices.Count} scanner(s) found.";
        }
        catch (Exception ex)
        {
            Status = $"Failed to list scanners: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddProfile()
    {
        var profile = new ScannerProfile { Name = $"Profile {Settings.Profiles.Count + 1}" };
        Settings.Profiles.Add(profile);
        Settings.SelectedProfile = profile;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProfile))]
    private void RemoveProfile()
    {
        if (Settings.SelectedProfile is not { } profile)
        {
            return;
        }

        var index = Settings.Profiles.IndexOf(profile);
        Settings.Profiles.Remove(profile);
        Settings.SelectedProfile = Settings.Profiles.ElementAtOrDefault(Math.Max(0, index - 1));
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScannerSettings.SelectedProfile))
        {
            OnPropertyChanged(nameof(HasSelectedProfile));
            RemoveProfileCommand.NotifyCanExecuteChanged();
        }
    }
}

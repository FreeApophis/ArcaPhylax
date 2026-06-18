using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcaPhylax.Abstractions;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAPS2.Images;
using NAPS2.Images.ImageSharp;
using NAPS2.Pdf;
using NAPS2.Scan;
using ScannerPlugin.Models;
using ScannerPlugin.Services;

namespace ArcaPhylax.ViewModels;

/// <summary>
/// Scan page: pick a profile (the scanner is configured on the Configure page), scan one or more
/// pages, select pages, and save them as a single PDF or as individual images.
/// </summary>
public sealed partial class ScanWorkflowViewModel : ObservableObject, ITabContentViewModel, IDisposable
{
    private readonly ScanningContext _scanningContext;
    private readonly ScanController _controller;

    private CancellationTokenSource? _scanCts;

    public ScanWorkflowViewModel(ScannerSettings settings)
    {
        Settings = settings;
        Settings.PropertyChanged += OnSettingsChanged;

        _scanningContext = new ScanningContext(new ImageSharpImageContext());
        _controller = new ScanController(_scanningContext);

        Pages = new ObservableCollection<ScanPageViewModel>();
        Pages.CollectionChanged += (_, _) => NotifySaveState();
    }

    public string Title => "Scan";
    public string Icon => ScannerIcons.Scan;
    public int NavigationWeight => 80;

    /// <summary>Set by the view so the file pickers have a parent window.</summary>
    public TopLevel? HostTopLevel { get; set; }

    public ScannerSettings Settings { get; }

    public ObservableCollection<ScanPageViewModel> Pages { get; }

    [ObservableProperty]
    public partial string FileName { get; set; } = "Scan";

    [ObservableProperty]
    public partial string Status { get; set; } = "Pick a profile and press Scan.";

    [ObservableProperty]
    public partial bool IsScanning { get; set; }

    public string DeviceDisplay => Settings.SelectedDevice?.Name ?? "No scanner configured (see Configuration)";

    public bool CanScan => Settings.SelectedDevice is not null && Settings.SelectedProfile is not null && !IsScanning;
    public bool HasSelection => Pages.Any(p => p.IsSelected);
    public bool CanSave => HasSelection && !IsScanning;

    partial void OnIsScanningChanged(bool value)
    {
        ScanCommand.NotifyCanExecuteChanged();
        CancelScanCommand.NotifyCanExecuteChanged();
        NotifySaveState();
    }

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task Scan()
    {
        if (Settings.SelectedDevice is not { } device || Settings.SelectedProfile is not { } profile)
        {
            return;
        }

        IsScanning = true;
        _scanCts = new CancellationTokenSource();

        try
        {
            Status = "Scanning...";

            var options = new ScanOptions
            {
                Device = device,
                Dpi = profile.Dpi,
                PageSize = ToPageSize(profile.PageSize),
                PaperSource = profile.PaperSource,
                BitDepth = profile.BitDepth,
                ThumbnailSize = 256,
            };

            var count = 0;
            await foreach (var image in _controller.Scan(options, _scanCts.Token))
            {
                var page = new ScanPageViewModel(image)
                {
                    IsSelected = true,
                    Thumbnail = ToBitmap(image),
                };
                page.PropertyChanged += OnPageChanged;
                Pages.Add(page);

                count++;
                Status = $"Scanned {count} page(s)...";
            }

            Status = $"Done. Scanned {count} page(s).";
        }
        catch (OperationCanceledException)
        {
            Status = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            Status = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }

    [RelayCommand(CanExecute = nameof(IsScanning))]
    private void CancelScan() => _scanCts?.Cancel();

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var page in Pages)
        {
            page.IsSelected = true;
        }
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var page in Pages)
        {
            page.IsSelected = false;
        }
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        foreach (var page in Pages.Where(p => p.IsSelected).ToList())
        {
            page.PropertyChanged -= OnPageChanged;
            Pages.Remove(page);
            page.Dispose();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsPdf()
    {
        if (HostTopLevel?.StorageProvider is not { } storage)
        {
            Status = "No storage provider available.";
            return;
        }

        var selected = Pages.Where(p => p.IsSelected).Select(p => p.ProcessedImage).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save PDF",
            SuggestedFileName = SafeBaseName() + ".pdf",
            FileTypeChoices = [new FilePickerFileType("PDF") { Patterns = ["*.pdf"] }],
        });

        if (file is null)
        {
            return;
        }

        try
        {
            Status = "Creating PDF...";
            var exporter = new PdfExporter(_scanningContext);
            await exporter.Export(file.Path.LocalPath, selected, new PdfExportParams());
            Status = $"PDF saved: {Path.GetFileName(file.Path.LocalPath)} ({selected.Count} page(s)).";
        }
        catch (Exception ex)
        {
            Status = $"PDF export failed: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsImages()
    {
        if (HostTopLevel?.StorageProvider is not { } storage)
        {
            Status = "No storage provider available.";
            return;
        }

        var selected = Pages.Where(p => p.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose a folder for the images",
            AllowMultiple = false,
        });

        if (folders.FirstOrDefault() is not { } folder)
        {
            return;
        }

        try
        {
            Status = "Saving images...";
            var directory = folder.Path.LocalPath;
            var baseName = SafeBaseName();

            var index = 1;
            foreach (var page in selected)
            {
                var path = Path.Combine(directory, $"{baseName}_{index}.png");
                page.ProcessedImage.Save(path, ImageFileFormat.Png);
                index++;
            }

            Status = $"Saved {selected.Count} image(s) to {directory}.";
        }
        catch (Exception ex)
        {
            Status = $"Image export failed: {ex.Message}";
        }
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ScannerSettings.SelectedDevice):
                OnPropertyChanged(nameof(DeviceDisplay));
                ScanCommand.NotifyCanExecuteChanged();
                break;
            case nameof(ScannerSettings.SelectedProfile):
                ScanCommand.NotifyCanExecuteChanged();
                break;
        }
    }

    private void OnPageChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScanPageViewModel.IsSelected))
        {
            NotifySaveState();
        }
    }

    private void NotifySaveState()
    {
        OnPropertyChanged(nameof(HasSelection));
        SaveAsPdfCommand.NotifyCanExecuteChanged();
        SaveAsImagesCommand.NotifyCanExecuteChanged();
    }

    private string SafeBaseName()
    {
        var name = string.IsNullOrWhiteSpace(FileName) ? "Scan" : FileName.Trim();
        return Path.GetInvalidFileNameChars().Aggregate(name, (current, c) => current.Replace(c, '_'));
    }

    private static PageSize ToPageSize(ScanPageSize pageSize) => pageSize switch
    {
        ScanPageSize.A5 => PageSize.A5,
        ScanPageSize.A3 => PageSize.A3,
        ScanPageSize.Letter => PageSize.Letter,
        ScanPageSize.Legal => PageSize.Legal,
        _ => PageSize.A4,
    };

    private static Avalonia.Media.Imaging.Bitmap? ToBitmap(ProcessedImage processed)
    {
        try
        {
            using var stream = processed.SaveToMemoryStream(ImageFileFormat.Png);
            stream.Position = 0;
            return new Avalonia.Media.Imaging.Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        Settings.PropertyChanged -= OnSettingsChanged;

        foreach (var page in Pages)
        {
            page.PropertyChanged -= OnPageChanged;
            page.Dispose();
        }

        Pages.Clear();

        _scanCts?.Cancel();
        _scanCts?.Dispose();
        _scanningContext.Dispose();
    }
}

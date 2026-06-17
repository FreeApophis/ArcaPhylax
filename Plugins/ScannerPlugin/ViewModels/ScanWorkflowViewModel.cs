using ArcaPhylax.Abstractions;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAPS2.Images;
using NAPS2.Images.ImageSharp;
using NAPS2.Pdf;
using NAPS2.Scan;
using System.Collections.ObjectModel;

namespace ArcaPhylax.ViewModels;

public partial class ScanWorkflowViewModel : ObservableObject, ITabContentViewModel, IDisposable
{
    private readonly ScanningContext _scanningContext;
    private readonly ScanController _controller;

    private CancellationTokenSource? _scanCts;

    public string Title => "Scan";

    public string Icon => ScannerIcons.Scan;


    public ScanWorkflowViewModel()
    {
        // Plattformunabhängig: ImageSharp
        _scanningContext = new ScanningContext(new ImageSharpImageContext()); // :contentReference[oaicite:4]{index=4}
        _controller = new ScanController(_scanningContext);

        Pages = new ObservableCollection<ScanPageViewModel>();
        Devices = new ObservableCollection<ScanDevice>();

        DocumentName = "Scan";
        TagsText = "";

        RefreshDevicesCommand.Execute(null);
    }

    public TopLevel? HostTopLevel { get; set; }

    public ObservableCollection<ScanDevice> Devices { get; }
    public ObservableCollection<ScanPageViewModel> Pages { get; }

    [ObservableProperty] private ScanDevice? selectedDevice;
    [ObservableProperty] private ScanPageViewModel? activePage;

    [ObservableProperty] private string status = "Bereit.";
    [ObservableProperty] private bool isScanning;

    [ObservableProperty] private string documentName;
    [ObservableProperty] private string tagsText;

    [ObservableProperty] private Bitmap? activePagePreview;

    public bool CanScan => SelectedDevice != null && !IsScanning;
    public bool CanExport => Pages.Any(p => p.IsSelected) && !IsScanning;
    public bool HasAnySelected => Pages.Any(p => p.IsSelected);

    partial void OnSelectedDeviceChanged(ScanDevice? value)
        => OnPropertyChanged(nameof(CanScan));

    partial void OnIsScanningChanged(bool value)
    {
        OnPropertyChanged(nameof(CanScan));
        OnPropertyChanged(nameof(CanExport));
    }

    partial void OnActivePageChanged(ScanPageViewModel? value)
    {
        if (value?.ProcessedImage is null)
        {
            ActivePagePreview = null;
            return;
        }

        // Preview (größer): als PNG in MemoryStream speichern und in Avalonia Bitmap laden
        ActivePagePreview = ToAvaloniaBitmap(value.ProcessedImage, thumbnail: false);
    }

    [RelayCommand]
    private async Task RefreshDevices()
    {
        try
        {
            Status = "Scanner werden gesucht...";
            Devices.Clear();

            // Alternativ: GetDeviceList() liefert Task<List<ScanDevice>> :contentReference[oaicite:5]{index=5}
            var list = await _controller.GetDeviceList();
            foreach (var d in list.OrderBy(x => x.Name))
                Devices.Add(d);

            SelectedDevice ??= Devices.FirstOrDefault();
            Status = Devices.Count == 0 ? "Keine Scanner gefunden." : $"{Devices.Count} Scanner gefunden.";
        }
        catch (Exception ex)
        {
            Status = $"Fehler beim Laden der Scanner: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task Scan()
    {
        if (SelectedDevice is null) return;

        IsScanning = true;
        _scanCts = new CancellationTokenSource();

        try
        {
            Status = "Scan läuft...";

            var options = new ScanOptions
            {
                Device = SelectedDevice,
                Dpi = 300,
                PageSize = PageSize.A4,
                PaperSource = PaperSource.Feeder,
                ThumbnailSize = 256
            };

            int pageCounter = 0;

            await foreach (var img in _controller.Scan(options, _scanCts.Token)) // :contentReference[oaicite:6]{index=6}
            {
                pageCounter++;

                var pageVm = new ScanPageViewModel(img)
                {
                    IsSelected = true
                };

                // Thumbnail aus PostProcessingData, falls vorhanden
                pageVm.Thumbnail = ToAvaloniaBitmap(img, thumbnail: true);

                Pages.Add(pageVm);

                ActivePage ??= pageVm;
                Status = $"Gescannt: {pageCounter} Seite(n)";
                OnPropertyChanged(nameof(CanExport));
                OnPropertyChanged(nameof(HasAnySelected));
            }

            Status = $"Scan fertig ({pageCounter} Seite(n)).";
        }
        catch (OperationCanceledException)
        {
            Status = "Scan abgebrochen.";
        }
        catch (Exception ex)
        {
            Status = $"Scan-Fehler: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }

    [RelayCommand]
    private void CancelScan()
    {
        if (!IsScanning) return;
        _scanCts?.Cancel();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var p in Pages) p.IsSelected = true;
        OnPropertyChanged(nameof(CanExport));
        OnPropertyChanged(nameof(HasAnySelected));
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var p in Pages) p.IsSelected = false;
        OnPropertyChanged(nameof(CanExport));
        OnPropertyChanged(nameof(HasAnySelected));
    }

    [RelayCommand]
    private void DeleteSelectedPages()
    {
        var toDelete = Pages.Where(p => p.IsSelected).ToList();
        foreach (var p in toDelete)
        {
            p.Dispose();
            Pages.Remove(p);
        }

        ActivePage = Pages.FirstOrDefault();
        OnPropertyChanged(nameof(CanExport));
        OnPropertyChanged(nameof(HasAnySelected));
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportPdf()
    {
        if (HostTopLevel?.StorageProvider is null)
        {
            Status = "Kein StorageProvider verfügbar (TopLevel nicht gesetzt).";
            return;
        }

        var selected = Pages.Where(p => p.IsSelected).Select(p => p.ProcessedImage).ToList();
        if (selected.Count == 0) return;

        var file = await HostTopLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "PDF speichern",
            SuggestedFileName = MakeSafeFileName(DocumentName) + ".pdf",
            FileTypeChoices =
            [
                new FilePickerFileType("PDF") { Patterns = ["*.pdf"] }
            ]
        });

        if (file is null) return;

        try
        {
            Status = "PDF wird erzeugt...";

            var exporter = new PdfExporter(_scanningContext); // :contentReference[oaicite:7]{index=7}

            // Name + Tags als PDF-Metadaten: Title + Keywords :contentReference[oaicite:8]{index=8}
            var metadata = new PdfMetadata
            {
                Title = (DocumentName ?? "").Trim(),
                Keywords = NormalizeTags(TagsText),
                Subject = "",
                Author = "",
                Creator = "ArcaPhylax (Avalonia + NAPS2)"
            };

            var exportParams = new PdfExportParams
            {
                Metadata = metadata,
                Compat = PdfCompat.Default,
                Encryption = new PdfEncryption()
            };

            var path = file.Path.LocalPath;
            await exporter.Export(path, selected, exportParams);

            Status = $"PDF gespeichert: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            Status = $"PDF-Export Fehler: {ex.Message}";
        }
    }

    private static string NormalizeTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags)) return "";
        var list = tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return string.Join(", ", list);
    }

    private static string MakeSafeFileName(string? input)
    {
        var name = string.IsNullOrWhiteSpace(input) ? "Scan" : input.Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private static Bitmap? ToAvaloniaBitmap(ProcessedImage processed, bool thumbnail)
    {
        IMemoryImage? image = null;

        // Wenn ScanOptions.ThumbnailSize gesetzt ist, liegt oft ein Thumbnail in PostProcessingData :contentReference[oaicite:9]{index=9}
        if (thumbnail)
        {
            image = processed.PostProcessingData?.Thumbnail;
        }

        // Fallback: gerenderte Seite selbst speichern
        if (image is null)
        {

            // image ??= processed.RenderToMemoryImage(); wenn verfügbar; falls nicht, unten Fallback über Save(IRenderableImage,..)
            throw new NotImplementedException();
        }
        try
        {
            using var ms = new MemoryStream();

            if (image != null)
            {
                image.Save(ms, ImageFileFormat.Png); // :contentReference[oaicite:10]{index=10}
            }
            else
            {
                // Fallback: ProcessedImage ist IRenderableImage und kann per Extension Save(...) gespeichert werden :contentReference[oaicite:11]{index=11}
                processed.Save(ms, ImageFileFormat.Png);
            }

            ms.Position = 0;
            return new Bitmap(ms);
        }
        catch
        {
            return null;
        }
        finally
        {
            image?.Dispose();
        }
    }

    public void Dispose()
    {
        foreach (var p in Pages) p.Dispose();
        Pages.Clear();

        _scanCts?.Cancel();
        _scanCts?.Dispose();

        _scanningContext?.Dispose();
    }
}

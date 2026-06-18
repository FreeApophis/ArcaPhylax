using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using NAPS2.Images;

namespace ArcaPhylax.ViewModels;

public sealed partial class ScanPageViewModel(ProcessedImage processed) : ObservableObject, IDisposable
{
    public ProcessedImage ProcessedImage { get; } = processed ?? throw new ArgumentNullException(nameof(processed));

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial Bitmap? Thumbnail { get; set; }

    public string DisplayName { get; } = $"Page {processed.PostProcessingData?.PageNumber ?? 0}";

    public string Info { get; } = processed.PostProcessingData?.PageSide.ToString() ?? string.Empty;

    public void Dispose()
    {
        Thumbnail?.Dispose();
        Thumbnail = null;

        // ScanWorkflowViewModel hands the scanned image to this page and never touches it again;
        // the page owns its lifetime, so disposing it here is correct despite IDISP007.
#pragma warning disable IDISP007 // Don't dispose injected
        ProcessedImage.Dispose();
#pragma warning restore IDISP007 // Don't dispose injected
    }
}
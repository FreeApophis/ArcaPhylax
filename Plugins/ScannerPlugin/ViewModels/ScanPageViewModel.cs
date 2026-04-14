using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using NAPS2.Images;

namespace ArcaPhylax.ViewModels;

public partial class ScanPageViewModel : ObservableObject, IDisposable
{

    public ScanPageViewModel(ProcessedImage processed)
    {
        ProcessedImage = processed ?? throw new ArgumentNullException(nameof(processed));
        DisplayName = $"Seite {processed.PostProcessingData?.PageNumber ?? 0}";
        Info = processed.PostProcessingData?.PageSide.ToString() ?? "";
    }

    public ProcessedImage ProcessedImage { get; }

    [ObservableProperty] 
    private bool isSelected;

    [ObservableProperty] 
    private Bitmap? thumbnail;

    public string DisplayName { get; }
    public string Info { get; }


    public void Dispose()
    {
        Thumbnail?.Dispose();
        Thumbnail = null;

        ProcessedImage.Dispose();
    }
}
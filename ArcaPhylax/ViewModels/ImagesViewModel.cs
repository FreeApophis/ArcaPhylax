using ArcaPhylax.Abstractions;

namespace ArcaPhylax.ViewModels;

public sealed class ImagesViewModel : NotifyBase, ITabContentViewModel
{
    public string Title => "Images";
    public string Icon => "🖼";
}

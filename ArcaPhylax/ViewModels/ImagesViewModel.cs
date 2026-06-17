using ArcaPhylax.Abstractions;

namespace ArcaPhylax.ViewModels;

public sealed class ImagesViewModel : ViewModelBase, ITabContentViewModel
{
    public string Title => "Images";
    public string Icon => NavigationIcons.Images;
}

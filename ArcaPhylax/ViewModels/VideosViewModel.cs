using ArcaPhylax.Abstractions;

namespace ArcaPhylax.ViewModels;

public sealed class VideosViewModel : ViewModelBase, ITabContentViewModel
{
    public string Title => "Videos";

    public string Icon => NavigationIcons.Videos;

    public int NavigationWeight => 40;
}

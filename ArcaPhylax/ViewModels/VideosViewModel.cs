using ArcaPhylax.Abstractions;

namespace ArcaPhylax.ViewModels;

public sealed class VideosViewModel : NotifyBase, ITabContentViewModel
{
    public string Title => "Videos";
    public string Icon => "🎬";
}

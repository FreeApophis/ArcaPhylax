using ArcaPhylax.Abstractions;

namespace ArcaPhylax.ViewModels;

public sealed class TagsViewModel : NotifyBase, ITabContentViewModel
{
    public string Title => "Tags";
    public string Icon => "🏷";
    public bool IsActive { get; set; }
}

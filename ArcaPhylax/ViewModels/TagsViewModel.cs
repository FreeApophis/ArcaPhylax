using ArcaPhylax.Abstractions;

namespace ArcaPhylax.ViewModels;

public sealed class TagsViewModel : ViewModelBase, ITabContentViewModel
{
    public string Title => "Tags";
    public string Icon => NavigationIcons.Tags;
    public bool IsActive { get; set; }
}

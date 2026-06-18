using ArcaPhylax.Abstractions;

namespace ArcaPhylax.ViewModels;

public sealed class TabButtonViewModel(ITabContentViewModel tab) : ViewModelBase
{
    public ITabContentViewModel Tab { get; } = tab;

    public string Title => Tab.Title;

    public object Icon => Tab.Icon;
}

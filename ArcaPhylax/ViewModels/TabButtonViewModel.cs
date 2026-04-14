using ArcaPhylax.Abstractions;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace ArcaPhylax.ViewModels;

public sealed class TabButtonViewModel(ITabContentViewModel tab, Action<ITabContentViewModel> select) : NotifyBase
{
    public ITabContentViewModel Tab { get; } = tab;
    public string Title => Tab.Title;
    public object Icon => Tab.Icon;

    private readonly Action<ITabContentViewModel> _select = select;

    public ICommand SelectCommand => new RelayCommand(() => _select(Tab));

    public bool IsSelected
    {
        get => field;
        set
        {
            if (!Set(ref field, value)) return;
            if (value) _select(Tab); // only react when turned on
        }
    }
}

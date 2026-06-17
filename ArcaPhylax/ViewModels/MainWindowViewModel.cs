using ArcaPhylax.Abstractions;
using ArcaPhylax.Composition;
using ArcaPhylax.Data;
using ArcaPhylax.PluginSystem;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ArcaPhylax.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IViewFactory _views;

    [ObservableProperty]
    public partial bool IsPaneOpen { get; set; }

    private Control? _currentContent;
    public Control? CurrentContent { get => _currentContent; private set => SetProperty(ref _currentContent, value); }

    /// <summary>Weight-ordered tabs that fill the top of the navigation rail.</summary>
    public ObservableCollection<TabButtonViewModel> Tabs { get; }

    /// <summary>Configure and About, pinned to the bottom of the navigation rail.</summary>
    public ObservableCollection<TabButtonViewModel> BottomTabs { get; }

    // The two lists share one logical selection. Selecting in one clears the other so only a
    // single nav item is ever highlighted, and the active tab drives CurrentContent.
    public TabButtonViewModel? SelectedMainTab
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                SelectedBottomTab = null;
                CurrentContent = _views.Create(value.Tab);
            }
        }
    }

    public TabButtonViewModel? SelectedBottomTab
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                SelectedMainTab = null;
                CurrentContent = _views.Create(value.Tab);
            }
        }
    }

    public MainWindowViewModel(
        IViewFactory views,
        PluginManager pluginManager)
    {
        _views = views;

        var ordered = pluginManager.AllTabs.OrderBy(NavigationSortKey).ToList();

        Tabs = new ObservableCollection<TabButtonViewModel>(
            ordered.Where(tab => !IsBottomTab(tab)).Select(tab => new TabButtonViewModel(tab)));
        BottomTabs = new ObservableCollection<TabButtonViewModel>(
            ordered.Where(IsBottomTab).Select(tab => new TabButtonViewModel(tab)));

        SelectedMainTab = Tabs.FirstOrDefault();
    }

    private static bool IsBottomTab(ITabContentViewModel tab) => tab is ConfigureViewModel or AboutViewModel;

    // Configure then About are pinned to the bottom; every other tab is ordered by its
    // NavigationWeight (lower first). OrderBy is stable, so equal weights keep their original order.
    private static (int Section, int Weight) NavigationSortKey(ITabContentViewModel tab) => tab switch
    {
        ConfigureViewModel => (1, 0),
        AboutViewModel => (2, 0),
        _ => (0, tab.NavigationWeight),
    };


    [RelayCommand]
    private void TogglePane() => IsPaneOpen = !IsPaneOpen;
}

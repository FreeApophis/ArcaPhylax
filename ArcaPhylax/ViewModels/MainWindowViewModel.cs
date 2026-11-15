using System.Collections.ObjectModel;
using ArcaPhylax.Abstractions;
using ArcaPhylax.Composition;
using ArcaPhylax.PluginSystem;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArcaPhylax.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IViewFactory _views;

    private Control? _currentContent;

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

        // A plugin installed while the app runs contributes its tabs immediately.
        pluginManager.PluginLoaded += (_, plugin) => AddTabs(PluginManager.TabsOf(plugin));
    }

    [ObservableProperty]
    public partial bool IsPaneOpen { get; set; }

    public Control? CurrentContent { get => _currentContent; private set => SetProperty(ref _currentContent, value); }

    /// <summary>Weight-ordered tabs that fill the top of the navigation rail.</summary>
    public ObservableCollection<TabButtonViewModel> Tabs { get; }

    /// <summary>Configure and About, pinned to the bottom of the navigation rail.</summary>
    public ObservableCollection<TabButtonViewModel> BottomTabs { get; }

    // The two lists share one logical selection. Selecting in one clears the other so only a
    // single nav item is ever highlighted, and the active tab drives CurrentContent.
    public TabButtonViewModel? SelectedMainTab
    {
        get
        {
            return field;
        }

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
        get;
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                SelectedMainTab = null;
                CurrentContent = _views.Create(value.Tab);
            }
        }
    }

    private static bool IsBottomTab(ITabContentViewModel tab) => tab is PluginsViewModel or ConfigureViewModel or AboutViewModel;

    // Plugins, Configure, then About are pinned to the bottom; every other tab is ordered by its
    // NavigationWeight (lower first). OrderBy is stable, so equal weights keep their original order.
    private static (int Section, int Weight) NavigationSortKey(ITabContentViewModel tab) => tab switch
    {
        PluginsViewModel => (1, 0),
        ConfigureViewModel => (2, 0),
        AboutViewModel => (3, 0),
        _ => (0, tab.NavigationWeight),
    };

    /// <summary>Inserts plugin tabs into the main list at their weight position.</summary>
    private void AddTabs(IEnumerable<ITabContentViewModel> tabs)
    {
        foreach (var tab in tabs.Where(tab => !IsBottomTab(tab)))
        {
            var index = Tabs.TakeWhile(existing => existing.Tab.NavigationWeight <= tab.NavigationWeight).Count();
            Tabs.Insert(index, new TabButtonViewModel(tab));
        }
    }

    [RelayCommand]
    private void TogglePane() => IsPaneOpen = !IsPaneOpen;
}

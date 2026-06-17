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
    private readonly IDocumentRepository _repo;

    [ObservableProperty]
    public partial bool IsPaneOpen { get; set; }

    /// <summary>Icon for the kebab/menu toggle button.</summary>
    public Geometry MenuIcon { get; } = Geometry.Parse(NavigationIcons.Menu);

    public ObservableCollection<DocumentTileVm> Items { get; } = new();

    public string? Query
    {
        get => field;
        set
        {
            SetProperty(ref field, value);

            ReloadTiles();
        }
    }

    private Control? _currentContent;
    public Control? CurrentContent { get => _currentContent; private set => SetProperty(ref _currentContent, value); }

    public ObservableCollection<TabButtonViewModel> Tabs { get; }

    public TabButtonViewModel? SelectedTab
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value))
            {
                CurrentContent = value is null ? null : _views.Create(value.Tab);
            }
        }
    }



    public MainWindowViewModel(
        IViewFactory views,
        PluginManager pluginManager,
        IDocumentRepository repo)
    {
        _views = views;
        _repo = repo;

        Tabs = new ObservableCollection<TabButtonViewModel>(
            pluginManager.AllTabs.Select(tab => new TabButtonViewModel(tab)));

        SelectedTab = Tabs.FirstOrDefault();

        ReloadTiles();
    }

    private void ReloadTiles()
    {
        Items.Clear();

        // Demo paging - you can later replace with incremental loading
        foreach (var t in _repo.QueryTiles(Query, limit: 5000, offset: 0))
        {
            Items.Add(new DocumentTileVm(t.Id, t.Title, t.Kind, t.Subtitle));
        }
    }

    [RelayCommand]
    private void TogglePane() => IsPaneOpen = !IsPaneOpen;
}

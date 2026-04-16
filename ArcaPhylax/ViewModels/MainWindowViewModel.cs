using ArcaPhylax.Abstractions;
using ArcaPhylax.Composition;
using ArcaPhylax.Data;
using ArcaPhylax.PluginSystem;
using Avalonia.Controls;
using System.Collections.ObjectModel;

namespace ArcaPhylax.ViewModels;

public sealed class MainWindowViewModel : NotifyBase
{
    private readonly IViewFactory _views;
    private readonly IDocumentRepository _repo;

    public ObservableCollection<DocumentTileVm> Items { get; } = new();

    public string? Query
    {
        get => field;
        set
        {
            if (!Set(ref field, value))
            {
                return;
            }

            ReloadTiles();
        }
    }

    private Control? _currentContent;
    public Control? CurrentContent { get => _currentContent; private set => Set(ref _currentContent, value); }

    public ObservableCollection<TabButtonViewModel> Tabs { get; }

    public ITabContentViewModel? SelectedTab
    {
        get => field;
        set
        {
            if (Set(ref field, value))
            {
                CurrentContent = value is null ? null : _views.Create(value);

                // keep button state in sync
                foreach (var tab in Tabs)
                {
                    tab.IsSelected = ReferenceEquals(tab.Tab, value);
                }
            }
        }
    }



    public MainWindowViewModel(
        IViewFactory views,
        PluginManager pluginManager,
        IDbInitializer dbInit,
        IDocumentRepository repo)
    {
        _views = views;
        _repo = repo;

        Tabs = new ObservableCollection<TabButtonViewModel>(pluginManager.AllTabs.Select(Mushelbar()));

        SelectedTab = Tabs.FirstOrDefault()?.Tab;

        dbInit.EnsureCreatedAndSeed();

        ReloadTiles();
    }

    private Func<ITabContentViewModel, TabButtonViewModel> Mushelbar()
    {
        return tab => new TabButtonViewModel(tab, tab => SelectedTab = tab);
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
}

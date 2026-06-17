using ArcaPhylax.Abstractions;
using ArcaPhylax.Data;
using System.Collections.ObjectModel;

namespace ArcaPhylax.ViewModels;

/// <summary>The Dashboard page: what ArcaPhylax is and what it is built on.</summary>
internal sealed class DashboardViewModel : ViewModelBase, ITabContentViewModel
{
    private readonly IDocumentRepository _repo;

    public DashboardViewModel(IDocumentRepository repo)
    {
        _repo = repo;

        ReloadTiles();
    }

    public string Title => "Dashboard";

    public string Icon => NavigationIcons.Dashboard;

    public int NavigationWeight => 10;

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

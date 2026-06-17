using System.Collections.ObjectModel;
using ArcaPhylax.Abstractions;
using Avalonia.Media.Imaging;
using CharacterArchivePlugin.Data;
using CharacterArchivePlugin.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CharacterArchivePlugin.ViewModels;

/// <summary>
/// Character Archive tab: a search box over a thumbnail grid (left) with a description detail
/// pane (right). Searching is debounced; results and thumbnails load off the UI thread.
/// </summary>
// Derives from CommunityToolkit's ObservableObject directly (not the shared ViewModelBase): the
// [ObservableProperty] source generator emits calls to ObservableObject's protected members, which
// must resolve to the plugin's own CommunityToolkit.Mvvm copy. Inheriting ViewModelBase (in the
// shared Abstractions assembly) binds the base to the host's copy instead, causing a cross-context
// MethodAccessException when a generated setter runs.
public sealed partial class CharacterArchiveViewModel : ObservableObject, ITabContentViewModel
{
    private const int ResultLimit = 2000;
    private const int ThumbnailWidth = 160;
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(300);

    private readonly ICharacterRepository _repository;
    private readonly IImageStore _images;

    private CancellationTokenSource? _searchCts;

    public CharacterArchiveViewModel(ICharacterRepository repository, IImageStore images)
    {
        _repository = repository;
        _images = images;
    }

    public string Title => "Character Archive";
    public string Icon => CharacterArchiveIcons.Character;
    public int NavigationWeight => 25;

    public ObservableCollection<CharacterTileVm> Results { get; } = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string? _status;

    [ObservableProperty]
    private CharacterTileVm? _selectedTile;

    [ObservableProperty]
    private Character? _selectedDetail;

    [ObservableProperty]
    private Bitmap? _selectedImage;

    public bool HasSelection => SelectedDetail is not null;

    partial void OnSearchTextChanged(string value) => _ = DebouncedSearchAsync();

    partial void OnSelectedTileChanged(CharacterTileVm? value) => _ = LoadDetailAsync(value);

    partial void OnSelectedDetailChanged(Character? value) => OnPropertyChanged(nameof(HasSelection));

    private async Task DebouncedSearchAsync()
    {
        _searchCts?.Cancel();
        var cts = new CancellationTokenSource();
        _searchCts = cts;

        try
        {
            await Task.Delay(DebounceDelay, cts.Token);
            await RunSearchAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer keystroke.
        }
    }

    [RelayCommand]
    private Task Search() => RunSearchAsync(CancellationToken.None);

    private async Task RunSearchAsync(CancellationToken cancellationToken)
    {
        Status = "Searching…";

        try
        {
            var tiles = await Task.Run(() => _repository.Search(SearchText, ResultLimit).ToList(), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            Results.Clear();
            foreach (var tile in tiles)
            {
                Results.Add(new CharacterTileVm(tile, _images));
            }

            Status = Results.Count == 0 ? "No matches." : $"{Results.Count} result(s).";

            await LoadThumbnailsAsync(Results.ToList(), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // A newer search replaced this one.
        }
        catch (Exception ex)
        {
            Status = $"Search failed: {ex.Message}";
        }
    }

    private static async Task LoadThumbnailsAsync(IReadOnlyList<CharacterTileVm> tiles, CancellationToken cancellationToken)
    {
        foreach (var tile in tiles)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await tile.LoadThumbnailAsync(ThumbnailWidth, cancellationToken);
        }
    }

    private async Task LoadDetailAsync(CharacterTileVm? tile)
    {
        if (tile is null)
        {
            SelectedDetail = null;
            SelectedImage = null;
            return;
        }

        try
        {
            SelectedDetail = await Task.Run(() => _repository.ById(tile.Key).Match<Character?>(() => null, character => character));
            SelectedImage = await _images.LoadFullAsync(tile.Hash);
        }
        catch (Exception ex)
        {
            Status = $"Failed to load character: {ex.Message}";
        }
    }
}

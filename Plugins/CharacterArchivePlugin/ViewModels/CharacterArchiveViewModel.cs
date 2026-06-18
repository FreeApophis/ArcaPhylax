using System.Collections.ObjectModel;
using ArcaPhylax.Abstractions;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CharacterArchivePlugin.Data;
using CharacterArchivePlugin.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Funcky.Monads;

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
    private const int TagPickerLimit = 20;
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(300);

    private readonly ICharacterRepository _repository;
    private readonly IImageStore _images;

    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _tagFilterCts;

    public CharacterArchiveViewModel(ICharacterRepository repository, IImageStore images)
    {
        _repository = repository;
        _images = images;
    }

    public string Title => "Character Archive";
    public string Icon => CharacterArchiveIcons.Character;
    public int NavigationWeight => 25;

    /// <summary>Set by the view so the download save picker has a parent window.</summary>
    public TopLevel? HostTopLevel { get; set; }

    public ObservableCollection<CharacterTileVm> Results { get; } = new();

    /// <summary>Active tag filters; results must carry every one (AND).</summary>
    public ObservableCollection<string> SelectedTags { get; } = new();

    /// <summary>Pickable tags (popular, or matching <see cref="TagFilter"/>), excluding selected ones.</summary>
    public ObservableCollection<TagOption> AvailableTags { get; } = new();

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TagFilter { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Status { get; set; }

    [ObservableProperty]
    public partial CharacterTileVm? SelectedTile { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    public partial Character? SelectedDetail { get; set; }

    [ObservableProperty]
    public partial Bitmap? SelectedImage { get; set; }

    [ObservableProperty]
    public partial CardSpec? SelectedSpec { get; set; }

    public bool HasSelection => SelectedDetail is not null;

    /// <summary>Drives the V1/V2 ribbon overlay on the selected card's image.</summary>
    public bool IsSelectedV1 => SelectedSpec == CardSpec.V1;
    public bool IsSelectedV2 => SelectedSpec == CardSpec.V2;

    partial void OnSearchTextChanged(string value) => _ = DebouncedSearchAsync();

    partial void OnSelectedTileChanged(CharacterTileVm? value) => _ = LoadDetailAsync(value);

    partial void OnSelectedDetailChanged(Character? value) => OnPropertyChanged(nameof(HasSelection));

    partial void OnSelectedSpecChanged(CardSpec? value)
    {
        OnPropertyChanged(nameof(IsSelectedV1));
        OnPropertyChanged(nameof(IsSelectedV2));
    }

    partial void OnTagFilterChanged(string value) => _ = DebouncedTagReloadAsync();

    /// <summary>Loads the initial tag picker the first time the tab is shown.</summary>
    public Task InitializeAsync() =>
        AvailableTags.Count == 0 ? LoadAvailableTagsAsync(CancellationToken.None) : Task.CompletedTask;

    [RelayCommand]
    private async Task AddTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || SelectedTags.Contains(tag))
        {
            return;
        }

        SelectedTags.Add(tag);
        await Task.WhenAll(RunSearchAsync(CancellationToken.None), LoadAvailableTagsAsync(CancellationToken.None));
    }

    [RelayCommand]
    private async Task RemoveTag(string? tag)
    {
        if (tag is null || !SelectedTags.Remove(tag))
        {
            return;
        }

        await Task.WhenAll(RunSearchAsync(CancellationToken.None), LoadAvailableTagsAsync(CancellationToken.None));
    }

    [RelayCommand]
    private async Task ClearTags()
    {
        if (SelectedTags.Count == 0)
        {
            return;
        }

        SelectedTags.Clear();
        await Task.WhenAll(RunSearchAsync(CancellationToken.None), LoadAvailableTagsAsync(CancellationToken.None));
    }

    private async Task DebouncedTagReloadAsync()
    {
        _tagFilterCts?.Cancel();
        var cts = new CancellationTokenSource();
        _tagFilterCts = cts;

        try
        {
            await Task.Delay(DebounceDelay, cts.Token);
            await LoadAvailableTagsAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer keystroke.
        }
    }

    private async Task LoadAvailableTagsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tags = await Task.Run(() => _repository.PopularTags(TagFilter, TagPickerLimit).ToList(), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var selected = SelectedTags.ToHashSet();

            AvailableTags.Clear();
            foreach (var tag in tags.Where(tag => !selected.Contains(tag.Tag)))
            {
                AvailableTags.Add(tag);
            }
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer reload.
        }
        catch (Exception ex)
        {
            Status = $"Tag load failed: {ex.Message}";
        }
    }

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
            var tags = SelectedTags.ToArray();
            var tiles = await Task.Run(() => _repository.Search(SearchText, tags, ResultLimit).ToList(), cancellationToken);
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
            SelectedSpec = null;
            return;
        }

        try
        {
            SelectedDetail = await Task.Run(() => _repository.ById(tile.Key).Match<Character?>(() => null, character => character));
            SelectedSpec = await Task.Run(() => _repository.DefinitionByKey(tile.Key).Match(() => CardSpec.V1, CardSpecClassifier.Classify));
            SelectedImage = await _images.LoadFullAsync(tile.Hash);
        }
        catch (Exception ex)
        {
            Status = $"Failed to load character: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task Download()
    {
        if (SelectedTile is not { } tile || SelectedDetail is not { } detail)
        {
            return;
        }

        if (HostTopLevel?.StorageProvider is not { } storage)
        {
            Status = "No storage provider available.";
            return;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Download Character Card",
            SuggestedFileName = $"{SafeFileName(detail.Name)}.png",
            DefaultExtension = "png",
            FileTypeChoices = [new FilePickerFileType("PNG Character Card") { Patterns = ["*.png"] }],
        });

        if (file is null)
        {
            return;
        }

        Status = "Building character card…";
        try
        {
            var card = await BuildCardAsync(tile.Key, tile.Hash, CancellationToken.None);
            if (card.Match<byte[]?>(none: () => null, some: bytes => bytes) is not { } bytes)
            {
                Status = "Character definition or image is unavailable.";
                return;
            }

            await using var stream = await file.OpenWriteAsync();
            await stream.WriteAsync(bytes);
            Status = $"Saved {Path.GetFileName(file.Path.LocalPath)}.";
        }
        catch (Exception ex)
        {
            Status = $"Download failed: {ex.Message}";
        }
    }

    private async Task<Option<byte[]>> BuildCardAsync(string key, string hash, CancellationToken cancellationToken)
    {
        var definition = await Task.Run(() => _repository.DefinitionByKey(key), cancellationToken);
        var imageBytes = await _images.ReadBytesAsync(hash, cancellationToken);

        var inputs =
            from json in definition
            from bytes in Option.FromNullable(imageBytes)
            select (json, bytes);

        return await inputs.Match(
            none: () => Task.FromResult(Option<byte[]>.None),
            some: pair => Task.Run(
                () => Option.Some(CharacterCardWriter.EmbedCharacterJson(pair.bytes, CardDefinitionNormalizer.Normalize(pair.json))),
                cancellationToken));
    }

    private static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Trim().Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "character" : cleaned;
    }
}

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using ArcaPhylax.Abstractions;
using ArcaPhylax.CharacterCards.Cards;
using ArcaPhylax.CharacterCards.Png;
using Avalonia.Media.Imaging;
using CharacterPlugin.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Funcky.Extensions;
using Funcky.Monads;

namespace CharacterPlugin.ViewModels;

/// <summary>
/// Character Editor tab: lists TavernCard PNGs from the configured cards directory (left) and edits the
/// core fields common to V1 and V2 cards (right). Saving merges the edits back onto the card's original
/// definition — preserving every field the editor does not touch — and re-embeds it into the PNG.
/// </summary>
// Derives from CommunityToolkit's ObservableObject directly (not the shared ViewModelBase) so the
// [ObservableProperty] source generator binds to the plugin's own CommunityToolkit.Mvvm copy — see the
// note on CharacterArchiveViewModel and CLAUDE.md's plugin-isolation section.
public sealed partial class CharacterEditorViewModel(CharacterEditorSettings settings) : ObservableObject, ITabContentViewModel
{
    // Decoded well above the ~130px display box so tiles stay crisp on high-DPI displays.
    private const int ThumbnailWidth = 300;

    private readonly CharacterEditorSettings _settings = settings;

    // Held for the selected card so a save preserves every field the editor does not surface.
    private string _originalJson = string.Empty;
    private byte[] _originalImage = [];
    private CardSpec _loadedSpec = CardSpec.V2;

    // The version stored by the last save; the baseline that decides whether a save bumps the revision.
    private string _savedVersion = string.Empty;

    public string Title => "Character Editor";

    public string Icon => CharacterEditorIcons.Editor;

    public int NavigationWeight => 26;

    public ObservableCollection<CardListItem> Cards { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial CardListItem? SelectedCard { get; set; }

    [ObservableProperty]
    public partial Bitmap? Image { get; set; }

    [ObservableProperty]
    public partial CardSpec? Spec { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Personality { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Scenario { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FirstMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MessageExample { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CharacterVersion { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Creator { get; set; } = string.Empty;

    /// <summary>Comma-separated tag list, split on save.</summary>
    [ObservableProperty]
    public partial string TagsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Status { get; set; }

    public bool HasSelection => SelectedCard is not null;

    public bool IsV1 => Spec == CardSpec.V1;

    public bool IsV2 => Spec == CardSpec.V2;

    /// <summary>Loads the card list the first time the tab is shown.</summary>
    public Task InitializeAsync()
        => Cards.Count == 0 ? ReloadAsync() : Task.CompletedTask;

    partial void OnSelectedCardChanged(CardListItem? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        _ = LoadCardAsync(value);
    }

    partial void OnSpecChanged(CardSpec? value)
    {
        OnPropertyChanged(nameof(IsV1));
        OnPropertyChanged(nameof(IsV2));
    }

    [RelayCommand]
    private async Task Reload()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var directory = _settings.CardsDirectory;
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            Cards.Clear();
            SelectedCard = null;
            Status = "Set a cards directory in the Configure page.";
            return;
        }

        Status = "Loading cards…";
        try
        {
            var items = await Task.Run(() => LoadCards(directory));

            Cards.Clear();
            foreach (var item in items)
            {
                Cards.Add(item);
            }

            Status = Cards.Count == 0 ? "No character cards found." : $"{Cards.Count} card(s).";
        }
        catch (Exception ex)
        {
            Status = $"Failed to list cards: {ex.Message}";
        }
    }

    // Every *.png in the directory that actually carries a chara chunk, named by its card name.
    private static List<CardListItem> LoadCards(string directory) =>
        Directory.EnumerateFiles(directory, "*.png")
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .WhereSelect(CardItemOrNone)
            .ToList();

    private static Option<CardListItem> CardItemOrNone(string path) =>
        ItemFrom(path, File.ReadAllBytes(path));

    // One read per file feeds both the chara-chunk name and the gallery thumbnail.
    private static Option<CardListItem> ItemFrom(string path, byte[] bytes) =>
        CharacterCardPng.ReadCharacterJson(bytes)
            .Select(json => new CardListItem(
                path,
                Path.GetFileName(path),
                CharacterCardJson.Read(json).Name,
                DecodeThumbnail(bytes)));

    private async Task LoadCardAsync(CardListItem? item)
    {
        if (item is null)
        {
            ClearFields();
            return;
        }

        try
        {
            var (json, bytes, image) = await Task.Run(() =>
            {
                var raw = File.ReadAllBytes(item.Path);
                var definition = CharacterCardPng.ReadCharacterJson(raw).Match(() => string.Empty, json => json);
                return (definition, raw, DecodeBitmap(raw));
            });

            _originalJson = json;
            _originalImage = bytes;

            var card = CharacterCardJson.Read(json);
            _loadedSpec = card.Spec;

            Spec = card.Spec;
            Name = card.Name;
            Description = card.Description;
            Personality = card.Personality;
            Scenario = card.Scenario;
            FirstMessage = card.FirstMessage;
            MessageExample = card.MessageExample;
            CharacterVersion = card.CharacterVersion;
            _savedVersion = card.CharacterVersion;
            Creator = card.Creator;
            TagsText = string.Join(", ", card.Tags);
            Image = image;
            Status = $"Loaded {item.FileName}.";
        }
        catch (Exception ex)
        {
            Status = $"Failed to load card: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task Save()
    {
        if (SelectedCard is not { } item)
        {
            return;
        }

        Status = "Saving…";
        try
        {
            // Bump the version/revision before persisting, then reflect it back into the editor.
            var nextVersion = CardVersion.Next(CharacterVersion, _savedVersion);
            CharacterVersion = nextVersion;

            var edited = new CharacterCard(
                _loadedSpec,
                Name,
                Description,
                Personality,
                Scenario,
                FirstMessage,
                MessageExample,
                nextVersion,
                Creator,
                ParseTags(TagsText));

            var original = _originalJson;
            var image = _originalImage;
            var (bytes, merged) = await Task.Run(() =>
            {
                var definition = CardDefinitionNormalizer.Normalize(CharacterCardJson.Apply(original, edited));
                return (CharacterCardPng.EmbedCharacterJson(image, definition), definition);
            });

            await File.WriteAllBytesAsync(item.Path, bytes);

            // Adopt the saved bytes/definition as the new baseline so a further edit merges onto them.
            _originalImage = bytes;
            _originalJson = merged;
            _savedVersion = nextVersion;
            _loadedSpec = CardSpec.V2;
            Spec = CardSpec.V2;
            item.Name = Name;
            Status = $"Saved {item.FileName}.";
        }
        catch (Exception ex)
        {
            Status = $"Save failed: {ex.Message}";
        }
    }

    private void ClearFields()
    {
        _originalJson = string.Empty;
        _originalImage = [];
        _savedVersion = string.Empty;
        Spec = null;
        Name = string.Empty;
        Description = string.Empty;
        Personality = string.Empty;
        Scenario = string.Empty;
        FirstMessage = string.Empty;
        MessageExample = string.Empty;
        CharacterVersion = string.Empty;
        Creator = string.Empty;
        TagsText = string.Empty;
        Image = null;
    }

    private static ImmutableArray<string> ParseTags(string text) =>
        text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToImmutableArray();

    private static Bitmap DecodeBitmap(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return new Bitmap(stream);
    }

    private static Bitmap? DecodeThumbnail(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            return Bitmap.DecodeToWidth(stream, ThumbnailWidth);
        }
        catch
        {
            // Undecodable image shows as a blank thumbnail rather than dropping the card.
            return null;
        }
    }
}

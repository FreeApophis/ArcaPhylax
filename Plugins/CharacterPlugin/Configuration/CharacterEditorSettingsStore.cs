using System.Text.Json;

namespace CharacterPlugin.Configuration;

/// <summary>
/// Loads and saves <see cref="CharacterEditorSettings"/> to a small JSON file next to the app. Mirrors
/// the CharacterArchive plugin's own-persistence approach — the host has no settings store yet.
/// </summary>
public sealed class CharacterEditorSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path =
        Path.Combine(AppContext.BaseDirectory, "character-editor.settings.json");

    public void Load(CharacterEditorSettings settings)
    {
        if (!File.Exists(_path))
        {
            return;
        }

        var persisted = JsonSerializer.Deserialize<Persisted>(File.ReadAllText(_path));
        if (persisted is not null && !string.IsNullOrWhiteSpace(persisted.CardsDirectory))
        {
            settings.CardsDirectory = persisted.CardsDirectory;
        }
    }

    public void Save(CharacterEditorSettings settings) =>
        File.WriteAllText(_path, JsonSerializer.Serialize(new Persisted(settings.CardsDirectory), JsonOptions));

    private sealed record Persisted(string CardsDirectory);
}

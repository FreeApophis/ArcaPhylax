using System.Text.Json;

namespace CharacterArchivePlugin.Configuration;

/// <summary>
/// Loads and saves <see cref="CharacterArchiveSettings"/> to a small JSON file next to the app.
/// The host has no settings persistence yet, so the plugin owns its own.
/// </summary>
public sealed class CharacterArchiveSettingsStore
{
    private sealed record Persisted(string ConnectionString, string ImageRoot);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path =
        Path.Combine(AppContext.BaseDirectory, "character-archive.settings.json");

    public void Load(CharacterArchiveSettings settings)
    {
        if (!File.Exists(_path))
        {
            return;
        }

        var persisted = JsonSerializer.Deserialize<Persisted>(File.ReadAllText(_path));
        if (persisted is null)
        {
            return;
        }

        settings.ConnectionString = persisted.ConnectionString;
        if (!string.IsNullOrWhiteSpace(persisted.ImageRoot))
        {
            settings.ImageRoot = persisted.ImageRoot;
        }
    }

    public void Save(CharacterArchiveSettings settings) =>
        File.WriteAllText(
            _path,
            JsonSerializer.Serialize(new Persisted(settings.ConnectionString, settings.ImageRoot), JsonOptions));
}

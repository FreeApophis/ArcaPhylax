using System.Text.Json;

namespace CharacterArchivePlugin.Configuration;

/// <summary>
/// Loads and saves <see cref="CharacterArchiveSettings"/> to a small JSON file next to the app.
/// The host has no settings persistence yet, so the plugin owns its own.
/// </summary>
public sealed class CharacterArchiveSettingsStore
{
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

        settings.DatabaseHost = persisted.DatabaseHost;
        settings.DatabasePort = persisted.DatabasePort;
        settings.DatabaseName = persisted.DatabaseName;
        settings.DatabaseUsername = persisted.DatabaseUsername;
        settings.DatabasePassword = persisted.DatabasePassword;
        if (!string.IsNullOrWhiteSpace(persisted.ImageRoot))
        {
            settings.ImageRoot = persisted.ImageRoot;
        }
    }

    public void Save(CharacterArchiveSettings settings) =>
        File.WriteAllText(
            _path,
            JsonSerializer.Serialize(new Persisted(settings.DatabaseHost, settings.DatabasePort, settings.DatabaseName, settings.DatabaseUsername, settings.DatabasePassword, settings.ImageRoot), JsonOptions));

    private sealed record Persisted(
        string DatabaseHost,
        int DatabasePort,
        string DatabaseName,
        string DatabaseUsername,
        string DatabasePassword,
        string ImageRoot);
}

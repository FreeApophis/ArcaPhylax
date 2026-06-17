using CommunityToolkit.Mvvm.ComponentModel;

namespace CharacterArchivePlugin.Configuration;

/// <summary>
/// Session-wide CharacterArchive settings shared between the tab and the configuration section.
/// Persisted by <see cref="CharacterArchiveSettingsStore"/>.
/// </summary>
public sealed partial class CharacterArchiveSettings : ObservableObject
{
    /// <summary>Npgsql connection string for the <c>char_archive</c> database supplied by the user.</summary>
    [ObservableProperty]
    private string _connectionString = string.Empty;

    /// <summary>Root of the sharded <c>hashed-data</c> image tree.</summary>
    [ObservableProperty]
    private string _imageRoot = @"C:\_Software\character-archive\hashed-data";
}

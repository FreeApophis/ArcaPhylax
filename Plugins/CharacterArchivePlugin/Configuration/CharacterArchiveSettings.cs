using CommunityToolkit.Mvvm.ComponentModel;

namespace CharacterArchivePlugin.Configuration;

/// <summary>
/// Session-wide CharacterArchive settings shared between the tab and the configuration section.
/// Persisted by <see cref="CharacterArchiveSettingsStore"/>.
/// </summary>
public sealed partial class CharacterArchiveSettings : ObservableObject
{
    public string ConnectionString
        => $"Host={DatabaseHost};Port={DatabasePort};Database={DatabaseName};Username={DatabaseUsername};Password={DatabasePassword}";

    /// <summary>Root of the sharded <c>hashed-data</c> image tree.</summary>
    [ObservableProperty]
    public partial string ImageRoot { get; set; } = @"C:\_Software\character-archive\hashed-data";


    [ObservableProperty]
    public partial string DatabaseHost { get; set; } = "localhost";

    [ObservableProperty]
    public partial int DatabasePort { get; set; } = 5432;

    [ObservableProperty]
    public partial string DatabaseName { get; set; } = "char_archive";

    [ObservableProperty]
    public partial string DatabaseUsername { get; set; } = "postgres";

    [ObservableProperty]
    public partial string DatabasePassword { get; set; } = string.Empty;
}

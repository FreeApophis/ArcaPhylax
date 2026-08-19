using CommunityToolkit.Mvvm.ComponentModel;

namespace CharacterPlugin.Configuration;

/// <summary>
/// Session-wide Character Editor settings shared between the editor tab and its configuration section.
/// Persisted by <see cref="CharacterEditorSettingsStore"/>.
/// </summary>
public sealed partial class CharacterEditorSettings : ObservableObject
{
    /// <summary>Folder the editor lists TavernCard PNGs from and saves edits back into.</summary>
    [ObservableProperty]
    public partial string CardsDirectory { get; set; } = string.Empty;
}

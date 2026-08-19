namespace CharacterPlugin.ViewModels;

/// <summary>
/// Filled SVG-path geometry for the Character Editor nav entry. Lives inside the plugin so it owns its
/// own icon rather than depending on the host.
/// </summary>
public static class CharacterEditorIcons
{
    /// <summary>A pencil over a card — editing a character card.</summary>
    public const string Editor =
        "M4,3 H15 A1,1 0 0 1 16,4 V10 H14 V5 H5 V19 H11 V21 H4 A1,1 0 0 1 3,20 V4 A1,1 0 0 1 4,3 Z " +
        "M20.7,12.3 L21.7,13.3 A1,1 0 0 1 21.7,14.7 L15.4,21 L12,22 L13,18.6 L19.3,12.3 A1,1 0 0 1 20.7,12.3 Z";
}

namespace CharacterArchivePlugin.ViewModels;

/// <summary>
/// Filled SVG-path geometry for the CharacterArchive nav entry. Lives inside the plugin assembly
/// so the plugin owns its own icon rather than depending on the host.
/// </summary>
public static class CharacterArchiveIcons
{
    /// <summary>A simple portrait bust — head over shoulders.</summary>
    public const string Character =
        "M12,12 A5,5 0 1 0 12,2 A5,5 0 0 0 12,12 Z " +
        "M3,22 C3,16.5 7,14 12,14 C17,14 21,16.5 21,22 Z";
}

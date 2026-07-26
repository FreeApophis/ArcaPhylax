namespace CharacterArchivePlugin.Data;

/// <summary>
/// Full character detail shown in the right-hand pane. <see cref="Key"/> is a synthetic
/// <c>source:primaryKey</c> identifier (the source def tables have heterogeneous primary keys).
/// </summary>
public sealed record Character(
    string Key,
    string Source,
    string Name,
    string Author,
    string Summary,
    string Description,
    string ImageHash,
    string[] Tags);

/// <summary>Lightweight projection for a single character (no description). Used for the variant
/// switcher listing every row that shares one image.</summary>
public sealed record CharacterTile(string Key, string Source, string Name, string ImageHash);

/// <summary>
/// One result-grid entry: the most-complete representative of every row sharing <see cref="ImageHash"/>,
/// plus how many rows the stack collapses (<see cref="VariantCount"/> is 1 for a lone character).
/// </summary>
public sealed record CharacterStack(string Key, string Source, string Name, string ImageHash, int VariantCount);

/// <summary>A tag and how many characters carry it, for the tag-filter picker.</summary>
public sealed record TagOption(string Tag, long Count)
{
    /// <summary>Compact count badge: <c>1.2k</c> for 1234, the raw number below 1000.</summary>
    public string CountLabel => Count >= 1000 ? $"{Count / 1000d:0.#}k" : Count.ToString();
}

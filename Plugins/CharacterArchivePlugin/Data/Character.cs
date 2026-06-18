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

/// <summary>Lightweight projection for the result grid (no description).</summary>
public sealed record CharacterTile(string Key, string Source, string Name, string ImageHash);

/// <summary>A tag and how many characters carry it, for the tag-filter picker.</summary>
public sealed record TagOption(string Tag, long Count)
{
    /// <summary>Compact count badge: <c>1.2k</c> for 1234, the raw number below 1000.</summary>
    public string CountLabel => Count >= 1000 ? $"{Count / 1000d:0.#}k" : Count.ToString();
}

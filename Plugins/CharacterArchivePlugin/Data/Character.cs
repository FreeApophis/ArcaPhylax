namespace CharacterArchivePlugin.Data;

/// <summary>
/// Full character detail shown in the right-hand pane. <see cref="Key"/> is a synthetic
/// <c>source:primaryKey</c> identifier (the source def tables have heterogeneous primary keys).
/// </summary>
public sealed record Character(string Key, string Source, string Name, string Description, string ImageHash);

/// <summary>Lightweight projection for the result grid (no description).</summary>
public sealed record CharacterTile(string Key, string Source, string Name, string ImageHash);

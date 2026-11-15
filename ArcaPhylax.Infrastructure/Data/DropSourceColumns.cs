namespace ArcaPhylax.Data;

/// <summary>
/// The flat <c>Kind</c> / <c>OriginalPath</c> / <c>StoredPath</c> columns a <see cref="DropSource"/> is
/// persisted as. Only the persistence edge deals in this shape; everything above it uses
/// <see cref="DropSource"/>.
/// </summary>
public sealed record DropSourceColumns(string Kind, string? OriginalPath, string? StoredPath)
{
    private const string FolderKind = "Folder";
    private const string FileKind = "File";

    public static DropSourceColumns Of(DropSource source) => source switch
    {
        LinkedFolder folder => new(FolderKind, folder.Path, null),
        LinkedFile file => new(FileKind, file.Path, null),
        CachedFile file => new(FileKind, null, file.StoredPath),
    };

    /// <summary>The source these columns describe, or None when they are not a valid combination.</summary>
    public Option<DropSource> ToDropSource() => (Kind, OriginalPath, StoredPath) switch
    {
        (FolderKind, { } path, null) => new LinkedFolder(path),
        (FileKind, { } path, null) => new LinkedFile(path),
        (FileKind, null, { } storedPath) => new CachedFile(storedPath),
        _ => Option<DropSource>.None,
    };
}

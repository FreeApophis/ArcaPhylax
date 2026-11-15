using ArcaPhylax.Data;

namespace ArcaPhylax.ViewModels;

public sealed record DroppedItemRowVm(
    long Id,
    string Kind,
    string Name,
    string Path,
    string CreatedUtc)
{
    public static DroppedItemRowVm Of(DroppedItem item)
        => new(item.Id, KindLabel(item.Source), item.Name, PathOf(item.Source), item.CreatedUtc);

    private static string KindLabel(DropSource source) => source switch
    {
        LinkedFolder => "Folder",
        LinkedFile => "File",
        CachedFile => "Cached file",
    };

    private static string PathOf(DropSource source) => source switch
    {
        LinkedFolder folder => folder.Path,
        LinkedFile file => file.Path,
        CachedFile file => file.StoredPath,
    };
}

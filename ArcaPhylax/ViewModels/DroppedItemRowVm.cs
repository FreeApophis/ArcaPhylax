namespace ArcaPhylax.ViewModels;

public sealed record DroppedItemRowVm(
    long Id,
    string Kind,
    string Name,
    string? OriginalPath,
    string? StoredPath,
    string CreatedUtc);

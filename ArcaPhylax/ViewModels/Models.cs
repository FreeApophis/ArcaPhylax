namespace ArcaPhylax.ViewModels;

public sealed record DocumentTileVm(
    long Id,
    string Title,
    string Kind,
    string? Subtitle);

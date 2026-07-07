namespace ArcaPhylax.Data;

public sealed record IndexJob(
    string Kind,
    string Name,
    string? OriginalPath,
    string? StoredPath);

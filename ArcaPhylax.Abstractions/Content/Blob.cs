namespace ArcaPhylax.Abstractions.Content;

public record Blob(
    long    Id,
    long    ItemId,
    string  Role,
    string  MimeType,
    long    SizeBytes,
    string  StorageUri,
    string? ContentHash);

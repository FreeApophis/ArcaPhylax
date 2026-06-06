namespace ArcaPhylax.Abstractions.Content;

public record Item(
    long    Id,
    string  Kind,
    string  Title,
    string  CreatedAt,
    string  ImportedAt,
    string? ContentHash);

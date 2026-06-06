namespace ArcaPhylax.Abstractions.Content;

public record ItemSet(
    long    Id,
    string  Name,
    string? Description,
    bool    IsSmart,
    string  CreatedAt);

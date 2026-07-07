namespace ArcaPhylax.Data;

public record Document(
    long Id,
    string Title,
    string Kind,
    string? Subtitle);

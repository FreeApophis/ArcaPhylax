namespace ArcaPhylax.Abstractions.Content;

public record Tag(
    long   Id,
    string Name,
    long?  GroupId);

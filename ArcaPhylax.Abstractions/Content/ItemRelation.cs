namespace ArcaPhylax.Abstractions.Content;

public record ItemRelation(
    long   Id,
    long   FromItemId,
    long   ToItemId,
    string RelationType);

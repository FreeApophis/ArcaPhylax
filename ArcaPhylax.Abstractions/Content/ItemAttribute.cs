namespace ArcaPhylax.Abstractions.Content;

public record ItemAttribute(
    long ItemId,
    long AttrId,
    string? ValueText,
    long? ValueInt,
    double? ValueReal,
    string? ValueTs);

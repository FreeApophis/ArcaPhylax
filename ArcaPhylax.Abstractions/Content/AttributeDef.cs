namespace ArcaPhylax.Abstractions.Content;

public record AttributeDef(
    long    Id,
    string  Name,
    string  ValueType,
    string? AppliesTo);

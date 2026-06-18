namespace ArcaPhylax.Abstractions.Content;

public record SetRule(
    long Id,
    long SetId,
    string Field,
    string Operator,
    string Value);

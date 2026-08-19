using System.Text.Json;

namespace CharacterArchivePlugin.Data;

/// <summary>
/// Extracts the free-text roleplay fields — personality, scenario, greeting (<c>first_mes</c>),
/// the author-supplied <c>character_version</c> and <c>creator_notes</c> — from a raw card
/// <c>definition</c>. V2/v3 cards nest
/// these under <c>data</c>; V1 cards keep them flat at the top level. Anything unparseable, absent,
/// or non-string yields an empty string.
/// </summary>
public static class CardFieldExtractor
{
    public static CardFields Extract(string? definitionJson)
    {
        if (string.IsNullOrWhiteSpace(definitionJson))
        {
            return CardFields.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(definitionJson);
            var source = FieldSource(document.RootElement);
            return new CardFields(
                StringField(source, "personality"),
                StringField(source, "scenario"),
                StringField(source, "first_mes"),
                StringField(source, "character_version"),
                StringField(source, "creator_notes"));
        }
        catch (JsonException)
        {
            return CardFields.Empty;
        }
    }

    // V2/v3 cards carry the fields inside `data`; V1 cards keep them at the root.
    private static JsonElement FieldSource(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty("data", out var data)
        && data.ValueKind == JsonValueKind.Object
            ? data
            : root;

    private static string StringField(JsonElement source, string name) =>
        source.ValueKind == JsonValueKind.Object
        && source.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
}

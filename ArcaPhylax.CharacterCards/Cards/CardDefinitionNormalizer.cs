using System.Text.Json;
using System.Text.Json.Nodes;

namespace ArcaPhylax.CharacterCards.Cards;

/// <summary>
/// Makes a stored V2 card <c>definition</c> conform to the TavernCard V2 schema before export. Some
/// archived cards carry <c>null</c> where the spec demands a value — most commonly
/// <c>data.character_book: null</c> (the field is <c>CharacterBook | undefined</c>, never null) and
/// occasionally null arrays. Lenient readers ignore this; strict editors reject the whole card. The
/// pass only touches offending <c>null</c>s and leaves every other byte of meaning intact; V1 cards
/// and anything unparseable are returned unchanged.
/// </summary>
public static class CardDefinitionNormalizer
{
    public static string Normalize(string? definitionJson)
    {
        if (string.IsNullOrWhiteSpace(definitionJson))
        {
            return definitionJson ?? string.Empty;
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(definitionJson);
        }
        catch (JsonException)
        {
            return definitionJson;
        }

        return root is JsonObject card && IsV2(card) && card["data"] is JsonObject data
            ? NormalizedCard(root, data)
            : definitionJson;
    }

    private static string NormalizedCard(JsonNode root, JsonObject data)
    {
        CoerceNullToEmpty(data, "tags", () => new JsonArray());
        CoerceNullToEmpty(data, "alternate_greetings", () => new JsonArray());
        CoerceNullToEmpty(data, "extensions", () => new JsonObject());

        // character_book is optional; a null value is invalid, so drop it rather than inventing one.
        if (data.TryGetPropertyValue("character_book", out var book) && book is null)
        {
            data.Remove("character_book");
        }

        return root.ToJsonString();
    }

    private static void CoerceNullToEmpty(JsonObject data, string field, Func<JsonNode> empty)
    {
        if (data.TryGetPropertyValue(field, out var value) && value is null)
        {
            data[field] = empty();
        }
    }

    private static bool IsV2(JsonObject card) =>
        card["spec"] is JsonValue spec
        && spec.TryGetValue(out string? value)
        && (value ?? string.Empty).StartsWith("chara_card_v", StringComparison.OrdinalIgnoreCase);
}

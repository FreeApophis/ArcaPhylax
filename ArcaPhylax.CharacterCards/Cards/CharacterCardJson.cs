using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ArcaPhylax.CharacterCards.Cards;

/// <summary>
/// Reads a card <c>definition</c> into an editable <see cref="CharacterCard"/> and merges an edited
/// card back onto the original JSON. V2/v3 cards carry the core fields under <c>data</c>; V1 cards keep
/// them flat at the root. <see cref="Apply"/> mutates only the fields the editor owns and leaves every
/// other field of the original definition intact, so unknown extensions survive a round-trip.
/// </summary>
public static class CharacterCardJson
{
    /// <summary>Projects the core editable fields out of a raw definition; a blank card when absent.</summary>
    public static CharacterCard Read(string? definitionJson)
    {
        if (string.IsNullOrWhiteSpace(definitionJson))
        {
            return CharacterCard.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(definitionJson);
            var spec = CardSpecClassifier.Classify(definitionJson);
            var source = FieldSource(document.RootElement);

            return new CharacterCard(
                spec,
                StringField(source, "name"),
                StringField(source, "description"),
                StringField(source, "personality"),
                StringField(source, "scenario"),
                StringField(source, "first_mes"),
                StringField(source, "character_version"),
                StringArrayField(source, "tags"));
        }
        catch (JsonException)
        {
            return CharacterCard.Empty;
        }
    }

    /// <summary>
    /// Returns <paramref name="originalJson"/> with the core fields of <paramref name="edited"/>
    /// written into the correct container (root for V1, <c>data</c> for V2), preserving all other
    /// fields. An empty or unparseable original yields a fresh minimal card matching the edit.
    /// </summary>
    public static string Apply(string? originalJson, CharacterCard edited)
    {
        var root = Parse(originalJson);
        var container = FieldContainer(root);

        container["name"] = edited.Name;
        container["description"] = edited.Description;
        container["personality"] = edited.Personality;
        container["scenario"] = edited.Scenario;
        container["first_mes"] = edited.FirstMessage;
        container["character_version"] = edited.CharacterVersion;
        container["tags"] = TagsArray(edited.Tags);

        return root.ToJsonString();
    }

    private static JsonObject Parse(string? originalJson)
    {
        if (string.IsNullOrWhiteSpace(originalJson))
        {
            return new JsonObject();
        }

        try
        {
            return JsonNode.Parse(originalJson) as JsonObject ?? new JsonObject();
        }
        catch (JsonException)
        {
            return new JsonObject();
        }
    }

    // The V2 nested `data` object when present; otherwise the root (V1 flat layout).
    private static JsonObject FieldContainer(JsonObject root) =>
        root["data"] is JsonObject data ? data : root;

    private static JsonArray TagsArray(ImmutableArray<string> tags)
    {
        var array = new JsonArray();
        foreach (var tag in tags)
        {
            array.Add(tag);
        }

        return array;
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

    private static ImmutableArray<string> StringArrayField(JsonElement source, string name)
    {
        if (source.ValueKind != JsonValueKind.Object
            || !source.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return ImmutableArray<string>.Empty;
        }

        // Dispose the array enumerator explicitly; both a LINQ chain and a foreach over it trip IDISP004.
        var builder = ImmutableArray.CreateBuilder<string>();
        using var elements = value.EnumerateArray();
        while (elements.MoveNext())
        {
            if (elements.Current.ValueKind == JsonValueKind.String)
            {
                builder.Add(elements.Current.GetString() ?? string.Empty);
            }
        }

        return builder.ToImmutable();
    }
}

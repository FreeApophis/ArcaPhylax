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
                StringField(source, "mes_example"),
                StringField(source, "character_version"),
                StringField(source, "creator"),
                StringArrayField(source, "tags"));
        }
        catch (JsonException)
        {
            return CharacterCard.Empty;
        }
    }

    /// <summary>
    /// Returns <paramref name="originalJson"/> rewritten as a valid TavernCard V2 card, with the core
    /// fields of <paramref name="edited"/> in its <c>data</c> object and every other field preserved.
    /// A V1 (flat) source is upgraded — its fields move into <c>data</c> — and an empty or unparseable
    /// source yields a fresh V2 card. The output is always <c>chara_card_v2</c>: strict importers such
    /// as SillyTavern reject cards that keep fields at the root or omit the spec.
    /// </summary>
    public static string Apply(string? originalJson, CharacterCard edited)
    {
        var root = Parse(originalJson);
        var data = DataObject(root);

        // Canonical V2 envelope.
        root["spec"] = "chara_card_v2";
        root["spec_version"] = "2.0";
        root["data"] = data;

        // Core fields the editor owns; always strings/arrays, never null.
        data["name"] = edited.Name;
        data["description"] = edited.Description;
        data["personality"] = edited.Personality;
        data["scenario"] = edited.Scenario;
        data["first_mes"] = edited.FirstMessage;
        data["mes_example"] = edited.MessageExample;
        data["character_version"] = edited.CharacterVersion;
        data["creator"] = edited.Creator;
        data["tags"] = TagsArray(edited.Tags);

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

    // The existing V2 `data` object, or a fresh one carrying a flat V1 card's fields so nothing is lost.
    private static JsonObject DataObject(JsonObject root)
    {
        if (root["data"] is JsonObject existing)
        {
            return existing;
        }

        var data = new JsonObject();
        var promoted = root
            .Select(pair => pair.Key)
            .Where(key => key is not ("spec" or "spec_version" or "data"))
            .ToList();

        // Clone while still parented (safe), then detach from the root so nothing is duplicated.
        foreach (var key in promoted)
        {
            data[key] = root[key]?.DeepClone();
        }

        foreach (var key in promoted)
        {
            root.Remove(key);
        }

        return data;
    }

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

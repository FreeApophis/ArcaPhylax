using System.Text.Json;

namespace CharacterArchivePlugin.Data;

/// <summary>
/// Classifies a raw card <c>definition</c> as V1 (flat top-level fields) or V2 (a <c>chara_card_v2</c>
/// wrapper around a nested <c>data</c> object). Anything unparseable or unrecognised is treated as V1,
/// the lowest common denominator. v3+ cards also nest under <c>data</c> and read as V2.
/// </summary>
public static class CardSpecClassifier
{
    public static CardSpec Classify(string? definitionJson)
    {
        if (string.IsNullOrWhiteSpace(definitionJson))
        {
            return CardSpec.V1;
        }

        try
        {
            using var document = JsonDocument.Parse(definitionJson);
            return IsV2(document.RootElement) ? CardSpec.V2 : CardSpec.V1;
        }
        catch (JsonException)
        {
            return CardSpec.V1;
        }
    }

    private static bool IsV2(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty("data", out var data)
        && data.ValueKind == JsonValueKind.Object
        && root.TryGetProperty("spec", out var spec)
        && spec.ValueKind == JsonValueKind.String
        && (spec.GetString() ?? string.Empty).StartsWith("chara_card_v", StringComparison.OrdinalIgnoreCase);
}

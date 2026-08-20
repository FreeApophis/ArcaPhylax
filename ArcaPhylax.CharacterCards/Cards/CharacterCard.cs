using System.Collections.Immutable;

namespace ArcaPhylax.CharacterCards.Cards;

/// <summary>
/// The core fields of a character card that are common to TavernCard V1 and V2 and editable by the
/// simple editor. This is a pure projection of a card <c>definition</c>: read it with
/// <see cref="CharacterCardJson.Read"/> and merge edits back onto the original JSON with
/// <see cref="CharacterCardJson.Apply"/>, which preserves every field the editor does not touch.
/// </summary>
public sealed record CharacterCard(
    CardSpec Spec,
    string Name,
    string Description,
    string Personality,
    string Scenario,
    string FirstMessage,
    string MessageExample,
    string CharacterVersion,
    string Creator,
    ImmutableArray<string> Tags)
{
    public static readonly CharacterCard Empty = new(
        CardSpec.V2,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        ImmutableArray<string>.Empty);
}

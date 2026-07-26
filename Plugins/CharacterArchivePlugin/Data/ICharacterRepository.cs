using Funcky.Monads;

namespace CharacterArchivePlugin.Data;

public interface ICharacterRepository
{
    /// <summary>
    /// Up to <paramref name="limit"/> stacks whose full-text matches <paramref name="query"/>
    /// (a null/blank query matches everything) and that carry <em>all</em> of <paramref name="tags"/>
    /// (an empty collection applies no tag filter), ordered by name. Rows sharing an image collapse
    /// into one stack represented by its most-complete variant; the count reflects matched variants.
    /// </summary>
    IEnumerable<CharacterStack> Search(string? query, IReadOnlyCollection<string> tags, int limit);

    /// <summary>
    /// Every character sharing <paramref name="imageHash"/> (the variants of one stack), most
    /// complete first then by name. A hash with a single character yields one entry.
    /// </summary>
    IEnumerable<CharacterTile> VariantsByImageHash(string imageHash);

    /// <summary>
    /// The most-used tags across every source, most frequent first, limited to
    /// <paramref name="limit"/>. <paramref name="filter"/> (when non-blank) keeps only tags whose
    /// name contains it (case-insensitive).
    /// </summary>
    IEnumerable<TagOption> PopularTags(string? filter, int limit);

    /// <summary>Full detail for a single character by its <c>source:primaryKey</c> key, or None.</summary>
    Option<Character> ById(string key);

    /// <summary>
    /// The original, unprojected card <c>definition</c> JSON for a <c>source:primaryKey</c> key, or
    /// None. This is the payload embedded into a downloaded TavernCard PNG.
    /// </summary>
    Option<string> DefinitionByKey(string key);
}

using CharacterArchivePlugin.Data;
using Funcky.Monads;

namespace CharacterArchivePlugin.Tests.Fakes;

/// <summary>
/// Hand-written <see cref="ICharacterRepository"/> that records the last <see cref="Search"/> call and
/// serves canned stacks, variants and details so view-model behaviour can be asserted without a database.
/// </summary>
internal sealed class FakeCharacterRepository : ICharacterRepository
{
    public string? LastSearchQuery { get; private set; }

    public IReadOnlyCollection<string>? LastSearchTags { get; private set; }

    /// <summary>Stacks returned by <see cref="Search"/> (the grid tiles).</summary>
    public List<CharacterStack> Stacks { get; } = [];

    /// <summary>Every variant row; <see cref="VariantsByImageHash"/> serves those matching a hash.</summary>
    public List<CharacterTile> Variants { get; } = [];

    /// <summary>Full detail served by <see cref="ById"/>, keyed by character key.</summary>
    public Dictionary<string, Character> Details { get; } = [];

    public List<TagOption> Tags { get; } = [];

    public IEnumerable<CharacterStack> Search(string? query, IReadOnlyCollection<string> tags, int limit)
    {
        LastSearchQuery = query;
        LastSearchTags = tags.ToArray();
        return Stacks;
    }

    public IEnumerable<CharacterTile> VariantsByImageHash(string imageHash) =>
        Variants.Where(variant => variant.ImageHash == imageHash).ToList();

    public IEnumerable<TagOption> PopularTags(string? filter, int limit) => Tags;

    public Option<Character> ById(string key) =>
        Details.TryGetValue(key, out var character) ? Option.Some(character) : Option<Character>.None;

    public Option<string> DefinitionByKey(string key) => Option<string>.None;
}

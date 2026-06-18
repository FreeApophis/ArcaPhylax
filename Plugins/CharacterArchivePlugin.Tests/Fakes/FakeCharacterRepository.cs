using CharacterArchivePlugin.Data;
using Funcky.Monads;

namespace CharacterArchivePlugin.Tests.Fakes;

/// <summary>
/// Hand-written <see cref="ICharacterRepository"/> that records the last <see cref="Search"/> call so
/// view-model tag-filter behaviour can be asserted without a database.
/// </summary>
internal sealed class FakeCharacterRepository : ICharacterRepository
{
    public string? LastSearchQuery { get; private set; }

    public IReadOnlyCollection<string>? LastSearchTags { get; private set; }

    public List<CharacterTile> Tiles { get; } = [];

    public List<TagOption> Tags { get; } = [];

    public IEnumerable<CharacterTile> Search(string? query, IReadOnlyCollection<string> tags, int limit)
    {
        LastSearchQuery = query;
        LastSearchTags = tags.ToArray();
        return Tiles;
    }

    public IEnumerable<TagOption> PopularTags(string? filter, int limit) => Tags;

    public Option<Character> ById(string key) => Option<Character>.None;

    public Option<string> DefinitionByKey(string key) => Option<string>.None;
}

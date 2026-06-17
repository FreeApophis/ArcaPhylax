using Funcky.Monads;

namespace CharacterArchivePlugin.Data;

public interface ICharacterRepository
{
    /// <summary>
    /// Up to <paramref name="limit"/> characters whose name matches <paramref name="query"/>
    /// (case-insensitive) across every source. A null/blank query returns the first
    /// <paramref name="limit"/> by name.
    /// </summary>
    IEnumerable<CharacterTile> Search(string? query, int limit);

    /// <summary>Full detail for a single character by its <c>source:primaryKey</c> key, or None.</summary>
    Option<Character> ById(string key);
}

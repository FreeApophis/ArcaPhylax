using System.Collections.Immutable;
using Funcky.Monads;

namespace CharacterArchivePlugin.Data;

/// <summary>
/// Resolves a synthetic <c>source:primaryKey</c> character key to the source <c>*_character_def</c>
/// table and primary-key column that hold its original card <c>definition</c> JSON. Table and column
/// names come from a fixed whitelist, so they are safe to interpolate into SQL.
/// </summary>
internal sealed record DefinitionLookup(string Table, string KeyColumn, string Key)
{
    // source prefix -> (def table, primary-key column). Mirrors the UNION in Data/character_search.sql.
    private static readonly ImmutableDictionary<string, (string Table, string KeyColumn)> Sources =
        ImmutableDictionary.CreateRange(StringComparer.Ordinal,
        [
            Source("booru", "booru_character_def", "id"),
            Source("char_tavern", "char_tavern_character_def", "increment"),
            Source("chub", "chub_character_def", "increment"),
            Source("generic", "generic_character_def", "card_data_hash"),
            Source("nyaime", "nyaime_character_def", "increment"),
            Source("risuai", "risuai_character_def", "increment"),
            Source("webring", "webring_character_def", "card_data_hash"),
        ]);

    public static Option<DefinitionLookup> ForKey(string? key)
    {
        var separator = key?.IndexOf(':') ?? -1;
        if (key is null || separator <= 0 || separator == key.Length - 1)
        {
            return Option<DefinitionLookup>.None;
        }

        var primaryKey = key[(separator + 1)..];
        return Sources.TryGetValue(key[..separator], out var table)
            ? Option.Some(new DefinitionLookup(table.Table, table.KeyColumn, primaryKey))
            : Option<DefinitionLookup>.None;
    }

    private static KeyValuePair<string, (string, string)> Source(string source, string table, string keyColumn) =>
        new(source, (table, keyColumn));
}

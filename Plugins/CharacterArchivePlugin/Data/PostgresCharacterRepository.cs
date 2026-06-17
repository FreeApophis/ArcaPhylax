using System.Collections.Immutable;
using Dapper;
using Funcky.Monads;

namespace CharacterArchivePlugin.Data;

/// <summary>
/// Reads characters from the <c>character_search</c> materialized view (a uniform, full-text-indexed
/// projection over every source <c>*_character_def</c> table; see <c>Data/character_search.sql</c>).
/// </summary>
public sealed class PostgresCharacterRepository(INpgsqlConnectionFactory db) : ICharacterRepository
{
    // Column order matches the CharacterTile(Key, Source, Name, ImageHash) constructor — Dapper binds
    // positional records by constructor order. Full-text match over name + author + tags + summary +
    // description; a blank query lists the first rows by name.
    private const string SearchSql = """
        SELECT key AS "Key", source AS "Source", name AS "Name", image_hash AS "ImageHash"
        FROM character_search
        WHERE (@query::text IS NULL OR search @@ websearch_to_tsquery('english', @query::text))
        ORDER BY name
        LIMIT @limit;
        """;

    // Tags come back as a newline-joined string and are split in C#: Npgsql surfaces text[] as
    // System.Array, which Dapper's positional-record matching can't bind to a string[] parameter.
    // Column order matches the CharacterRow constructor.
    private const string ByKeySql = """
        SELECT key AS "Key", source AS "Source", name AS "Name", coalesce(author, '') AS "Author",
               coalesce(summary, '') AS "Summary", description AS "Description",
               image_hash AS "ImageHash", array_to_string(tags, E'\n') AS "TagsText"
        FROM character_search
        WHERE key = @key;
        """;

    private sealed record CharacterRow(
        string Key, string Source, string Name, string Author, string Summary, string Description,
        string ImageHash, string TagsText);

    public IEnumerable<CharacterTile> Search(string? query, int limit)
    {
        using var connection = db.Create();
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("query", string.IsNullOrWhiteSpace(query) ? null : query)
            .Add("limit", limit)
            .ToDynamicParameters();

        return connection.Query<CharacterTile>(SearchSql, parameters).ToList();
    }

    public Option<Character> ById(string key)
    {
        using var connection = db.Create();
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("key", key)
            .ToDynamicParameters();

        return Option.FromNullable(connection.QuerySingleOrDefault<CharacterRow>(ByKeySql, parameters))
            .Select(ToCharacter);
    }

    private static Character ToCharacter(CharacterRow row) =>
        new(row.Key, row.Source, row.Name, row.Author, row.Summary, row.Description, row.ImageHash,
            row.TagsText.Split('\n', StringSplitOptions.RemoveEmptyEntries));
}

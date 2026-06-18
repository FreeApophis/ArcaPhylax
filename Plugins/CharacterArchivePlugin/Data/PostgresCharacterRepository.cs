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
    // description; a blank query lists the first rows by name. The selected tags arrive newline-joined
    // (the repo's established tag delimiter) and are re-split in SQL — passing them as one text param
    // avoids Dapper's IEnumerable "IN (...)" expansion, which would mangle the text[] containment.
    private const string SearchSql = """
        SELECT key AS "Key", source AS "Source", name AS "Name", image_hash AS "ImageHash"
        FROM character_search
        WHERE (@query::text IS NULL OR search @@ websearch_to_tsquery('english', @query::text))
          AND (@tags::text IS NULL OR tags @> string_to_array(@tags, E'\n'))
        ORDER BY name
        LIMIT @limit;
        """;

    // Lateral unnest of the tag arrays, grouped into per-tag frequencies. Count(*) is bigint -> long.
    private const string PopularTagsSql = """
        SELECT tag AS "Tag", count(*) AS "Count"
        FROM character_search, unnest(tags) AS tag
        WHERE (@filter::text IS NULL OR tag ILIKE '%' || @filter || '%')
        GROUP BY tag
        ORDER BY count(*) DESC, tag
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

    // Table and column come from DefinitionLookup's whitelist, so interpolating them is injection-safe.
    // The primary key is compared as text to sidestep the heterogeneous key types across sources.
    private const string DefinitionSqlTemplate =
        """SELECT definition::text FROM public.{0} WHERE {1}::text = @pk LIMIT 1;""";

    private sealed record CharacterRow(
        string Key, string Source, string Name, string Author, string Summary, string Description,
        string ImageHash, string TagsText);

    public IEnumerable<CharacterTile> Search(string? query, IReadOnlyCollection<string> tags, int limit)
    {
        using var connection = db.Create();
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("query", string.IsNullOrWhiteSpace(query) ? null : query)
            .Add("tags", tags.Count == 0 ? null : string.Join('\n', tags))
            .Add("limit", limit)
            .ToDynamicParameters();

        return connection.Query<CharacterTile>(SearchSql, parameters).ToList();
    }

    public IEnumerable<TagOption> PopularTags(string? filter, int limit)
    {
        using var connection = db.Create();
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("filter", string.IsNullOrWhiteSpace(filter) ? null : filter.Trim())
            .Add("limit", limit)
            .ToDynamicParameters();

        return connection.Query<TagOption>(PopularTagsSql, parameters).ToList();
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

    public Option<string> DefinitionByKey(string key) =>
        DefinitionLookup.ForKey(key).AndThen(QueryDefinition);

    private Option<string> QueryDefinition(DefinitionLookup lookup)
    {
        using var connection = db.Create();
        var sql = string.Format(DefinitionSqlTemplate, lookup.Table, lookup.KeyColumn);
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("pk", lookup.Key)
            .ToDynamicParameters();

        return Option.FromNullable(connection.QueryFirstOrDefault<string?>(sql, parameters));
    }

    private static Character ToCharacter(CharacterRow row) =>
        new(row.Key, row.Source, row.Name, row.Author, row.Summary, row.Description, row.ImageHash,
            row.TagsText.Split('\n', StringSplitOptions.RemoveEmptyEntries));
}

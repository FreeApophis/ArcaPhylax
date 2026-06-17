using System.Collections.Immutable;
using Dapper;
using Funcky.Monads;

namespace CharacterArchivePlugin.Data;

/// <summary>
/// Reads characters from the user's <c>char_archive</c> PostgreSQL database via Npgsql + Dapper.
/// The archive splits characters across one definition table per source platform; this repository
/// presents them as a single unified collection keyed by <c>source:primaryKey</c>.
/// </summary>
public sealed class PostgresCharacterRepository(INpgsqlConnectionFactory db) : ICharacterRepository
{
    /// <summary>A source platform's <c>*_character_def</c> table and how to read it.</summary>
    private sealed record SourceTable(string Tag, string Table, string PkColumn, bool IsNumericPk, bool HasSummary);

    // Fixed, code-controlled list (never user input) — safe to interpolate into SQL.
    private static readonly ImmutableArray<SourceTable> Sources =
    [
        new("booru",       "booru_character_def",       "id",             IsNumericPk: false, HasSummary: true),
        new("char_tavern", "char_tavern_character_def", "increment",      IsNumericPk: true,  HasSummary: false),
        new("chub",        "chub_character_def",        "increment",      IsNumericPk: true,  HasSummary: false),
        new("generic",     "generic_character_def",     "card_data_hash", IsNumericPk: false, HasSummary: true),
        new("nyaime",      "nyaime_character_def",      "increment",      IsNumericPk: true,  HasSummary: false),
        new("risuai",      "risuai_character_def",      "increment",      IsNumericPk: true,  HasSummary: false),
        new("webring",     "webring_character_def",     "card_data_hash", IsNumericPk: false, HasSummary: true),
    ];

    private static readonly string SearchSql = BuildSearchSql();

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
        var separator = key.IndexOf(':');
        if (separator < 0)
        {
            return Option<Character>.None;
        }

        var tag = key[..separator];
        var id = key[(separator + 1)..];

        return Sources.FirstOrDefault(source => source.Tag == tag) is { } table
            ? QueryByKey(table, id)
            : Option<Character>.None;
    }

    private Option<Character> QueryByKey(SourceTable table, string id)
    {
        // Column order matches the Character(Key, Source, Name, Description, ImageHash) constructor.
        var sql = $"""
            SELECT '{table.Tag}:' || {table.PkColumn}::text AS "Key",
                   '{table.Tag}' AS "Source",
                   name AS "Name",
                   {DescriptionExpression(table)} AS "Description",
                   image_hash AS "ImageHash"
            FROM public.{table.Table}
            WHERE {table.PkColumn} = @id;
            """;

        object idValue = table.IsNumericPk && long.TryParse(id, out var numeric) ? numeric : id;

        using var connection = db.Create();
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("id", idValue)
            .ToDynamicParameters();

        return Option.FromNullable(connection.QuerySingleOrDefault<Character>(sql, parameters));
    }

    private static string BuildSearchSql()
    {
        var union = string.Join(
            "\nUNION ALL\n",
            Sources.Select(source =>
                $"SELECT '{source.Tag}' AS source, '{source.Tag}:' || {source.PkColumn}::text AS key, " +
                $"name, image_hash FROM public.{source.Table} " +
                "WHERE (@query::text IS NULL OR name ILIKE '%' || @query::text || '%')"));

        // Column order matches the CharacterTile(Key, Source, Name, ImageHash) constructor — Dapper
        // binds positional records by constructor order (see DocumentRepository).
        return $"""
            SELECT key AS "Key", source AS "Source", name AS "Name", image_hash AS "ImageHash"
            FROM (
            {union}
            ) t
            ORDER BY name
            LIMIT @limit;
            """;
    }

    // The plain description lives in the character-card jsonb; curated summary/tagline (where the
    // source provides them) take precedence.
    private static string DescriptionExpression(SourceTable table) =>
        table.HasSummary
            ? "COALESCE(NULLIF(summary, ''), NULLIF(tagline, ''), definition #>> '{data,description}', definition ->> 'description', '')"
            : "COALESCE(definition #>> '{data,description}', definition ->> 'description', '')";
}

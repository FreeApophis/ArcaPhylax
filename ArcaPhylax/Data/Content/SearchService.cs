using ArcaPhylax.Abstractions;
using ArcaPhylax.Extensions;
using Dapper;
using System.Collections.Immutable;

namespace ArcaPhylax.Data.Content;

public sealed class SearchService(ISqliteConnectionFactory db) : ISearchService
{
    private const string SqlSearch = """
        SELECT rowid FROM fts_items
        WHERE fts_items MATCH @query
        ORDER BY rank
        LIMIT @limit;
        """;

    private const string SqlBuildFtsRow = """
        SELECT
            i.title AS title,
            COALESCE(
                (SELECT ia.value_text
                 FROM item_attributes ia
                 INNER JOIN attribute_defs ad ON ad.id = ia.attr_id
                 WHERE ia.item_id = i.id AND ad.name = 'body'
                 LIMIT 1),
                ''
            ) AS body,
            COALESCE(
                (SELECT GROUP_CONCAT(t.name, ' ')
                 FROM item_tags it
                 INNER JOIN tags t ON t.id = it.tag_id
                 WHERE it.item_id = i.id),
                ''
            ) AS tags,
            COALESCE(
                (SELECT GROUP_CONCAT(ad.name || ':' || COALESCE(ia.value_text, CAST(ia.value_int AS TEXT), CAST(ia.value_real AS TEXT), ia.value_ts), ' ')
                 FROM item_attributes ia
                 INNER JOIN attribute_defs ad ON ad.id = ia.attr_id
                 WHERE ia.item_id = i.id),
                ''
            ) AS attributes
        FROM items i
        WHERE i.id = @itemId;
        """;

    private const string SqlFtsTombstone = """
        INSERT INTO fts_items(fts_items, rowid, title, body, tags, attributes)
        VALUES ('delete', @rowid, @title, @body, @tags, @attributes);
        """;

    private const string SqlFtsInsert = """
        INSERT INTO fts_items(rowid, title, body, tags, attributes)
        VALUES (@rowid, @title, @body, @tags, @attributes);
        """;

    public IEnumerable<long> Search(string query, int limit = 50)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("query", query)
            .Add("limit", limit)
            .ToDynamicParameters();
        return connection.Query<long>(SqlSearch, p);
    }

    public void ReindexItem(long itemId)
    {
        using var connection = db.Create();
        var queryP = ImmutableDictionary<string, object?>.Empty
            .Add("itemId", itemId)
            .ToDynamicParameters();

        var row = connection.QuerySingleOrDefault<FtsRow>(SqlBuildFtsRow, queryP);
        if (row is null)
            return;

        var tombstoneP = ImmutableDictionary<string, object?>.Empty
            .Add("rowid", itemId)
            .Add("title", row.Title)
            .Add("body", row.Body)
            .Add("tags", row.Tags)
            .Add("attributes", row.Attributes)
            .ToDynamicParameters();
        connection.Execute(SqlFtsTombstone, tombstoneP);

        var insertP = ImmutableDictionary<string, object?>.Empty
            .Add("rowid", itemId)
            .Add("title", row.Title)
            .Add("body", row.Body)
            .Add("tags", row.Tags)
            .Add("attributes", row.Attributes)
            .ToDynamicParameters();
        connection.Execute(SqlFtsInsert, insertP);
    }

    public void DeleteFromIndex(long itemId)
    {
        using var connection = db.Create();

        var currentTitle = connection.ExecuteScalar<string?>(
            "SELECT title FROM fts_items WHERE rowid = @id;",
            ImmutableDictionary<string, object?>.Empty.Add("id", itemId).ToDynamicParameters())
            ?? string.Empty;

        var tombstoneP = ImmutableDictionary<string, object?>.Empty
            .Add("rowid", itemId)
            .Add("title", currentTitle)
            .Add("body", string.Empty)
            .Add("tags", string.Empty)
            .Add("attributes", string.Empty)
            .ToDynamicParameters();
        connection.Execute(SqlFtsTombstone, tombstoneP);
    }

    private sealed record FtsRow(string Title, string Body, string Tags, string Attributes);
}

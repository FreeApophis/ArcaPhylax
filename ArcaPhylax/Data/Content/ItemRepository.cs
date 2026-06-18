using System.Collections.Immutable;
using ArcaPhylax.Abstractions;
using ArcaPhylax.Abstractions.Content;
using ArcaPhylax.Extensions;
using Dapper;

namespace ArcaPhylax.Data.Content;

public sealed class ItemRepository(ISqliteConnectionFactory db) : IItemRepository
{
    private const string SqlInsert = """
        INSERT INTO items (kind, title, content_hash) VALUES (@kind, @title, @hash);
        SELECT last_insert_rowid();
        """;

    private const string SqlGetById = """
        SELECT id AS Id, kind AS Kind, title AS Title,
               created_at AS CreatedAt, imported_at AS ImportedAt,
               content_hash AS ContentHash
        FROM items WHERE id = @id;
        """;

    private const string SqlUpdateTitle = "UPDATE items SET title = @title WHERE id = @id;";
    private const string SqlDelete = "DELETE FROM items WHERE id = @id;";

    private const string SqlQuery = """
        SELECT id AS Id, kind AS Kind, title AS Title,
               created_at AS CreatedAt, imported_at AS ImportedAt,
               content_hash AS ContentHash
        FROM items
        WHERE (@kind IS NULL OR kind = @kind)
        ORDER BY id DESC
        LIMIT @limit OFFSET @offset;
        """;

    private const string SqlInsertRelation = """
        INSERT INTO item_relations (from_item_id, to_item_id, relation_type)
        VALUES (@from, @to, @type);
        SELECT last_insert_rowid();
        """;

    private const string SqlGetRelations = """
        SELECT id AS Id, from_item_id AS FromItemId,
               to_item_id AS ToItemId, relation_type AS RelationType
        FROM item_relations
        WHERE from_item_id = @itemId OR to_item_id = @itemId;
        """;

    private const string SqlDeleteRelation = "DELETE FROM item_relations WHERE id = @id;";

    public long Add(string kind, string title, string? contentHash = null)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("kind", kind)
            .Add("title", title)
            .Add("hash", contentHash)
            .ToDynamicParameters();
        return connection.QuerySingle<long>(SqlInsert, p);
    }

    public Item? GetById(long id)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("id", id)
            .ToDynamicParameters();
        return connection.QuerySingleOrDefault<Item>(SqlGetById, p);
    }

    public void UpdateTitle(long id, string title)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("id", id)
            .Add("title", title)
            .ToDynamicParameters();
        connection.Execute(SqlUpdateTitle, p);
    }

    public void Delete(long id)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("id", id)
            .ToDynamicParameters();
        connection.Execute(SqlDelete, p);
    }

    public IEnumerable<Item> Query(string? kindFilter, int limit, int offset)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("kind", kindFilter)
            .Add("limit", limit)
            .Add("offset", offset)
            .ToDynamicParameters();
        return connection.Query<Item>(SqlQuery, p);
    }

    public IEnumerable<ItemRelation> GetRelations(long itemId)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("itemId", itemId)
            .ToDynamicParameters();
        return connection.Query<ItemRelation>(SqlGetRelations, p);
    }

    public void AddRelation(long fromItemId, long toItemId, string relationType)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("from", fromItemId)
            .Add("to", toItemId)
            .Add("type", relationType)
            .ToDynamicParameters();
        connection.Execute(SqlInsertRelation, p);
    }

    public void DeleteRelation(long relationId)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("id", relationId)
            .ToDynamicParameters();
        connection.Execute(SqlDeleteRelation, p);
    }
}

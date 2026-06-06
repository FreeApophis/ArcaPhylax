using ArcaPhylax.Abstractions;
using ArcaPhylax.Abstractions.Content;
using ArcaPhylax.Extensions;
using Dapper;
using System.Collections.Immutable;

namespace ArcaPhylax.Data.Content;

public sealed class SetRepository(ISqliteConnectionFactory db) : ISetRepository
{
    private const string SqlCreate = """
        INSERT INTO sets (name, description, is_smart) VALUES (@name, @desc, @smart);
        SELECT last_insert_rowid();
        """;

    private const string SqlGetById = """
        SELECT id AS Id, name AS Name, description AS Description,
               is_smart AS IsSmart, created_at AS CreatedAt
        FROM sets WHERE id = @id;
        """;

    private const string SqlGetAll = """
        SELECT id AS Id, name AS Name, description AS Description,
               is_smart AS IsSmart, created_at AS CreatedAt
        FROM sets ORDER BY name;
        """;

    private const string SqlUpdate = "UPDATE sets SET name = @name, description = @desc WHERE id = @id;";
    private const string SqlDelete = "DELETE FROM sets WHERE id = @id;";

    private const string SqlAddItem = "INSERT OR IGNORE INTO set_items (set_id, item_id) VALUES (@setId, @itemId);";
    private const string SqlRemoveItem = "DELETE FROM set_items WHERE set_id = @setId AND item_id = @itemId;";

    private const string SqlGetItems = """
        SELECT i.id AS Id, i.kind AS Kind, i.title AS Title,
               i.created_at AS CreatedAt, i.imported_at AS ImportedAt,
               i.content_hash AS ContentHash
        FROM items i
        INNER JOIN set_items si ON si.item_id = i.id
        WHERE si.set_id = @setId
        ORDER BY si.added_at DESC
        LIMIT @limit OFFSET @offset;
        """;

    private const string SqlContains = "SELECT COUNT(1) FROM set_items WHERE set_id = @setId AND item_id = @itemId;";

    private const string SqlAddRule = """
        INSERT INTO set_rules (set_id, field, operator, value) VALUES (@setId, @field, @op, @val);
        """;

    private const string SqlDeleteRule = "DELETE FROM set_rules WHERE id = @id;";

    private const string SqlGetRules = """
        SELECT id AS Id, set_id AS SetId, field AS Field,
               operator AS Operator, value AS Value
        FROM set_rules WHERE set_id = @setId ORDER BY id;
        """;

    public long CreateSet(string name, string? description = null, bool isSmart = false)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("name", name)
            .Add("desc", (object?)description)
            .Add("smart", isSmart ? 1 : 0)
            .ToDynamicParameters();
        return connection.QuerySingle<long>(SqlCreate, p);
    }

    public ItemSet? GetById(long id)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("id", id)
            .ToDynamicParameters();
        return connection.QuerySingleOrDefault<ItemSet>(SqlGetById, p);
    }

    public IEnumerable<ItemSet> GetAll()
    {
        using var connection = db.Create();
        return connection.Query<ItemSet>(SqlGetAll);
    }

    public void UpdateSet(long id, string name, string? description)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("id", id)
            .Add("name", name)
            .Add("desc", (object?)description)
            .ToDynamicParameters();
        connection.Execute(SqlUpdate, p);
    }

    public void DeleteSet(long id)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("id", id)
            .ToDynamicParameters();
        connection.Execute(SqlDelete, p);
    }

    public void AddItem(long setId, long itemId)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("setId", setId)
            .Add("itemId", itemId)
            .ToDynamicParameters();
        connection.Execute(SqlAddItem, p);
    }

    public void RemoveItem(long setId, long itemId)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("setId", setId)
            .Add("itemId", itemId)
            .ToDynamicParameters();
        connection.Execute(SqlRemoveItem, p);
    }

    public IEnumerable<Item> GetItems(long setId, int limit, int offset)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("setId", setId)
            .Add("limit", limit)
            .Add("offset", offset)
            .ToDynamicParameters();
        return connection.Query<Item>(SqlGetItems, p);
    }

    public bool Contains(long setId, long itemId)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("setId", setId)
            .Add("itemId", itemId)
            .ToDynamicParameters();
        return connection.ExecuteScalar<long>(SqlContains, p) > 0;
    }

    public void AddRule(long setId, string field, string @operator, string value)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("setId", setId)
            .Add("field", field)
            .Add("op", @operator)
            .Add("val", value)
            .ToDynamicParameters();
        connection.Execute(SqlAddRule, p);
    }

    public void DeleteRule(long ruleId)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("id", ruleId)
            .ToDynamicParameters();
        connection.Execute(SqlDeleteRule, p);
    }

    public IEnumerable<SetRule> GetRules(long setId)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("setId", setId)
            .ToDynamicParameters();
        return connection.Query<SetRule>(SqlGetRules, p);
    }
}

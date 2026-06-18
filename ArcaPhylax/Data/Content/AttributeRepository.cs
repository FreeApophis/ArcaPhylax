using System.Collections.Immutable;
using ArcaPhylax.Abstractions;
using ArcaPhylax.Abstractions.Content;
using ArcaPhylax.Extensions;
using Dapper;

namespace ArcaPhylax.Data.Content;

public sealed class AttributeRepository(ISqliteConnectionFactory db) : IAttributeRepository
{
    private const string SqlEnsureDef = """
        INSERT INTO attribute_defs (name, value_type, applies_to) VALUES (@name, @valueType, @appliesTo)
        ON CONFLICT(name) DO NOTHING;
        SELECT id FROM attribute_defs WHERE name = @name;
        """;

    private const string SqlGetDefById = """
        SELECT id AS Id, name AS Name, value_type AS ValueType, applies_to AS AppliesTo
        FROM attribute_defs WHERE id = @id;
        """;

    private const string SqlGetDefByName = """
        SELECT id AS Id, name AS Name, value_type AS ValueType, applies_to AS AppliesTo
        FROM attribute_defs WHERE name = @name;
        """;

    private const string SqlGetAllDefs = """
        SELECT id AS Id, name AS Name, value_type AS ValueType, applies_to AS AppliesTo
        FROM attribute_defs ORDER BY name;
        """;

    private const string SqlUpsertText = """
        INSERT INTO item_attributes (item_id, attr_id, value_text)
        VALUES (@itemId, @attrId, @val)
        ON CONFLICT(item_id, attr_id) DO UPDATE SET
            value_text = @val, value_int = NULL, value_real = NULL, value_ts = NULL;
        """;

    private const string SqlUpsertInt = """
        INSERT INTO item_attributes (item_id, attr_id, value_int)
        VALUES (@itemId, @attrId, @val)
        ON CONFLICT(item_id, attr_id) DO UPDATE SET
            value_text = NULL, value_int = @val, value_real = NULL, value_ts = NULL;
        """;

    private const string SqlUpsertReal = """
        INSERT INTO item_attributes (item_id, attr_id, value_real)
        VALUES (@itemId, @attrId, @val)
        ON CONFLICT(item_id, attr_id) DO UPDATE SET
            value_text = NULL, value_int = NULL, value_real = @val, value_ts = NULL;
        """;

    private const string SqlUpsertTs = """
        INSERT INTO item_attributes (item_id, attr_id, value_ts)
        VALUES (@itemId, @attrId, @val)
        ON CONFLICT(item_id, attr_id) DO UPDATE SET
            value_text = NULL, value_int = NULL, value_real = NULL, value_ts = @val;
        """;

    private const string SqlRemove = """
        DELETE FROM item_attributes WHERE item_id = @itemId AND attr_id = @attrId;
        """;

    private const string SqlGetForItem = """
        SELECT item_id AS ItemId, attr_id AS AttrId,
               value_text AS ValueText, value_int AS ValueInt,
               value_real AS ValueReal, value_ts AS ValueTs
        FROM item_attributes WHERE item_id = @itemId;
        """;

    private const string SqlGetOne = """
        SELECT item_id AS ItemId, attr_id AS AttrId,
               value_text AS ValueText, value_int AS ValueInt,
               value_real AS ValueReal, value_ts AS ValueTs
        FROM item_attributes WHERE item_id = @itemId AND attr_id = @attrId;
        """;

    public long EnsureDefinition(string name, string valueType, string? appliesTo = null)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("name", name)
            .Add("valueType", valueType)
            .Add("appliesTo", (object?)appliesTo)
            .ToDynamicParameters();
        return connection.QuerySingle<long>(SqlEnsureDef, p);
    }

    public AttributeDef? GetDefinition(long id)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("id", id)
            .ToDynamicParameters();
        return connection.QuerySingleOrDefault<AttributeDef>(SqlGetDefById, p);
    }

    public AttributeDef? GetDefinitionByName(string name)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("name", name)
            .ToDynamicParameters();
        return connection.QuerySingleOrDefault<AttributeDef>(SqlGetDefByName, p);
    }

    public IEnumerable<AttributeDef> GetAllDefinitions()
    {
        using var connection = db.Create();
        return connection.Query<AttributeDef>(SqlGetAllDefs);
    }

    public void SetText(long itemId, long attrId, string value)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("itemId", itemId)
            .Add("attrId", attrId)
            .Add("val", value)
            .ToDynamicParameters();
        connection.Execute(SqlUpsertText, p);
    }

    public void SetInt(long itemId, long attrId, long value)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("itemId", itemId)
            .Add("attrId", attrId)
            .Add("val", value)
            .ToDynamicParameters();
        connection.Execute(SqlUpsertInt, p);
    }

    public void SetReal(long itemId, long attrId, double value)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("itemId", itemId)
            .Add("attrId", attrId)
            .Add("val", value)
            .ToDynamicParameters();
        connection.Execute(SqlUpsertReal, p);
    }

    public void SetTimestamp(long itemId, long attrId, string isoValue)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("itemId", itemId)
            .Add("attrId", attrId)
            .Add("val", isoValue)
            .ToDynamicParameters();
        connection.Execute(SqlUpsertTs, p);
    }

    public void Remove(long itemId, long attrId)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("itemId", itemId)
            .Add("attrId", attrId)
            .ToDynamicParameters();
        connection.Execute(SqlRemove, p);
    }

    public IEnumerable<ItemAttribute> GetForItem(long itemId)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("itemId", itemId)
            .ToDynamicParameters();
        return connection.Query<ItemAttribute>(SqlGetForItem, p);
    }

    public ItemAttribute? Get(long itemId, long attrId)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("itemId", itemId)
            .Add("attrId", attrId)
            .ToDynamicParameters();
        return connection.QuerySingleOrDefault<ItemAttribute>(SqlGetOne, p);
    }
}

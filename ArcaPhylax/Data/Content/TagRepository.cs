using ArcaPhylax.Abstractions;
using ArcaPhylax.Abstractions.Content;
using ArcaPhylax.Extensions;
using Dapper;
using System.Collections.Immutable;

namespace ArcaPhylax.Data.Content;

public sealed class TagRepository(ISqliteConnectionFactory db) : ITagRepository
{
    private const string SqlEnsureTag = """
        INSERT INTO tags (name, group_id) VALUES (@name, @groupId)
        ON CONFLICT(name) DO UPDATE SET group_id = COALESCE(@groupId, group_id);
        SELECT id FROM tags WHERE name = @name;
        """;

    private const string SqlGetTagById = """
        SELECT id AS Id, name AS Name, group_id AS GroupId
        FROM tags WHERE id = @id;
        """;

    private const string SqlGetTagByName = """
        SELECT id AS Id, name AS Name, group_id AS GroupId
        FROM tags WHERE name = @name;
        """;

    private const string SqlGetAllTags = """
        SELECT id AS Id, name AS Name, group_id AS GroupId
        FROM tags ORDER BY name;
        """;

    private const string SqlTagItem = """
        INSERT OR IGNORE INTO item_tags (item_id, tag_id)
        SELECT @itemId, id FROM tags WHERE name = @name;
        """;

    private const string SqlUntagItem = """
        DELETE FROM item_tags
        WHERE item_id = @itemId
          AND tag_id = (SELECT id FROM tags WHERE name = @name);
        """;

    private const string SqlUntagAll = "DELETE FROM item_tags WHERE item_id = @itemId;";

    private const string SqlGetTagsForItem = """
        SELECT t.id AS Id, t.name AS Name, t.group_id AS GroupId
        FROM tags t
        INNER JOIN item_tags it ON it.tag_id = t.id
        WHERE it.item_id = @itemId
        ORDER BY t.name;
        """;

    private const string SqlGetItemsForTag = """
        SELECT i.id AS Id, i.kind AS Kind, i.title AS Title,
               i.created_at AS CreatedAt, i.imported_at AS ImportedAt,
               i.content_hash AS ContentHash
        FROM items i
        INNER JOIN item_tags it ON it.item_id = i.id
        INNER JOIN tags t ON t.id = it.tag_id
        WHERE t.name = @name
        ORDER BY i.id DESC
        LIMIT @limit OFFSET @offset;
        """;

    private const string SqlEnsureGroup = """
        INSERT INTO tag_groups (name, color) VALUES (@name, @color)
        ON CONFLICT(name) DO UPDATE SET color = COALESCE(@color, color);
        SELECT id FROM tag_groups WHERE name = @name;
        """;

    private const string SqlGetAllGroups = """
        SELECT id AS Id, name AS Name, color AS Color
        FROM tag_groups ORDER BY name;
        """;

    public long EnsureTag(string name, long? groupId = null)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("name", name)
            .Add("groupId", (object?)groupId)
            .ToDynamicParameters();
        return connection.QuerySingle<long>(SqlEnsureTag, p);
    }

    public Tag? GetById(long id)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("id", id)
            .ToDynamicParameters();
        return connection.QuerySingleOrDefault<Tag>(SqlGetTagById, p);
    }

    public Tag? GetByName(string name)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("name", name)
            .ToDynamicParameters();
        return connection.QuerySingleOrDefault<Tag>(SqlGetTagByName, p);
    }

    public IEnumerable<Tag> GetAll()
    {
        using var connection = db.Create();
        return connection.Query<Tag>(SqlGetAllTags);
    }

    public void Tag(long itemId, string tagName)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("itemId", itemId)
            .Add("name", tagName)
            .ToDynamicParameters();
        connection.Execute(SqlTagItem, p);
    }

    public void Untag(long itemId, string tagName)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("itemId", itemId)
            .Add("name", tagName)
            .ToDynamicParameters();
        connection.Execute(SqlUntagItem, p);
    }

    public void UntagAll(long itemId)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("itemId", itemId)
            .ToDynamicParameters();
        connection.Execute(SqlUntagAll, p);
    }

    public IEnumerable<Tag> GetTagsForItem(long itemId)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("itemId", itemId)
            .ToDynamicParameters();
        return connection.Query<Tag>(SqlGetTagsForItem, p);
    }

    public IEnumerable<Item> GetItemsForTag(string tagName, int limit, int offset)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("name", tagName)
            .Add("limit", limit)
            .Add("offset", offset)
            .ToDynamicParameters();
        return connection.Query<Item>(SqlGetItemsForTag, p);
    }

    public long EnsureGroup(string name, string? color = null)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("name", name)
            .Add("color", (object?)color)
            .ToDynamicParameters();
        return connection.QuerySingle<long>(SqlEnsureGroup, p);
    }

    public IEnumerable<TagGroup> GetAllGroups()
    {
        using var connection = db.Create();
        return connection.Query<TagGroup>(SqlGetAllGroups);
    }
}

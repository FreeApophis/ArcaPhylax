using System.Collections.Immutable;
using ArcaPhylax.Abstractions;
using ArcaPhylax.Abstractions.Content;
using ArcaPhylax.Extensions;
using Dapper;

namespace ArcaPhylax.Data.Content;

public sealed class BlobRepository(ISqliteConnectionFactory db) : IBlobRepository
{
    private const string SqlInsert = """
        INSERT INTO blobs (item_id, role, mime_type, size_bytes, storage_uri, content_hash)
        VALUES (@itemId, @role, @mime, @size, @uri, @hash);
        SELECT last_insert_rowid();
        """;

    private const string SqlGetById = """
        SELECT id AS Id, item_id AS ItemId, role AS Role,
               mime_type AS MimeType, size_bytes AS SizeBytes,
               storage_uri AS StorageUri, content_hash AS ContentHash
        FROM blobs WHERE id = @id;
        """;

    private const string SqlGetByItem = """
        SELECT id AS Id, item_id AS ItemId, role AS Role,
               mime_type AS MimeType, size_bytes AS SizeBytes,
               storage_uri AS StorageUri, content_hash AS ContentHash
        FROM blobs WHERE item_id = @itemId ORDER BY id;
        """;

    private const string SqlGetPrimary = """
        SELECT id AS Id, item_id AS ItemId, role AS Role,
               mime_type AS MimeType, size_bytes AS SizeBytes,
               storage_uri AS StorageUri, content_hash AS ContentHash
        FROM blobs WHERE item_id = @itemId AND role = 'primary' LIMIT 1;
        """;

    private const string SqlDelete = "DELETE FROM blobs WHERE id = @id;";

    public long Add(long itemId, string role, string storageUri, string mimeType, long sizeBytes, string? contentHash = null)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("itemId", itemId)
            .Add("role", role)
            .Add("mime", mimeType)
            .Add("size", sizeBytes)
            .Add("uri", storageUri)
            .Add("hash", contentHash)
            .ToDynamicParameters();
        return connection.QuerySingle<long>(SqlInsert, p);
    }

    public Blob? GetById(long id)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("id", id)
            .ToDynamicParameters();
        return connection.QuerySingleOrDefault<Blob>(SqlGetById, p);
    }

    public IEnumerable<Blob> GetByItem(long itemId)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("itemId", itemId)
            .ToDynamicParameters();
        return connection.Query<Blob>(SqlGetByItem, p);
    }

    public Blob? GetPrimary(long itemId)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("itemId", itemId)
            .ToDynamicParameters();
        return connection.QuerySingleOrDefault<Blob>(SqlGetPrimary, p);
    }

    public void Delete(long id)
    {
        using var connection = db.Create();
        var p = ImmutableDictionary<string, object?>.Empty
            .Add("id", id)
            .ToDynamicParameters();
        connection.Execute(SqlDelete, p);
    }
}

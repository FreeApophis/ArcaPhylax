using ArcaPhylax.Extensions;
using Dapper;
using System.Collections.Immutable;

namespace ArcaPhylax.Data;

public sealed class DroppedItemsRepository : IDroppedItemsRepository
{
    private readonly ISqliteConnectionFactory _db;


    private const string DroppedItemQuery = """
        SELECT Id, Kind, Name, OriginalPath, StoredPath, CreatedUtc
        FROM DroppedItems
        ORDER BY Id DESC
        LIMIT @limit;
        """;

    private const string InsertDroppedItemQuery = """
        INSERT INTO DroppedItems (Kind, Name, OriginalPath, StoredPath, CreatedUtc)
        VALUES (@k, @n, @op, @sp, @ts);
        """;

    private const string SearchIdQuery = """
        SELECT rowid
        FROM DroppedItems_fts
        WHERE DroppedItems_fts MATCH @query
        LIMIT @limit;
        """;

    public DroppedItemsRepository(ISqliteConnectionFactory db) => _db = db;

    public void Add(string kind, string name, string? originalPath, string? storedPath)
    {
        using var connection = _db.Create();
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("k", kind)
            .Add("n", name)
            .Add("op", originalPath)
            .Add("sp", storedPath)
            .Add("ts", DateTime.UtcNow.ToString("O"))
            .ToDynamicParameters();

        connection.Execute(InsertDroppedItemQuery, parameters);
    }

    public IEnumerable<DroppedItem> Latest(int limit)
    {
        using var connection = _db.Create();
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("limit", limit)
            .ToDynamicParameters();

        return connection.Query<DroppedItem>(DroppedItemQuery, parameters);
    }


    public IEnumerable<long> SearchIds(string query, int limit)
    {
        using var connection = _db.Create();
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("query", query)
            .Add("limit", limit)
            .ToDynamicParameters();

        return connection.Query<long>(SearchIdQuery, parameters);
    }
}

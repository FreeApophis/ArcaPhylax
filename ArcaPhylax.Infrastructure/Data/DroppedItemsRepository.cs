using System.Collections.Immutable;
using ArcaPhylax.Extensions;
using Dapper;

namespace ArcaPhylax.Data;

public sealed class DroppedItemsRepository(ISqliteConnectionFactory db) : IDroppedItemsRepository
{
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

    private readonly ISqliteConnectionFactory _db = db;

    public void Add(string name, DropSource source)
    {
        var columns = DropSourceColumns.Of(source);

        using var connection = _db.Create();
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("k", columns.Kind)
            .Add("n", name)
            .Add("op", columns.OriginalPath)
            .Add("sp", columns.StoredPath)
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

        return connection.Query<DroppedItemRow>(DroppedItemQuery, parameters)
            .WhereSelect(ToDroppedItem)
            .ToImmutableList();
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

    /// <summary>The item a row describes, or None when its columns are not a valid source (legacy data).</summary>
    private static Option<DroppedItem> ToDroppedItem(DroppedItemRow row)
        => new DropSourceColumns(row.Kind, row.OriginalPath, row.StoredPath)
            .ToDropSource()
            .Select(source => new DroppedItem(row.Id, row.Name, source, row.CreatedUtc));

    /// <summary>The flat table row as Dapper materializes it (binds by column order).</summary>
    private sealed record DroppedItemRow(
        long Id,
        string Kind,
        string Name,
        string? OriginalPath,
        string? StoredPath,
        string CreatedUtc);
}

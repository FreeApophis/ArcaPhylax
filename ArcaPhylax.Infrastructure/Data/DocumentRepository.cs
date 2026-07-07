using System.Collections.Immutable;
using ArcaPhylax.Extensions;
using Dapper;

namespace ArcaPhylax.Data;

public sealed class DocumentRepository(ISqliteConnectionFactory db) : IDocumentRepository
{
    private const string DocumentQuery = """
        SELECT Id, Title, Kind, Subtitle
        FROM Documents
        WHERE (@query IS NULL OR Title LIKE '%' || @query || '%')
        ORDER BY Id
        LIMIT @limit OFFSET @offset;
        """;

    private readonly ISqliteConnectionFactory _db = db;

    public IEnumerable<Document> QueryTiles(string? search, int limit, int offset)
    {
        using var connection = _db.Create();
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("query", search)
            .Add("limit", limit)
            .Add("offset", offset)
            .ToDynamicParameters();

        return connection.Query<Document>(DocumentQuery, parameters);
    }
}

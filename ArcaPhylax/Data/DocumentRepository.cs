using Dapper;
using System.Collections.Immutable;
using ArcaPhylax.Extensions;

namespace ArcaPhylax.Data;

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly ISqliteConnectionFactory _db;


    private const string DocumentQuery = """
        SELECT Id, Title, Kind, Subtitle
        FROM Documents
        WHERE (@query IS NULL OR Title LIKE '%' || @query || '%')
        ORDER BY Id
        LIMIT @limit OFFSET @offset;
        """;

    public DocumentRepository(ISqliteConnectionFactory db) => _db = db;

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

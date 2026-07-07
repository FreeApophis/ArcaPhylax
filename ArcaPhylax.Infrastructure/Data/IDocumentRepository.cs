namespace ArcaPhylax.Data;

public interface IDocumentRepository
{
    IEnumerable<Document> QueryTiles(string? search, int limit, int offset);
}

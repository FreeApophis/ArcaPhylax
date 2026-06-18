namespace ArcaPhylax.Abstractions;

public interface ISearchService
{
    IEnumerable<long> Search(string query, int limit = 50);

    void ReindexItem(long itemId);

    void DeleteFromIndex(long itemId);
}

namespace ArcaPhylax.Data;

public interface IDroppedItemsRepository
{
    void Add(string kind, string name, string? originalPath, string? storedPath);

    IEnumerable<DroppedItem> Latest(int limit);

    IEnumerable<long> SearchIds(string query, int limit);
}

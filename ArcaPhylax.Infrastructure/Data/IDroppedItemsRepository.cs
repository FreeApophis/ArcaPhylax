namespace ArcaPhylax.Data;

public interface IDroppedItemsRepository
{
    void Add(string name, DropSource source);

    IEnumerable<DroppedItem> Latest(int limit);

    IEnumerable<long> SearchIds(string query, int limit);
}

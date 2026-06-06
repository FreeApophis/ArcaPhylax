using ArcaPhylax.Abstractions.Content;

namespace ArcaPhylax.Abstractions;

public interface IItemRepository
{
    long  Add(string kind, string title, string? contentHash = null);
    Item? GetById(long id);
    void  UpdateTitle(long id, string title);
    void  Delete(long id);
    IEnumerable<Item>         Query(string? kindFilter, int limit, int offset);
    IEnumerable<ItemRelation> GetRelations(long itemId);
    void  AddRelation(long fromItemId, long toItemId, string relationType);
    void  DeleteRelation(long relationId);
}

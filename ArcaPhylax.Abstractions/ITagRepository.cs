using ArcaPhylax.Abstractions.Content;

namespace ArcaPhylax.Abstractions;

public interface ITagRepository
{
    long EnsureTag(string name, long? groupId = null);

    Tag? GetById(long id);

    Tag? GetByName(string name);

    IEnumerable<Tag> GetAll();

    void Tag(long itemId, string tagName);

    void Untag(long itemId, string tagName);

    void UntagAll(long itemId);

    IEnumerable<Tag> GetTagsForItem(long itemId);

    IEnumerable<Item> GetItemsForTag(string tagName, int limit, int offset);

    long EnsureGroup(string name, string? color = null);

    IEnumerable<TagGroup> GetAllGroups();
}

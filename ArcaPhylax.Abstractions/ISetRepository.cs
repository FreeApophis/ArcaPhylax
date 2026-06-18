using ArcaPhylax.Abstractions.Content;

namespace ArcaPhylax.Abstractions;

public interface ISetRepository
{
    long CreateSet(string name, string? description = null, bool isSmart = false);

    ItemSet? GetById(long id);

    IEnumerable<ItemSet> GetAll();

    void UpdateSet(long id, string name, string? description);

    void DeleteSet(long id);

    void AddItem(long setId, long itemId);

    void RemoveItem(long setId, long itemId);

    IEnumerable<Item> GetItems(long setId, int limit, int offset);

    bool Contains(long setId, long itemId);

    void AddRule(long setId, string field, string @operator, string value);

    void DeleteRule(long ruleId);

    IEnumerable<SetRule> GetRules(long setId);
}

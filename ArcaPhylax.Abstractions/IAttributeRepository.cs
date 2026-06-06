using ArcaPhylax.Abstractions.Content;

namespace ArcaPhylax.Abstractions;

public interface IAttributeRepository
{
    long           EnsureDefinition(string name, string valueType, string? appliesTo = null);
    AttributeDef?  GetDefinition(long id);
    AttributeDef?  GetDefinitionByName(string name);
    IEnumerable<AttributeDef>   GetAllDefinitions();
    void   SetText(long itemId, long attrId, string value);
    void   SetInt(long itemId, long attrId, long value);
    void   SetReal(long itemId, long attrId, double value);
    void   SetTimestamp(long itemId, long attrId, string isoValue);
    void   Remove(long itemId, long attrId);
    IEnumerable<ItemAttribute>  GetForItem(long itemId);
    ItemAttribute? Get(long itemId, long attrId);
}

using ArcaPhylax.Abstractions.Content;

namespace ArcaPhylax.Abstractions;

public interface IBlobRepository
{
    long  Add(long itemId, string role, string storageUri, string mimeType, long sizeBytes, string? contentHash = null);
    Blob? GetById(long id);
    IEnumerable<Blob> GetByItem(long itemId);
    Blob? GetPrimary(long itemId);
    void  Delete(long id);
}

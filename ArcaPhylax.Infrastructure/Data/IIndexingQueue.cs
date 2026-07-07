namespace ArcaPhylax.Data;

public interface IIndexingQueue
{
    void Enqueue(IndexJob job);
}

namespace ArcaPhylax.Data;

public interface IIndexingWorker
{
    Task StartAsync(CancellationToken ct);
}

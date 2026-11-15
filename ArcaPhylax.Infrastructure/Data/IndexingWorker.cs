namespace ArcaPhylax.Data;

public sealed class IndexingWorker(IndexingQueue queue, ISqliteConnectionFactory db) : IIndexingWorker
{
    public async Task StartAsync(CancellationToken ct)
    {
        await foreach (var job in queue.Reader.ReadAllAsync(ct))
        {
            try
            {
                var columns = DropSourceColumns.Of(job.Source);
                using var c = db.Create();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
UPDATE DroppedItems
SET Name = Name
WHERE Name = $name
  AND (OriginalPath = $op OR (OriginalPath IS NULL AND $op IS NULL))
  AND (StoredPath = $sp OR (StoredPath IS NULL AND $sp IS NULL));";
                cmd.Parameters.AddWithValue("$name", job.Name);
                cmd.Parameters.AddWithValue("$op", (object?)columns.OriginalPath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$sp", (object?)columns.StoredPath ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // TODO: logging/retry strategy
            }
        }
    }
}

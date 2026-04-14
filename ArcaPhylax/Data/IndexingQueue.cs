using System.Threading.Channels;

namespace ArcaPhylax.Data;

public sealed class IndexingQueue : IIndexingQueue
{
    private readonly Channel<IndexJob> _ch = Channel.CreateUnbounded<IndexJob>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ChannelReader<IndexJob> Reader => _ch.Reader;

    public void Enqueue(IndexJob job) => _ch.Writer.TryWrite(job);
}

using LLama.Abstractions;

namespace DemiurgPlaygroundPlugin.Services;

/// <summary>
/// Streams model output with the ChatML control markers emitted by <see cref="ChatMlHistoryTransform"/>
/// removed — including a partial marker still in the buffer when generation stops. (The built-in
/// keyword transform flushes its tail verbatim at end-of-stream, which leaks a trailing
/// "&lt;|im_end|&gt;".) Also drops the U+FFFD replacement character some models emit around special
/// tokens.
/// </summary>
public sealed class ChatMlOutputTransform : ITextStreamTransform
{
    private static readonly string[] Markers = ["<|im_start|>", "<|im_end|>"];
    private static readonly int MaxMarkerLength = Markers.Max(m => m.Length);

    public async IAsyncEnumerable<string> TransformAsync(IAsyncEnumerable<string> tokens)
    {
        // 'buffer' is a streaming accumulator: text we have received but not yet safely emitted,
        // because the tail could be the start of a control marker split across tokens.
        var buffer = string.Empty;

        await foreach (var token in tokens)
        {
            buffer += token.Replace("�", string.Empty);

            while (FirstMarkerIndex(buffer) is { } found and >= 0)
            {
                if (found > 0)
                {
                    yield return buffer[..found];
                }

                buffer = buffer[(found + MarkerLengthAt(buffer, found))..];
            }

            var emittable = buffer.Length - TrailingMarkerPrefixLength(buffer);
            if (emittable > 0)
            {
                yield return buffer[..emittable];
                buffer = buffer[emittable..];
            }
        }

        // End of stream: the leftover is either plain text (emit) or a partial marker (drop).
        if (buffer.Length > 0 && TrailingMarkerPrefixLength(buffer) != buffer.Length)
        {
            yield return buffer;
        }
    }

    public ITextStreamTransform Clone() => new ChatMlOutputTransform();

    private static int FirstMarkerIndex(string text) =>
        Markers
            .Select(marker => text.IndexOf(marker, StringComparison.Ordinal))
            .Where(index => index >= 0)
            .DefaultIfEmpty(-1)
            .Min();

    private static int MarkerLengthAt(string text, int index) =>
        Markers.First(marker => text.AsSpan(index).StartsWith(marker)).Length;

    // Length of the longest suffix of 'text' that is a prefix of some marker, so it can be held back
    // in case the rest of the marker arrives in a later token.
    private static int TrailingMarkerPrefixLength(string text)
    {
        var max = Math.Min(text.Length, MaxMarkerLength - 1);
        return Enumerable.Range(1, max)
            .Reverse()
            .FirstOrDefault(k => Markers.Any(marker => marker.StartsWith(text[^k..], StringComparison.Ordinal)));
    }
}

namespace ArcaPhylax.PluginPackaging;

/// <summary>
/// The canonical text that a package signature covers: one <c>"&lt;sha256&gt; &lt;path&gt;\n"</c> line per
/// entry, ordered by path (ordinal). Every entry except the signature itself is listed, so any added,
/// removed, renamed or modified file invalidates the signature.
/// </summary>
public static class ContentListing
{
    public static string Of(IEnumerable<ContentEntry> entries)
        => string.Concat(entries
            .OrderBy(entry => entry.Path, StringComparer.Ordinal)
            .Select(entry => $"{entry.Sha256} {entry.Path}\n"));
}

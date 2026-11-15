namespace ArcaPhylax.PluginPackaging;

/// <summary>One package entry and the SHA-256 (lower-case hex) of its contents.</summary>
public sealed record ContentEntry(string Path, string Sha256);

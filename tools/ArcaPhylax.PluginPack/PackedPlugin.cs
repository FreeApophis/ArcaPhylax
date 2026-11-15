using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.PluginPack;

/// <summary>What `pack` prints: the manifest plus file name, size and SHA-256 for the catalog entry.</summary>
internal sealed record PackedPlugin(PluginManifest Manifest, string File, long Size, string Sha256);

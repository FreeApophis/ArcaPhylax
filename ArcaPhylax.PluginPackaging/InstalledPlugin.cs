namespace ArcaPhylax.PluginPackaging;

/// <summary>A plugin extracted into the store: its manifest, the directory holding its payload, and who signed it.</summary>
public sealed record InstalledPlugin(PluginManifest Manifest, string Directory, PublisherKey Signer)
{
    public const string SignerFileName = "signer.json";

    public string EntryAssemblyPath => Path.Combine(Directory, Manifest.EntryAssembly);
}
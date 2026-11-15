namespace ArcaPhylax.PluginPackaging;

/// <summary>A plugin extracted into the store: its manifest and the directory holding its payload.</summary>
public sealed record InstalledPlugin(PluginManifest Manifest, string Directory)
{
    public string EntryAssemblyPath => Path.Combine(Directory, Manifest.EntryAssembly);
}

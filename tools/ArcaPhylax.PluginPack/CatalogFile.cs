using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.PluginPack;

/// <summary>Adds a packed plugin to a <c>catalog.json</c> on disk, linking the package relative to the catalog.</summary>
internal static class CatalogFile
{
    public static Either<string, Unit> Add(string catalogPath, PackedPlugin packed, string packagePath)
        => Existing(catalogPath).Select(catalog => Written(
            catalogPath,
            catalog.With(packed.Manifest, RelativeUrl(catalogPath, packagePath), packed.Size, packed.Sha256)));

    private static Either<string, PluginCatalog> Existing(string catalogPath)
        => File.Exists(catalogPath)
            ? PluginCatalog.Parse(File.ReadAllText(catalogPath))
            : Either<string, PluginCatalog>.Right(PluginCatalog.Empty);

    private static string RelativeUrl(string catalogPath, string packagePath)
        => Path.GetRelativePath(Path.GetDirectoryName(Path.GetFullPath(catalogPath))!, Path.GetFullPath(packagePath))
            .Replace(Path.DirectorySeparatorChar, '/');

    private static Unit Written(string catalogPath, PluginCatalog catalog)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(catalogPath))!);
        File.WriteAllText(catalogPath, catalog.ToJson());
        return Unit.Value;
    }
}

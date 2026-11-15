namespace ArcaPhylax.PluginPackaging.Tests.Fakes;

/// <summary>Catalog entries built from <see cref="TempPayload.Manifest"/>.</summary>
public static class CatalogFixtures
{
    public static readonly HostEnvironment Host = new("win-x64", new Version(1, 0, 0));

    public static PluginManifest Manifest(string version = "1.2.3", string platform = "win-x64", string sdkVersion = "1.0.0")
        => TempPayload.Manifest with { Version = version, Platform = platform, SdkVersion = sdkVersion };

    public static PluginCatalog CatalogWith(params PluginManifest[] manifests)
        => manifests.Aggregate(PluginCatalog.Empty, (catalog, manifest) => catalog.With(manifest, Url(manifest), 100, "00"));

    public static CatalogPlugin PluginWith(params PluginManifest[] manifests)
        => CatalogWith(manifests).Plugins.Single();

    public static string Url(PluginManifest manifest) => $"plugins/{manifest.Id}-{manifest.Version}-{manifest.Platform}.arcaplugin";
}

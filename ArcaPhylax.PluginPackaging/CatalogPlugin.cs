using System.Collections.Immutable;

namespace ArcaPhylax.PluginPackaging;

/// <summary>A plugin in the catalog with its display metadata and every published package.</summary>
/// <remarks><c>EntryAssembly</c> lets the host match a bundled copy of the plugin to its catalog entry.</remarks>
public sealed record CatalogPlugin(
    string Id,
    string Name,
    string Description,
    string Author,
    string EntryAssembly,
    ImmutableList<CatalogPackage> Packages)
{
    /// <summary>The newest package this host can install, if any.</summary>
    public Option<CatalogPackage> NewestFor(HostEnvironment host)
        => Packages
            .Where(package => PluginCompatibility.Accepts(package.Platform, package.SdkVersion, host))
            .OrderByDescending(package => Version.Parse(package.Version))
            .FirstOrNone();

    internal static CatalogPlugin Of(PluginManifest manifest, CatalogPackage package)
        => new(manifest.Id, manifest.Name, manifest.Description, manifest.Author, manifest.EntryAssembly, [package]);

    /// <summary>This entry with <paramref name="package"/> added; display metadata follows the newest version.</summary>
    internal CatalogPlugin With(PluginManifest manifest, CatalogPackage package)
    {
        var packages = Packages
            .Where(existing => existing.Version != package.Version || existing.Platform != package.Platform)
            .Append(package)
            .OrderByDescending(existing => Version.Parse(existing.Version))
            .ThenBy(existing => existing.Platform, StringComparer.Ordinal)
            .ToImmutableList();

        return IsNewest(manifest.Version)
            ? Of(manifest, package) with { Packages = packages }
            : this with { Packages = packages };
    }

    private bool IsNewest(string version)
        => Packages.All(existing => Version.Parse(existing.Version) <= Version.Parse(version));
}

namespace ArcaPhylax.PluginPackaging;

/// <summary>What the catalog offers this host for one plugin, given what is installed. Closed.</summary>
public closed record PluginOffer
{
    /// <summary>The offer for <paramref name="plugin"/>; <paramref name="installedVersion"/> is what this host runs now (installed or bundled), if anything.</summary>
    public static PluginOffer For(CatalogPlugin plugin, HostEnvironment host, Option<string> installedVersion)
        => plugin.NewestFor(host).Match(
            none: () => installedVersion.Match<PluginOffer>(
                none: () => new NotOffered(),
                some: installed => new UpToDate(installed)),
            some: package => installedVersion.Match<PluginOffer>(
                none: () => new Installable(package),
                some: installed => Version.Parse(package.Version) > Version.Parse(installed)
                    ? new Updatable(installed, package)
                    : new UpToDate(installed)));
}

/// <summary>Not installed; <paramref name="Package"/> can be installed.</summary>
public sealed record Installable(CatalogPackage Package) : PluginOffer;

/// <summary>Installed in an older version; <paramref name="Package"/> is newer.</summary>
public sealed record Updatable(string InstalledVersion, CatalogPackage Package) : PluginOffer;

/// <summary>Installed, and the catalog has nothing newer for this host.</summary>
public sealed record UpToDate(string InstalledVersion) : PluginOffer;

/// <summary>Not installed, and no package fits this host's platform and SDK.</summary>
public sealed record NotOffered : PluginOffer;

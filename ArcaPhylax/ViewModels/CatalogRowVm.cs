using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.ViewModels;

/// <summary>One plugin from the online catalog, with what installing it would do on this computer.</summary>
public sealed record CatalogRowVm(string Name, string Description, string Author, PluginOffer Offer)
{
    public string Version => Offer switch
    {
        Installable installable => installable.Package.Version,
        Updatable updatable => $"{updatable.InstalledVersion} → {updatable.Package.Version}",
        UpToDate upToDate => upToDate.InstalledVersion,
        NotOffered => "–",
    };

    public string Detail => Offer switch
    {
        Installable installable => $"{Author} · {Megabytes(installable.Package.Size)}",
        Updatable updatable => $"{Author} · {Megabytes(updatable.Package.Size)}",
        UpToDate => Author,
        NotOffered => $"{Author} · not available for this computer",
    };

    public string ActionText => Offer switch
    {
        Installable => "Install",
        Updatable => "Update",
        UpToDate => "Installed",
        NotOffered => "Unavailable",
    };

    public Option<CatalogPackage> Package => Offer switch
    {
        Installable installable => installable.Package,
        Updatable updatable => updatable.Package,
        UpToDate or NotOffered => Option<CatalogPackage>.None,
    };

    public bool CanInstall => Package.Match(none: false, some: True);

    public static CatalogRowVm Of(CatalogPlugin plugin, PluginOffer offer)
        => new(plugin.Name, plugin.Description, plugin.Author, offer);

    private static string Megabytes(long bytes) => $"{bytes / 1024.0 / 1024.0:0.0} MB";
}

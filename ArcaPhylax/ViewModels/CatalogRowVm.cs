using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.ViewModels;

/// <summary>One plugin from the online catalog, with what installing it would do on this computer.</summary>
/// <remarks><c>Publisher</c> is the endorsed publisher that owns the plugin id, if any.</remarks>
public sealed record CatalogRowVm(string Name, string Description, string Author, Option<string> Publisher, PluginOffer Offer)
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
        Installable installable => $"{By} · {Megabytes(installable.Package.Size)}",
        Updatable updatable => $"{By} · {Megabytes(updatable.Package.Size)}",
        UpToDate => By,
        NotOffered => $"{By} · not available for this computer",
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

    /// <summary>Author, plus the publisher when someone else publishes it (or nobody endorsed vouches for it).</summary>
    private string By => Publisher.Match(
        none: () => $"{Author} · not from an endorsed publisher",
        some: publisher => string.Equals(publisher, Author, StringComparison.OrdinalIgnoreCase) || publisher == "ArcaPhylax"
            ? Author
            : $"{Author} · published by {publisher}");

    public static CatalogRowVm Of(CatalogPlugin plugin, Option<string> publisher, PluginOffer offer)
        => new(plugin.Name, plugin.Description, plugin.Author, publisher, offer);

    private static string Megabytes(long bytes) => $"{bytes / 1024.0 / 1024.0:0.0} MB";
}

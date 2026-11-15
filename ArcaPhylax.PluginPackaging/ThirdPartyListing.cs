namespace ArcaPhylax.PluginPackaging;

/// <summary>
/// The CI check for a plugin another publisher lists in the catalog: the package must be exactly what the
/// listing claims, downloadable over HTTPS, and signed by the endorsed publisher that owns the plugin id.
/// </summary>
public static class ThirdPartyListing
{
    /// <summary>Whether <paramref name="verified"/> (downloaded from <paramref name="package"/>) may be listed as <paramref name="entry"/>. <paramref name="policy"/> holds endorsed publishers only: no built-ins, no user overrides.</summary>
    public static Either<string, Unit> Check(CatalogPlugin entry, CatalogPackage package, VerifiedPackage verified, TrustPolicy policy)
        => ListingProblems(entry, package, verified.Manifest)
            .Concat(TrustProblems(entry.Id, verified.Signer, policy))
            .FirstOrNone()
            .Match(
                none: () => Either<string, Unit>.Right(Unit.Value),
                some: problem => Either<string, Unit>.Left($"{entry.Id} {package.Version} ({package.Platform}): {problem}"));

    private static IEnumerable<string> ListingProblems(CatalogPlugin entry, CatalogPackage package, PluginManifest manifest)
        => new (bool Valid, string Problem)[]
            {
                (IsHttps(package.Url), $"'{package.Url}' must be an absolute https:// URL."),
                (manifest.Id == entry.Id, $"the package is plugin '{manifest.Id}', not '{entry.Id}'."),
                (manifest.Version == package.Version, $"the package is version {manifest.Version}, the listing says {package.Version}."),
                (manifest.Platform == package.Platform, $"the package is for {manifest.Platform}, the listing says {package.Platform}."),
                (manifest.SdkVersion == package.SdkVersion, $"the package needs SDK {manifest.SdkVersion}, the listing says {package.SdkVersion}."),
            }
            .Where(check => !check.Valid)
            .Select(check => check.Problem);

    private static IEnumerable<string> TrustProblems(string pluginId, PublisherKey signer, TrustPolicy policy)
        => policy.Check(pluginId, signer).Match<IEnumerable<string>>(
            left: error => [Describe(error)],
            right: signed => signed switch
            {
                EndorsedSigner => [],
                UserTrustedSigner => ["the signer is not an endorsed publisher."],
            });

    private static string Describe(TrustError error) => error switch
    {
        UnknownPublisher unknown => $"the signing key {unknown.Key.KeyId} is not an endorsed publisher; request an endorsement first.",
        NotOwner notOwner => $"{notOwner.SignerName} is not endorsed for the plugin id '{notOwner.PluginId}'.",
        PublisherBlocked blocked => $"the signing key {blocked.KeyId} is blocked.",
        PublisherRevoked revoked => $"the signing key {revoked.KeyId} was revoked.",
    };

    private static bool IsHttps(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}

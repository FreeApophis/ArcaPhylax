using System.Collections.Immutable;
using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.PluginPack;

/// <summary>
/// <c>add-listings</c>: merges third-party listing files (<c>catalog/*.json</c>, catalog format with absolute
/// URLs) into a catalog after downloading and checking every package they list. Any problem fails the
/// command, so CI never publishes a listing that the app would refuse.
/// </summary>
internal static class AddListings
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(5) };

    public static int Run(string catalogPath, string listingsDirectory, IReadOnlyList<string> options)
        => (from pairs in ToolOptions.Pairs(options)
            from publishersPath in pairs.Value("--publishers")
            from rootPublicKey in pairs.Value("--root")
            select Run(catalogPath, listingsDirectory, publishersPath, rootPublicKey))
            .GetOrElse(Usage);

    private static int Run(string catalogPath, string listingsDirectory, string publishersPath, string rootPublicKey)
    {
        var result = from root in RootKey(rootPublicKey)
                     from policy in EndorsedOnly(publishersPath, root)
                     from catalog in ExistingCatalog(catalogPath)
                     from merged in Merged(catalog, ListingFiles(listingsDirectory), policy)
                     select merged;

        return result.Match(
            left: error =>
            {
                Console.Error.WriteLine($"error: {error}");
                return 1;
            },
            right: merged =>
            {
                File.WriteAllText(catalogPath, merged.ToJson());
                Console.WriteLine($"{catalogPath}: {merged.Plugins.Count} plugins.");
                return 0;
            });
    }

    private static Either<string, PublisherKey> RootKey(string base64)
    {
        var buffer = new byte[base64.Length];
        return Convert.TryFromBase64String(base64, buffer, out var written)
            ? Either<string, PublisherKey>.Right(PublisherKey.FromPublicKey(buffer[..written]))
            : Either<string, PublisherKey>.Left("--root must be the base64 root public key.");
    }

    /// <summary>Only the root-signed publisher list counts here: no built-in publishers, no user overrides.</summary>
    private static Either<string, TrustPolicy> EndorsedOnly(string publishersPath, PublisherKey root)
        => (File.Exists(publishersPath)
                ? PublisherList.Verify(File.ReadAllText(publishersPath), root)
                : Either<string, PublisherList>.Right(PublisherList.Empty))
            .Select(list => TrustPolicy.Of([], list, UserTrust.Empty));

    private static Either<string, PluginCatalog> ExistingCatalog(string catalogPath)
        => File.Exists(catalogPath)
            ? PluginCatalog.Parse(File.ReadAllText(catalogPath))
            : Either<string, PluginCatalog>.Right(PluginCatalog.Empty);

    private static ImmutableList<string> ListingFiles(string directory)
        => Directory.Exists(directory)
            ? Directory.GetFiles(directory, "*.json").Order(StringComparer.Ordinal).ToImmutableList()
            : [];

    private static Either<string, PluginCatalog> Merged(PluginCatalog catalog, ImmutableList<string> listingFiles, TrustPolicy policy)
    {
        var checkedListings = listingFiles.Select(file => Checked(file, policy)).ToImmutableList();
        var problems = checkedListings.SelectMany(listing => listing.Match(left: Identity, right: _ => ImmutableList<string>.Empty)).ToImmutableList();

        return problems.IsEmpty
            ? checkedListings.Aggregate(
                Either<string, PluginCatalog>.Right(catalog),
                (merged, listing) => merged.SelectMany(current => listing.Match(
                    left: _ => Either<string, PluginCatalog>.Right(current),
                    right: current.Merge)))
            : Either<string, PluginCatalog>.Left($"{problems.Count} listing problem(s):\n  " + string.Join("\n  ", problems));
    }

    /// <summary>The listing, or every problem found in it (all packages are checked, not just the first failure).</summary>
    private static Either<ImmutableList<string>, PluginCatalog> Checked(string file, TrustPolicy policy)
        => PluginCatalog.Parse(File.ReadAllText(file)).Match(
            left: error => Either<ImmutableList<string>, PluginCatalog>.Left([$"{Path.GetFileName(file)}: {error}"]),
            right: listing => ProblemsIn(listing, policy).Select(problem => $"{Path.GetFileName(file)}: {problem}").ToImmutableList() switch
            {
                { IsEmpty: true } => Either<ImmutableList<string>, PluginCatalog>.Right(listing),
                var problems => Either<ImmutableList<string>, PluginCatalog>.Left(problems),
            });

    private static IEnumerable<string> ProblemsIn(PluginCatalog listing, TrustPolicy policy)
        => listing.Plugins.SelectMany(plugin => plugin.Packages.Select(package => Problem(plugin, package, policy)))
            .WhereSelect(Identity);

    private static Option<string> Problem(CatalogPlugin plugin, CatalogPackage package, TrustPolicy policy)
    {
        Console.WriteLine($"Checking {plugin.Id} {package.Version} ({package.Platform}) from {package.Url}");
        return Downloaded(package)
            .SelectMany(bytes => package.Matches(bytes)
                ? Either<string, byte[]>.Right(bytes)
                : Either<string, byte[]>.Left($"{package.Url} does not match the listed size and SHA-256."))
            .SelectMany(bytes => VerifiedFrom(bytes))
            .SelectMany(verified => ThirdPartyListing.Check(plugin, package, verified, policy))
            .Match(left: Option.Some, right: _ => Option<string>.None);
    }

    private static Either<string, byte[]> Downloaded(CatalogPackage package)
    {
        // Network failures are listing problems to report, not crashes.
        try
        {
            return Uri.TryCreate(package.Url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
                ? Either<string, byte[]>.Right(Http.GetByteArrayAsync(uri).GetAwaiter().GetResult())
                : Either<string, byte[]>.Left($"'{package.Url}' must be an absolute https:// URL.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return Either<string, byte[]>.Left($"could not download {package.Url}: {exception.Message}");
        }
    }

    private static Either<string, VerifiedPackage> VerifiedFrom(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        return PluginPackage.Verify(stream).Match(
            left: error => Either<string, VerifiedPackage>.Left($"the package is invalid: {Describe(error)}"),
            right: Either<string, VerifiedPackage>.Right);
    }

    private static string Describe(PackageError error) => error switch
    {
        NotAPackage notAPackage => $"not an .arcaplugin file ({notAPackage.Reason}).",
        MissingEntry missing => $"'{missing.Path}' is missing.",
        UnsafeEntryPath unsafePath => $"unsafe file path '{unsafePath.Path}'.",
        InvalidManifest manifest => $"plugin.json is invalid: {manifest.Reason}",
        InvalidSignature => "the signature does not match the contents, or the package was packed by an older pack tool.",
    };

    private static int Usage()
    {
        Console.Error.WriteLine(
            """
            Usage:
              arcaphylax-pluginpack add-listings <catalog.json> <listings-dir> --publishers <publishers.json> --root <root-public-key-base64>
                  Downloads and checks every package in <listings-dir>/*.json and merges the listings into <catalog.json>.
            """);
        return 2;
    }
}

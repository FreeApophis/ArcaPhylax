using System.Net.Http.Headers;
using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.PluginSystem;

/// <summary>
/// Reads the online plugin catalog and downloads packages from it. A download is only handed on when it
/// is exactly the file the catalog describes; the package signature is checked afterwards by the store.
/// </summary>
public sealed class PluginCatalogClient(Uri catalogUri)
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(5),
        DefaultRequestHeaders =
        {
            UserAgent = { new ProductInfoHeaderValue("ArcaPhylax", typeof(PluginCatalogClient).Assembly.GetName().Version?.ToString(3) ?? "1.0.0") },
        },
    };

    public Uri CatalogUri => catalogUri;

    public async Task<Either<string, PluginCatalog>> CatalogAsync(CancellationToken cancellationToken = default)
        => (await BytesAsync(catalogUri, cancellationToken))
            .SelectMany(bytes => PluginCatalog.Parse(System.Text.Encoding.UTF8.GetString(bytes)));

    /// <summary>The signed publisher list published next to the catalog (verified by <see cref="PluginTrust"/>, not here).</summary>
    public async Task<Either<string, string>> PublishersAsync(CancellationToken cancellationToken = default)
        => (await BytesAsync(new Uri(catalogUri, PublisherList.FileName), cancellationToken))
            .Select(bytes => System.Text.Encoding.UTF8.GetString(bytes));

    public async Task<Either<string, byte[]>> DownloadAsync(CatalogPackage package, CancellationToken cancellationToken = default)
    {
        var downloaded = await PackageUri(package).Match(
            left: error => Task.FromResult(Either<string, byte[]>.Left(error)),
            right: uri => BytesAsync(uri, cancellationToken));

        return downloaded.SelectMany(bytes => Described(package, bytes));
    }

    private static Either<string, byte[]> Described(CatalogPackage package, byte[] bytes)
        => package.Matches(bytes)
            ? Either<string, byte[]>.Right(bytes)
            : Either<string, byte[]>.Left("the download does not match the catalog (size or SHA-256 differ).");

    /// <summary>Relative URLs resolve against the catalog. Packages only come over HTTPS, or from disk for a local catalog.</summary>
    private Either<string, Uri> PackageUri(CatalogPackage package)
        => Uri.TryCreate(catalogUri, package.Url, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || (uri.IsFile && catalogUri.IsFile))
            ? Either<string, Uri>.Right(uri)
            : Either<string, Uri>.Left($"the catalog links the package to an unsupported location ({package.Url}).");

    private static async Task<Either<string, byte[]>> BytesAsync(Uri uri, CancellationToken cancellationToken)
    {
        // Network and disk failures are expected here (offline, moved file); they become a message for the user.
        try
        {
            return Either<string, byte[]>.Right(uri.IsFile
                ? await File.ReadAllBytesAsync(uri.LocalPath, cancellationToken)
                : await Http.GetByteArrayAsync(uri, cancellationToken));
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or UnauthorizedAccessException
                                              || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            return Either<string, byte[]>.Left($"could not read {uri}: {exception.Message}");
        }
    }
}

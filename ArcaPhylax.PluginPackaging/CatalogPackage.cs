using System.Security.Cryptography;

namespace ArcaPhylax.PluginPackaging;

/// <summary>One downloadable package in the catalog: a plugin version built for one platform.</summary>
/// <param name="Version">Plugin version (<c>major.minor.patch</c>).</param>
/// <param name="Platform">Runtime identifier the package was published for.</param>
/// <param name="SdkVersion">Plugin SDK version the package was built against.</param>
/// <param name="Url">Download location; relative URLs are resolved against the catalog's own URL.</param>
/// <param name="Size">Package size in bytes.</param>
/// <param name="Sha256">Hex SHA-256 of the package file.</param>
public sealed record CatalogPackage(string Version, string Platform, string SdkVersion, string Url, long Size, string Sha256)
{
    /// <summary>Whether <paramref name="content"/> is the file this entry describes (size and hash).</summary>
    public bool Matches(byte[] content)
        => content.LongLength == Size
            && string.Equals(Convert.ToHexStringLower(SHA256.HashData(content)), Sha256, StringComparison.OrdinalIgnoreCase);
}

using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace ArcaPhylax.PluginPackaging.Tests;

public sealed class CatalogPackageTests
{
    private static readonly byte[] Content = Encoding.UTF8.GetBytes("package bytes");

    private static readonly CatalogPackage Package = new(
        "1.0.0", "win-x64", "1.0.0", "p.arcaplugin", Content.Length, Convert.ToHexStringLower(SHA256.HashData(Content)));

    [Fact]
    public void Matches_GivenTheDescribedBytes_ReturnsTrue()
    {
        // Act
        var matches = Package.Matches(Content);

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void Matches_GivenAnUpperCaseHash_ReturnsTrue()
    {
        // Arrange
        var package = Package with { Sha256 = Package.Sha256.ToUpperInvariant() };

        // Act
        var matches = package.Matches(Content);

        // Assert
        Assert.True(matches);
    }

    [Theory]
    [InlineData("package bytez")]
    [InlineData("package bytes!")]
    [InlineData("")]
    public void Matches_GivenOtherBytes_ReturnsFalse(string other)
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes(other);

        // Act
        var matches = Package.Matches(bytes);

        // Assert
        Assert.False(matches);
    }
}

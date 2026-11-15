using System.Security.Cryptography;
using Xunit;

namespace ArcaPhylax.PluginPackaging.Tests;

public sealed class PublisherKeyTests
{
    [Fact]
    public void FromPublicKey_KeyIdIsFirst16HexCharsOfSha256OfThePublicKey()
    {
        // Arrange
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var spki = key.ExportSubjectPublicKeyInfo();
        var expected = Convert.ToHexStringLower(SHA256.HashData(spki))[..16];

        // Act
        var publisherKey = PublisherKey.FromPublicKey(spki);

        // Assert
        Assert.Equal(expected, publisherKey.KeyId);
    }

    [Fact]
    public void FromPublicKey_StoresThePublicKeyAsBase64()
    {
        // Arrange
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var spki = key.ExportSubjectPublicKeyInfo();
        var expected = Convert.ToBase64String(spki);

        // Act
        var publisherKey = PublisherKey.FromPublicKey(spki);

        // Assert
        Assert.Equal(expected, publisherKey.PublicKey);
    }
}

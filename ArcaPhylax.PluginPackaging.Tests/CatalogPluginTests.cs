using ArcaPhylax.PluginPackaging.Tests.Fakes;
using Xunit;

namespace ArcaPhylax.PluginPackaging.Tests;

public sealed class CatalogPluginTests
{
    [Fact]
    public void NewestFor_GivenSeveralVersionsAndPlatforms_ReturnsTheNewestForTheHostPlatform()
    {
        // Arrange
        var plugin = CatalogFixtures.PluginWith(
            CatalogFixtures.Manifest(version: "1.0.0"),
            CatalogFixtures.Manifest(version: "1.1.0"),
            CatalogFixtures.Manifest(version: "1.2.0", platform: "linux-x64"));

        // Act
        var newest = plugin.NewestFor(CatalogFixtures.Host);

        // Assert
        Assert.Equal("1.1.0", Assert.Some(newest).Version);
    }

    [Fact]
    public void NewestFor_GivenTheNewestNeedsANewerSdk_ReturnsTheNewestCompatible()
    {
        // Arrange
        var plugin = CatalogFixtures.PluginWith(
            CatalogFixtures.Manifest(version: "1.0.0"),
            CatalogFixtures.Manifest(version: "2.0.0", sdkVersion: "2.0.0"));

        // Act
        var newest = plugin.NewestFor(CatalogFixtures.Host);

        // Assert
        Assert.Equal("1.0.0", Assert.Some(newest).Version);
    }

    [Fact]
    public void NewestFor_GivenOnlyOtherPlatforms_ReturnsNone()
    {
        // Arrange
        var plugin = CatalogFixtures.PluginWith(CatalogFixtures.Manifest(platform: "osx-arm64"));

        // Act
        var newest = plugin.NewestFor(CatalogFixtures.Host);

        // Assert
        Assert.None(newest);
    }
}

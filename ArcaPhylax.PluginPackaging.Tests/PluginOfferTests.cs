using ArcaPhylax.PluginPackaging.Tests.Fakes;
using Xunit;

namespace ArcaPhylax.PluginPackaging.Tests;

public sealed class PluginOfferTests
{
    private static readonly CatalogPlugin Plugin = CatalogFixtures.PluginWith(CatalogFixtures.Manifest(version: "1.2.3"));

    [Fact]
    public void For_GivenNothingInstalled_ReturnsInstallable()
    {
        // Arrange
        var expected = new Installable(Plugin.Packages.Single());

        // Act
        var offer = PluginOffer.For(Plugin, CatalogFixtures.Host, Option<string>.None);

        // Assert
        Assert.Equal(expected, offer);
    }

    [Fact]
    public void For_GivenAnOlderVersionInstalled_ReturnsUpdatable()
    {
        // Arrange
        var expected = new Updatable("1.2.0", Plugin.Packages.Single());

        // Act
        var offer = PluginOffer.For(Plugin, CatalogFixtures.Host, Option.Some("1.2.0"));

        // Assert
        Assert.Equal(expected, offer);
    }

    [Theory]
    [InlineData("1.2.3")]
    [InlineData("2.0.0")]
    public void For_GivenTheSameOrANewerVersionInstalled_ReturnsUpToDate(string installed)
    {
        // Arrange
        var expected = new UpToDate(installed);

        // Act
        var offer = PluginOffer.For(Plugin, CatalogFixtures.Host, Option.Some(installed));

        // Assert
        Assert.Equal(expected, offer);
    }

    [Fact]
    public void For_GivenNoPackageForThisHostAndNothingInstalled_ReturnsNotOffered()
    {
        // Arrange
        var linuxOnly = CatalogFixtures.PluginWith(CatalogFixtures.Manifest(platform: "linux-x64"));

        // Act
        var offer = PluginOffer.For(linuxOnly, CatalogFixtures.Host, Option<string>.None);

        // Assert
        Assert.IsType<NotOffered>(offer);
    }

    [Fact]
    public void For_GivenNoPackageForThisHostButInstalled_ReturnsUpToDate()
    {
        // Arrange
        var linuxOnly = CatalogFixtures.PluginWith(CatalogFixtures.Manifest(platform: "linux-x64"));
        var expected = new UpToDate("1.0.0");

        // Act
        var offer = PluginOffer.For(linuxOnly, CatalogFixtures.Host, Option.Some("1.0.0"));

        // Assert
        Assert.Equal(expected, offer);
    }
}

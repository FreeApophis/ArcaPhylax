using System.Collections.Immutable;
using ArcaPhylax.PluginPackaging.Tests.Fakes;
using Xunit;

namespace ArcaPhylax.PluginPackaging.Tests;

public sealed class ThirdPartyListingTests
{
    private static readonly Publisher Contoso = new("Contoso", Keys.NewPublicKey(), ["Contoso"]);

    private static readonly PluginManifest Manifest = TempPayload.Manifest with { Id = "Contoso.Hello", Version = "1.2.3", Platform = "win-x64", SdkVersion = "1.0.0" };

    private static readonly CatalogPackage Package = new("1.2.3", "win-x64", "1.0.0", "https://github.com/contoso/hello/releases/download/v1.2.3/hello-win-x64.arcaplugin", 10, "00");

    private static readonly CatalogPlugin Entry = new("Contoso.Hello", "Hello", "Says hello.", "Contoso", "Sample.dll", [Package]);

    private static readonly TrustPolicy Policy = TrustPolicy.Of([], PublisherList.Empty.Endorse(Contoso), UserTrust.Empty);

    [Fact]
    public void Check_GivenAMatchingPackageFromTheEndorsedOwner_ReturnsRight()
    {
        // Arrange
        var verified = Verified(Manifest, Contoso.Key);

        // Act
        var result = ThirdPartyListing.Check(Entry, Package, verified, Policy);

        // Assert
        Assert.Right(result);
    }

    [Theory]
    [InlineData("http://example.com/hello.arcaplugin")]
    [InlineData("plugins/hello.arcaplugin")]
    [InlineData("file:///C:/hello.arcaplugin")]
    public void Check_GivenAUrlThatIsNotAbsoluteHttps_ReturnsLeft(string url)
    {
        // Arrange
        var package = Package with { Url = url };

        // Act
        var result = ThirdPartyListing.Check(Entry, package, Verified(Manifest, Contoso.Key), Policy);

        // Assert
        Assert.Left(result);
    }

    [Fact]
    public void Check_GivenAPackageForAnotherPluginId_ReturnsLeft()
    {
        // Arrange
        var verified = Verified(Manifest with { Id = "Contoso.Other" }, Contoso.Key);

        // Act
        var result = ThirdPartyListing.Check(Entry, Package, verified, Policy);

        // Assert
        Assert.Left(result);
    }

    [Theory]
    [InlineData("9.9.9", "win-x64", "1.0.0")]
    [InlineData("1.2.3", "linux-x64", "1.0.0")]
    [InlineData("1.2.3", "win-x64", "1.1.0")]
    public void Check_GivenAManifestThatDisagreesWithTheListing_ReturnsLeft(string version, string platform, string sdkVersion)
    {
        // Arrange
        var verified = Verified(Manifest with { Version = version, Platform = platform, SdkVersion = sdkVersion }, Contoso.Key);

        // Act
        var result = ThirdPartyListing.Check(Entry, Package, verified, Policy);

        // Assert
        Assert.Left(result);
    }

    [Fact]
    public void Check_GivenASignerThatIsNotEndorsed_ReturnsLeft()
    {
        // Arrange
        var verified = Verified(Manifest, Keys.NewPublicKey());

        // Act
        var result = ThirdPartyListing.Check(Entry, Package, verified, Policy);

        // Assert
        Assert.Left(result);
    }

    [Fact]
    public void Check_GivenAnEndorsedSignerThatDoesNotOwnTheId_ReturnsLeft()
    {
        // Arrange
        var fabrikam = new Publisher("Fabrikam", Keys.NewPublicKey(), ["Fabrikam"]);
        var policy = TrustPolicy.Of([], PublisherList.Empty.Endorse(Contoso).Endorse(fabrikam), UserTrust.Empty);

        // Act
        var result = ThirdPartyListing.Check(Entry, Package, Verified(Manifest, fabrikam.Key), policy);

        // Assert
        Assert.Left(result);
    }

    [Fact]
    public void Merge_GivenPluginsWithOtherIds_AddsThem()
    {
        // Arrange
        var catalog = CatalogFixtures.CatalogWith(CatalogFixtures.Manifest());
        var listing = PluginCatalog.Empty with { Plugins = [Entry] };

        // Act
        var merged = catalog.Merge(listing);

        // Assert
        Assert.Equal(["ArcaPhylax.Sample", "Contoso.Hello"], Assert.Right(merged).Plugins.Select(plugin => plugin.Id));
    }

    [Fact]
    public void Merge_GivenAPluginIdThatIsAlreadyListed_ReturnsLeft()
    {
        // Arrange
        var catalog = PluginCatalog.Empty with { Plugins = [Entry] };
        var listing = PluginCatalog.Empty with { Plugins = [Entry with { Name = "Impostor" }] };

        // Act
        var merged = catalog.Merge(listing);

        // Assert
        Assert.Left(merged);
    }

    private static VerifiedPackage Verified(PluginManifest manifest, PublisherKey signer)
        => new(manifest, signer, ImmutableList<string>.Empty);
}

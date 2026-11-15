using ArcaPhylax.PluginPackaging.Tests.Fakes;
using Xunit;

namespace ArcaPhylax.PluginPackaging.Tests;

public sealed class PluginCatalogTests
{
    [Fact]
    public void Parse_GivenSerializedCatalog_RoundTripsThePackages()
    {
        // Arrange
        var catalog = CatalogFixtures.CatalogWith(CatalogFixtures.Manifest(), CatalogFixtures.Manifest(platform: "linux-x64"));
        var expected = catalog.Plugins.Single().Packages;

        // Act
        var parsed = PluginCatalog.Parse(catalog.ToJson());

        // Assert
        Assert.Equal(expected, Assert.Right(parsed).Plugins.Single().Packages);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{"schemaVersion":1,"plugins":[{"id":"A"}]}""")]
    public void Parse_GivenMalformedOrIncompleteJson_ReturnsLeft(string json)
    {
        // Act
        var parsed = PluginCatalog.Parse(json);

        // Assert
        Assert.Left(parsed);
    }

    [Fact]
    public void Parse_GivenNewerSchemaVersion_ReturnsLeft()
    {
        // Arrange
        var newer = PluginCatalog.Empty with { SchemaVersion = PluginCatalog.CurrentSchemaVersion + 1 };

        // Act
        var parsed = PluginCatalog.Parse(newer.ToJson());

        // Assert
        Assert.Left(parsed);
    }

    [Fact]
    public void With_GivenNewPlugin_AddsItWithOnePackage()
    {
        // Arrange
        var manifest = CatalogFixtures.Manifest();
        var expected = new CatalogPackage("1.2.3", "win-x64", "1.0.0", "plugins/a.arcaplugin", 42, "ab");

        // Act
        var catalog = PluginCatalog.Empty.With(manifest, "plugins/a.arcaplugin", 42, "ab");

        // Assert
        Assert.Equal([expected], catalog.Plugins.Single().Packages);
    }

    [Fact]
    public void With_GivenSameVersionAndPlatform_ReplacesThePackage()
    {
        // Arrange
        var manifest = CatalogFixtures.Manifest();
        var catalog = PluginCatalog.Empty.With(manifest, "old", 1, "11");
        var expected = new CatalogPackage("1.2.3", "win-x64", "1.0.0", "new", 2, "22");

        // Act
        var updated = catalog.With(manifest, "new", 2, "22");

        // Assert
        Assert.Equal([expected], updated.Plugins.Single().Packages);
    }

    [Fact]
    public void With_GivenOtherPlatformOrVersion_KeepsAllPackages()
    {
        // Arrange
        var catalog = CatalogFixtures.CatalogWith(CatalogFixtures.Manifest());

        // Act
        var updated = catalog
            .With(CatalogFixtures.Manifest(platform: "linux-x64"), "linux", 1, "11")
            .With(CatalogFixtures.Manifest(version: "1.3.0"), "newer", 1, "11");

        // Assert
        Assert.Equal(3, updated.Plugins.Single().Packages.Count);
    }

    [Fact]
    public void With_GivenOtherPlugin_AddsASecondEntry()
    {
        // Arrange
        var catalog = CatalogFixtures.CatalogWith(CatalogFixtures.Manifest());
        var other = CatalogFixtures.Manifest() with { Id = "ArcaPhylax.Other" };

        // Act
        var updated = catalog.With(other, "other", 1, "11");

        // Assert
        Assert.Equal(["ArcaPhylax.Other", "ArcaPhylax.Sample"], updated.Plugins.Select(plugin => plugin.Id));
    }

    [Fact]
    public void With_GivenNewerVersion_TakesTheDisplayMetadataFromIt()
    {
        // Arrange
        var catalog = CatalogFixtures.CatalogWith(CatalogFixtures.Manifest());
        var newer = CatalogFixtures.Manifest(version: "2.0.0") with { Name = "Renamed" };

        // Act
        var updated = catalog.With(newer, "newer", 1, "11");

        // Assert
        Assert.Equal("Renamed", updated.Plugins.Single().Name);
    }

    [Fact]
    public void With_GivenOlderVersion_KeepsTheDisplayMetadataOfTheNewest()
    {
        // Arrange
        var catalog = CatalogFixtures.CatalogWith(CatalogFixtures.Manifest(version: "2.0.0"));
        var older = CatalogFixtures.Manifest(version: "1.0.0") with { Name = "Old name" };

        // Act
        var updated = catalog.With(older, "older", 1, "11");

        // Assert
        Assert.Equal("Sample", updated.Plugins.Single().Name);
    }
}

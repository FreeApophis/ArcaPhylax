using ArcaPhylax.PluginPackaging.Tests.Fakes;
using Xunit;

namespace ArcaPhylax.PluginPackaging.Tests;

public sealed class PluginManifestTests
{
    [Fact]
    public void Parse_GivenSerializedManifest_RoundTrips()
    {
        // Arrange
        var manifest = TempPayload.Manifest;
        var json = manifest.ToJson();

        // Act
        var parsed = PluginManifest.Parse(json);

        // Assert
        Assert.Right(manifest, parsed);
    }

    [Fact]
    public void ToJson_UsesCamelCasePropertyNames()
    {
        // Arrange
        var manifest = TempPayload.Manifest;

        // Act
        var json = manifest.ToJson();

        // Assert
        Assert.Contains("\"entryAssembly\": \"Sample.dll\"", json);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{"id":"ArcaPhylax.Sample","version":"1.2.3","name":"Sample","description":"d","author":"a","platform":"win-x64","entryAssembly":"Sample.dll"}""")]
    [InlineData("""{"id":"ArcaPhylax.Sample","version":"1.2.3","name":"Sample","description":"d","author":"a","platform":"win-x64","entryAssembly":null,"sdkVersion":"1.0.0"}""")]
    public void Parse_GivenMalformedOrIncompleteJson_ReturnsInvalidManifest(string json)
    {
        // Arrange

        // Act
        var parsed = PluginManifest.Parse(json);

        // Assert
        Assert.IsType<InvalidManifest>(Assert.Left(parsed));
    }

    public static TheoryData<PluginManifest> InvalidManifests() =>
    [
        TempPayload.Manifest with { Id = string.Empty },
        TempPayload.Manifest with { Id = "has spaces" },
        TempPayload.Manifest with { Id = "../escape" },
        TempPayload.Manifest with { Version = "one" },
        TempPayload.Manifest with { Version = "1" },
        TempPayload.Manifest with { Name = " " },
        TempPayload.Manifest with { Platform = string.Empty },
        TempPayload.Manifest with { EntryAssembly = "Sample.exe" },
        TempPayload.Manifest with { EntryAssembly = "sub/Sample.dll" },
        TempPayload.Manifest with { EntryAssembly = @"..\Sample.dll" },
        TempPayload.Manifest with { SdkVersion = "x" },
    ];

    [Theory]
    [MemberData(nameof(InvalidManifests))]
    public void Parse_GivenWellFormedJsonWithInvalidValues_ReturnsInvalidManifest(PluginManifest manifest)
    {
        // Arrange
        var json = manifest.ToJson();

        // Act
        var parsed = PluginManifest.Parse(json);

        // Assert
        Assert.IsType<InvalidManifest>(Assert.Left(parsed));
    }
}

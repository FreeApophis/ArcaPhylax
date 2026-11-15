using ArcaPhylax.PluginPackaging.Tests.Fakes;
using Xunit;

namespace ArcaPhylax.PluginPackaging.Tests;

public sealed class PluginCompatibilityTests
{
    private static readonly HostEnvironment Host = new("win-x64", new Version(1, 2, 0));

    [Theory]
    [InlineData("1.2.0")]
    [InlineData("1.0.0")]
    [InlineData("1.2.9")]
    public void Check_GivenSamePlatformAndSdkTheHostCanServe_ReturnsTheManifest(string sdkVersion)
    {
        // Arrange
        var manifest = TempPayload.Manifest with { SdkVersion = sdkVersion };

        // Act
        var result = PluginCompatibility.Check(manifest, Host);

        // Assert
        Assert.Right(manifest, result);
    }

    [Fact]
    public void Check_GivenOtherPlatform_ReturnsWrongPlatform()
    {
        // Arrange
        var manifest = TempPayload.Manifest with { Platform = "linux-x64" };
        var expected = new WrongPlatform("linux-x64", "win-x64");

        // Act
        var result = PluginCompatibility.Check(manifest, Host);

        // Assert
        Assert.Left(expected, result);
    }

    [Theory]
    [InlineData("2.0.0")]
    [InlineData("0.9.0")]
    [InlineData("1.3.0")]
    public void Check_GivenSdkTheHostCannotServe_ReturnsIncompatibleSdk(string sdkVersion)
    {
        // Arrange
        var manifest = TempPayload.Manifest with { SdkVersion = sdkVersion };
        var expected = new IncompatibleSdk(sdkVersion, "1.2.0");

        // Act
        var result = PluginCompatibility.Check(manifest, Host);

        // Assert
        Assert.Left(expected, result);
    }
}

using System.Security.Cryptography;
using ArcaPhylax.PluginPackaging.Tests.Fakes;
using Xunit;

namespace ArcaPhylax.PluginPackaging.Tests;

public sealed class PluginStoreTests : IDisposable
{
    private static readonly HostEnvironment Host = new("win-x64", new Version(1, 0, 0));

    private readonly TempPayload _payload = new();
    private readonly TempStoreRoot _root = new();
    private readonly ECDsa _publisher = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    [Fact]
    public void Install_GivenValidPackage_ExtractsThePayloadIntoIdAndVersionDirectory()
    {
        // Arrange
        var sut = Store();
        using var package = _payload.Package(_publisher);
        var expectedFile = Path.Combine(_root.Path, "ArcaPhylax.Sample", "1.2.3", "runtimes", "win-x64", "native", "native.dll");

        // Act
        sut.Install(package);

        // Assert
        Assert.True(File.Exists(expectedFile));
    }

    [Fact]
    public void Install_GivenPluginNotInstalledBefore_ReturnsInstalledNowWithEntryAssemblyPath()
    {
        // Arrange
        var sut = Store();
        using var package = _payload.Package(_publisher);
        var expected = Path.Combine(_root.Path, "ArcaPhylax.Sample", "1.2.3", "Sample.dll");

        // Act
        var outcome = sut.Install(package);

        // Assert
        Assert.Equal(expected, Assert.IsType<InstalledNow>(Assert.Right(outcome)).Plugin.EntryAssemblyPath);
    }

    [Fact]
    public void Install_GivenPackageFromUntrustedPublisher_ReturnsInvalidPackageAndExtractsNothing()
    {
        // Arrange
        using var stranger = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var sut = Store();
        using var package = _payload.Package(stranger);

        // Act
        var outcome = sut.Install(package);

        // Assert
        Assert.IsType<InvalidPackage>(Assert.Left(outcome));
        Assert.Empty(sut.Installed());
    }

    [Fact]
    public void Install_GivenPackageForOtherPlatform_ReturnsWrongPlatform()
    {
        // Arrange
        var sut = Store();
        using var package = _payload.Package(TempPayload.Manifest with { Platform = "linux-x64" }, _publisher);
        var expected = new WrongPlatform("linux-x64", "win-x64");

        // Act
        var outcome = sut.Install(package);

        // Assert
        Assert.Left(expected, outcome);
    }

    [Fact]
    public void Install_GivenSameVersionTwice_ReturnsAlreadyInstalled()
    {
        // Arrange
        var sut = Store();
        using var first = _payload.Package(_publisher);
        sut.Install(first);
        using var second = _payload.Package(_publisher);
        var expected = new AlreadyInstalled("ArcaPhylax.Sample", "1.2.3");

        // Act
        var outcome = sut.Install(second);

        // Assert
        Assert.Left(expected, outcome);
    }

    [Fact]
    public void Install_GivenNewerVersionOfInstalledPlugin_StagesAnActivation()
    {
        // Arrange
        var sut = Store();
        using var v1 = _payload.Package(_publisher);
        sut.Install(v1);
        using var v2 = _payload.Package(TempPayload.Manifest with { Version = "2.0.0" }, _publisher);
        PendingOperation[] expected = [new Activate("ArcaPhylax.Sample", "2.0.0")];

        // Act
        var outcome = sut.Install(v2);

        // Assert
        Assert.IsType<UpdateStaged>(Assert.Right(outcome));
        Assert.Equal(expected, sut.Pending());
    }

    [Fact]
    public void ApplyPending_AfterStagedUpdate_KeepsOnlyTheNewVersion()
    {
        // Arrange
        var sut = Store();
        using var v1 = _payload.Package(_publisher);
        sut.Install(v1);
        using var v2 = _payload.Package(TempPayload.Manifest with { Version = "2.0.0" }, _publisher);
        sut.Install(v2);
        string[] expected = ["2.0.0"];

        // Act
        sut.ApplyPending();

        // Assert
        Assert.Equal(expected, Directory.GetDirectories(Path.Combine(_root.Path, "ArcaPhylax.Sample")).Select(Path.GetFileName));
        Assert.Empty(sut.Pending());
    }

    [Fact]
    public void ScheduleRemoval_ThenApplyPending_RemovesThePlugin()
    {
        // Arrange
        var sut = Store();
        using var package = _payload.Package(_publisher);
        sut.Install(package);

        // Act
        sut.ScheduleRemoval("ArcaPhylax.Sample");
        sut.ApplyPending();

        // Assert
        Assert.Empty(sut.Installed());
    }

    [Fact]
    public void ScheduleRemoval_KeepsThePluginInstalledUntilApplied()
    {
        // Arrange
        var sut = Store();
        using var package = _payload.Package(_publisher);
        sut.Install(package);

        // Act
        sut.ScheduleRemoval("ArcaPhylax.Sample");

        // Assert
        Assert.Single(sut.Installed());
    }

    [Fact]
    public void ScheduleRemoval_GivenUnknownId_ReturnsNotInstalled()
    {
        // Arrange
        var sut = Store();
        var expected = new NotInstalled("ArcaPhylax.Unknown");

        // Act
        var result = sut.ScheduleRemoval("ArcaPhylax.Unknown");

        // Assert
        Assert.Left(expected, result);
    }

    [Fact]
    public void Pending_IsPersistedAcrossStoreInstances()
    {
        // Arrange
        var first = Store();
        using var package = _payload.Package(_publisher);
        first.Install(package);
        first.ScheduleRemoval("ArcaPhylax.Sample");
        PendingOperation[] expected = [new Remove("ArcaPhylax.Sample")];

        // Act
        var pending = Store().Pending();

        // Assert
        Assert.Equal(expected, pending);
    }

    [Fact]
    public void Installed_ListsTheInstalledManifest()
    {
        // Arrange
        var sut = Store();
        using var package = _payload.Package(_publisher);
        sut.Install(package);

        // Act
        var installed = Assert.Single(sut.Installed());

        // Assert
        Assert.Equal(TempPayload.Manifest, installed.Manifest);
    }

    [Fact]
    public void Installed_GivenEmptyOrMissingRoot_ReturnsNothing()
    {
        // Arrange
        var sut = Store();

        // Act
        var installed = sut.Installed();

        // Assert
        Assert.Empty(installed);
    }

    public void Dispose()
    {
        _payload.Dispose();
        _root.Dispose();
        _publisher.Dispose();
    }

    private PluginStore Store() => new(_root.Path, Host, [PublisherKey.Of(_publisher)]);
}

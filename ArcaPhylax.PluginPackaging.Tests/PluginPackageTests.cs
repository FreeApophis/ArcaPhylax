using System.Security.Cryptography;
using ArcaPhylax.PluginPackaging.Tests.Fakes;
using Xunit;

namespace ArcaPhylax.PluginPackaging.Tests;

public sealed class PluginPackageTests : IDisposable
{
    private readonly TempPayload _payload = new();
    private readonly ECDsa _publisher = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    private PublisherKey[] Trusted => [PublisherKey.Of(_publisher)];

    [Fact]
    public void Verify_GivenPackageSignedByTrustedKey_ReturnsItsManifest()
    {
        // Arrange
        using var package = _payload.Package(_publisher);

        // Act
        var verified = PluginPackage.Verify(package, Trusted);

        // Assert
        Assert.Equal(TempPayload.Manifest, Assert.Right(verified).Manifest);
    }

    [Fact]
    public void Verify_GivenValidPackage_ListsAllPayloadFilesWithForwardSlashes()
    {
        // Arrange
        using var package = _payload.Package(_publisher);
        string[] expected = ["Sample.deps.json", "Sample.dll", "runtimes/win-x64/native/native.dll"];

        // Act
        var verified = PluginPackage.Verify(package, Trusted);

        // Assert
        Assert.Equal(expected, Assert.Right(verified).PayloadFiles.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Verify_GivenValidPackage_ReportsTheSigningKeyId()
    {
        // Arrange
        using var package = _payload.Package(_publisher);
        var expected = PublisherKey.Of(_publisher).KeyId;

        // Act
        var verified = PluginPackage.Verify(package, Trusted);

        // Assert
        Assert.Equal(expected, Assert.Right(verified).KeyId);
    }

    [Fact]
    public void Verify_GivenPackageSignedByUnknownKey_ReturnsUntrustedKey()
    {
        // Arrange
        using var stranger = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var package = _payload.Package(stranger);
        var expected = new UntrustedKey(PublisherKey.Of(stranger).KeyId);

        // Act
        var verified = PluginPackage.Verify(package, Trusted);

        // Assert
        Assert.Left(expected, verified);
    }

    [Fact]
    public void Verify_GivenModifiedPayloadFile_ReturnsInvalidSignature()
    {
        // Arrange
        using var original = _payload.Package(_publisher);
        using var tampered = ZipEdits.Replace(original, "payload/Sample.dll", "malicious bytes");

        // Act
        var verified = PluginPackage.Verify(tampered, Trusted);

        // Assert
        Assert.IsType<InvalidSignature>(Assert.Left(verified));
    }

    [Fact]
    public void Verify_GivenModifiedManifest_ReturnsInvalidSignature()
    {
        // Arrange
        using var original = _payload.Package(_publisher);
        var forged = (TempPayload.Manifest with { Version = "9.9.9" }).ToJson();
        using var tampered = ZipEdits.Replace(original, PluginManifest.FileName, forged);

        // Act
        var verified = PluginPackage.Verify(tampered, Trusted);

        // Assert
        Assert.IsType<InvalidSignature>(Assert.Left(verified));
    }

    [Fact]
    public void Verify_GivenAddedPayloadFile_ReturnsInvalidSignature()
    {
        // Arrange
        using var original = _payload.Package(_publisher);
        using var tampered = ZipEdits.Add(original, "payload/Injected.dll", "evil");

        // Act
        var verified = PluginPackage.Verify(tampered, Trusted);

        // Assert
        Assert.IsType<InvalidSignature>(Assert.Left(verified));
    }

    [Fact]
    public void Verify_GivenRemovedPayloadFile_ReturnsInvalidSignature()
    {
        // Arrange
        using var original = _payload.Package(_publisher);
        using var tampered = ZipEdits.Remove(original, "payload/Sample.deps.json");

        // Act
        var verified = PluginPackage.Verify(tampered, Trusted);

        // Assert
        Assert.IsType<InvalidSignature>(Assert.Left(verified));
    }

    [Fact]
    public void Verify_GivenPackageWithoutSignature_ReturnsMissingSignatureEntry()
    {
        // Arrange
        using var original = _payload.Package(_publisher);
        using var unsigned = ZipEdits.Remove(original, PluginPackage.SignatureFileName);
        var expected = new MissingEntry(PluginPackage.SignatureFileName);

        // Act
        var verified = PluginPackage.Verify(unsigned, Trusted);

        // Assert
        Assert.Left(expected, verified);
    }

    [Fact]
    public void Verify_GivenSignedManifestWhoseEntryAssemblyIsNotInThePayload_ReturnsMissingEntry()
    {
        // Arrange
        using var package = _payload.Package(TempPayload.Manifest with { EntryAssembly = "Other.dll" }, _publisher);
        var expected = new MissingEntry("payload/Other.dll");

        // Act
        var verified = PluginPackage.Verify(package, Trusted);

        // Assert
        Assert.Left(expected, verified);
    }

    [Theory]
    [InlineData("payload/../evil.dll")]
    [InlineData("../evil.dll")]
    [InlineData("/etc/evil")]
    [InlineData(@"payload\..\evil.dll")]
    [InlineData("C:/evil.dll")]
    [InlineData("elsewhere/evil.dll")]
    public void Verify_GivenEntryPathOutsideTheLayout_ReturnsUnsafeEntryPath(string entryName)
    {
        // Arrange
        using var package = ZipEdits.ZipWith(
            (PluginManifest.FileName, TempPayload.Manifest.ToJson()),
            (entryName, "evil"));
        var expected = new UnsafeEntryPath(entryName);

        // Act
        var verified = PluginPackage.Verify(package, Trusted);

        // Assert
        Assert.Left(expected, verified);
    }

    [Fact]
    public void Verify_GivenNonZipStream_ReturnsNotAPackage()
    {
        // Arrange
        using var garbage = new MemoryStream("definitely not a zip"u8.ToArray());

        // Act
        var verified = PluginPackage.Verify(garbage, Trusted);

        // Assert
        Assert.IsType<NotAPackage>(Assert.Left(verified));
    }

    public void Dispose()
    {
        _payload.Dispose();
        _publisher.Dispose();
    }
}

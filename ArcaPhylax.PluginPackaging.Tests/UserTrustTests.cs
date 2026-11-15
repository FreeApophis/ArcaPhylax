using ArcaPhylax.PluginPackaging.Tests.Fakes;
using Xunit;

namespace ArcaPhylax.PluginPackaging.Tests;

public sealed class UserTrustTests
{
    private static readonly PublisherKey Key = Keys.NewPublicKey();

    [Fact]
    public void Parse_GivenSerializedTrust_RoundTrips()
    {
        // Arrange
        var trust = UserTrust.Empty.Trust("Dev", Key).Block("abcdef0123456789");

        // Act
        var parsed = UserTrust.Parse(trust.ToJson());

        // Assert
        Assert.Equal(trust.Trusted, parsed.Trusted);
        Assert.Equal(trust.BlockedKeyIds, parsed.BlockedKeyIds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{}")]
    public void Parse_GivenMissingOrBrokenFile_ReturnsEmpty(string json)
    {
        // Act
        var parsed = UserTrust.Parse(json);

        // Assert
        Assert.Empty(parsed.Trusted);
        Assert.Empty(parsed.BlockedKeyIds);
    }

    [Fact]
    public void Trust_GivenABlockedKey_UnblocksIt()
    {
        // Arrange
        var blocked = UserTrust.Empty.Block(Key.KeyId);

        // Act
        var trusted = blocked.Trust("Dev", Key);

        // Assert
        Assert.Empty(trusted.BlockedKeyIds);
    }

    [Fact]
    public void Block_GivenATrustedKey_RemovesTheTrust()
    {
        // Arrange
        var trusted = UserTrust.Empty.Trust("Dev", Key);

        // Act
        var blocked = trusted.Block(Key.KeyId);

        // Assert
        Assert.Empty(blocked.Trusted);
    }

    [Fact]
    public void Forget_RemovesTrustAndBlock()
    {
        // Arrange
        var trust = UserTrust.Empty.Trust("Dev", Key).Block("abcdef0123456789");

        // Act
        var forgotten = trust.Forget(Key.KeyId).Forget("abcdef0123456789");

        // Assert
        Assert.Empty(forgotten.Trusted);
        Assert.Empty(forgotten.BlockedKeyIds);
    }
}

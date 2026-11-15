using System.Text;
using System.Text.Json.Nodes;
using ArcaPhylax.PluginPackaging.Tests.Fakes;
using Xunit;

namespace ArcaPhylax.PluginPackaging.Tests;

public sealed class PublisherListTests
{
    private static readonly Publisher Contoso = new("Contoso", Keys.NewPublicKey(), ["Contoso"]);

    [Fact]
    public void Verify_GivenAListSignedByTheRoot_ReturnsTheList()
    {
        // Arrange
        using var root = Keys.NewKey();
        var list = PublisherList.Empty.Endorse(Contoso);
        var signed = list.Sign(root);

        // Act
        var verified = PublisherList.Verify(signed, PublisherKey.Of(root));

        // Assert
        Assert.Equal(Contoso.Key, Assert.Right(verified).Publishers.Single().Key);
    }

    [Fact]
    public void Verify_GivenAListSignedByAnotherKey_ReturnsLeft()
    {
        // Arrange
        using var root = Keys.NewKey();
        using var impostor = Keys.NewKey();
        var signed = PublisherList.Empty.Endorse(Contoso).Sign(impostor);

        // Act
        var verified = PublisherList.Verify(signed, PublisherKey.Of(root));

        // Assert
        Assert.Left(verified);
    }

    [Fact]
    public void Verify_GivenATamperedPayload_ReturnsLeft()
    {
        // Arrange
        using var root = Keys.NewKey();
        var signed = PublisherList.Empty.Endorse(Contoso).Sign(root);
        var envelope = JsonNode.Parse(signed)!;
        var forged = PublisherList.Empty.Endorse(Contoso with { PluginPrefixes = ["ArcaPhylax"] });
        envelope["payload"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(forged.ToJson()));

        // Act
        var verified = PublisherList.Verify(envelope.ToJsonString(), PublisherKey.Of(root));

        // Assert
        Assert.Left(verified);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{"payload":"%%%","keyId":"x","signature":"y"}""")]
    public void Verify_GivenMalformedInput_ReturnsLeft(string json)
    {
        // Arrange
        var root = Keys.NewPublicKey();

        // Act
        var verified = PublisherList.Verify(json, root);

        // Assert
        Assert.Left(verified);
    }

    [Fact]
    public void Endorse_RaisesTheSequence()
    {
        // Act
        var list = PublisherList.Empty.Endorse(Contoso);

        // Assert
        Assert.Equal(PublisherList.Empty.Sequence + 1, list.Sequence);
    }

    [Fact]
    public void Endorse_GivenTheSameKeyAgain_ReplacesTheEntry()
    {
        // Arrange
        var list = PublisherList.Empty.Endorse(Contoso);
        var renamed = Contoso with { Name = "Contoso Ltd" };

        // Act
        var updated = list.Endorse(renamed);

        // Assert
        Assert.Equal(["Contoso Ltd"], updated.Publishers.Select(publisher => publisher.Name));
    }

    [Fact]
    public void Revoke_RemovesThePublisherAndListsItsKeyAsRevoked()
    {
        // Arrange
        var list = PublisherList.Empty.Endorse(Contoso);

        // Act
        var revoked = list.Revoke(Contoso.Key.KeyId);

        // Assert
        Assert.Empty(revoked.Publishers);
        Assert.Equal([Contoso.Key.KeyId], revoked.RevokedKeyIds);
    }

    [Fact]
    public void Endorse_GivenARevokedKey_ClearsTheRevocation()
    {
        // Arrange
        var revoked = PublisherList.Empty.Endorse(Contoso).Revoke(Contoso.Key.KeyId);

        // Act
        var endorsed = revoked.Endorse(Contoso);

        // Assert
        Assert.Empty(endorsed.RevokedKeyIds);
    }
}

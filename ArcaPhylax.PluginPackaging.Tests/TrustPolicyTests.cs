using ArcaPhylax.PluginPackaging.Tests.Fakes;
using Xunit;

namespace ArcaPhylax.PluginPackaging.Tests;

public sealed class TrustPolicyTests
{
    private static readonly Publisher FirstParty = new("ArcaPhylax", Keys.NewPublicKey(), ["ArcaPhylax"]);
    private static readonly Publisher Contoso = new("Contoso", Keys.NewPublicKey(), ["Contoso"]);
    private static readonly PublisherKey Developer = Keys.NewPublicKey();

    [Fact]
    public void Check_GivenABuiltInPublisherSigningItsOwnId_ReturnsEndorsed()
    {
        // Act
        var result = Policy().Check("ArcaPhylax.Scanner", FirstParty.Key);

        // Assert
        Assert.Right<TrustError, TrustedSigner>(new EndorsedSigner(FirstParty), result);
    }

    [Fact]
    public void Check_GivenAListedPublisherSigningItsOwnId_ReturnsEndorsed()
    {
        // Act
        var result = Policy().Check("Contoso.Hello", Contoso.Key);

        // Assert
        Assert.Right<TrustError, TrustedSigner>(new EndorsedSigner(Contoso), result);
    }

    [Fact]
    public void Check_GivenAnEndorsedPublisherSigningAnotherPublishersId_ReturnsNotOwner()
    {
        // Arrange
        var expected = new NotOwner("ArcaPhylax.Scanner", "Contoso");

        // Act
        var result = Policy().Check("ArcaPhylax.Scanner", Contoso.Key);

        // Assert
        Assert.Left(expected, result);
    }

    [Fact]
    public void Check_GivenAnUnknownKey_ReturnsUnknownPublisherWithTheKey()
    {
        // Arrange
        var expected = new UnknownPublisher(Developer);

        // Act
        var result = Policy().Check("Dev.Hello", Developer);

        // Assert
        Assert.Left(expected, result);
    }

    [Fact]
    public void Check_GivenAKeyTheUserTrusts_ReturnsTrustedByUser()
    {
        // Arrange
        var user = UserTrust.Empty.Trust("Me", Developer);

        // Act
        var result = Policy(user: user).Check("Dev.Hello", Developer);

        // Assert
        Assert.Right<TrustError, TrustedSigner>(new UserTrustedSigner(new UserTrustedKey("Me", Developer)), result);
    }

    [Fact]
    public void Check_GivenAUserTrustedKeySigningAnEndorsedPublishersId_ReturnsNotOwner()
    {
        // Arrange
        var user = UserTrust.Empty.Trust("Me", Developer);
        var expected = new NotOwner("Contoso.Hello", "Me");

        // Act
        var result = Policy(user: user).Check("Contoso.Hello", Developer);

        // Assert
        Assert.Left(expected, result);
    }

    [Fact]
    public void Check_GivenAnEndorsedPublisherTheUserBlocked_ReturnsPublisherBlocked()
    {
        // Arrange
        var user = UserTrust.Empty.Block(Contoso.Key.KeyId);
        var expected = new PublisherBlocked(Contoso.Key.KeyId);

        // Act
        var result = Policy(user: user).Check("Contoso.Hello", Contoso.Key);

        // Assert
        Assert.Left(expected, result);
    }

    [Fact]
    public void Check_GivenARevokedBuiltInKey_ReturnsPublisherRevoked()
    {
        // Arrange
        var list = PublisherList.Empty.Revoke(FirstParty.Key.KeyId);
        var expected = new PublisherRevoked(FirstParty.Key.KeyId);

        // Act
        var result = Policy(list: list).Check("ArcaPhylax.Scanner", FirstParty.Key);

        // Assert
        Assert.Left(expected, result);
    }

    [Fact]
    public void Check_GivenARevokedKeyTheUserTrusts_StillReturnsPublisherRevoked()
    {
        // Arrange
        var list = PublisherList.Empty.Revoke(Developer.KeyId);
        var user = UserTrust.Empty.Trust("Me", Developer);
        var expected = new PublisherRevoked(Developer.KeyId);

        // Act
        var result = Policy(list, user).Check("Dev.Hello", Developer);

        // Assert
        Assert.Left(expected, result);
    }

    [Fact]
    public void Check_GivenAListedPublisherWithABuiltInKey_UsesTheListedPrefixes()
    {
        // Arrange
        var list = PublisherList.Empty.Endorse(FirstParty with { PluginPrefixes = ["ArcaPhylax", "Arca"] });

        // Act
        var result = Policy(list: list).Check("Arca.Extra", FirstParty.Key);

        // Assert
        Assert.Right(result);
    }

    private static TrustPolicy Policy(PublisherList? list = null, UserTrust? user = null)
        => TrustPolicy.Of([FirstParty], list ?? PublisherList.Empty.Endorse(Contoso), user ?? UserTrust.Empty);
}

using ArcaPhylax.PluginPackaging.Tests.Fakes;
using Xunit;

namespace ArcaPhylax.PluginPackaging.Tests;

public sealed class PublisherStandingTests
{
    private static readonly Publisher Contoso = new("Contoso", Keys.NewPublicKey(), ["Contoso"]);
    private static readonly Publisher Fabrikam = new("Fabrikam", Keys.NewPublicKey(), ["Fabrikam"]);
    private static readonly PublisherKey Developer = Keys.NewPublicKey();

    [Fact]
    public void Standings_ListsEndorsedPublishers()
    {
        // Arrange
        var policy = TrustPolicy.Of([Contoso], PublisherList.Empty, UserTrust.Empty);

        // Act
        var standings = policy.Standings();

        // Assert
        Assert.Equal([new EndorsedPublisher(Contoso)], standings);
    }

    [Fact]
    public void Standings_ListsKeysTheUserTrusts()
    {
        // Arrange
        var policy = TrustPolicy.Of([], PublisherList.Empty, UserTrust.Empty.Trust("Me", Developer));

        // Act
        var standings = policy.Standings();

        // Assert
        Assert.Equal([new UserTrustedPublisher(new UserTrustedKey("Me", Developer))], standings);
    }

    [Fact]
    public void Standings_ShowsABlockedEndorsedPublisherOnlyAsBlockedWithItsName()
    {
        // Arrange
        var policy = TrustPolicy.Of([Contoso, Fabrikam], PublisherList.Empty, UserTrust.Empty.Block(Contoso.Key.KeyId));
        PublisherStanding[] expected = [new BlockedPublisher(Contoso.Key.KeyId, Option.Some("Contoso")), new EndorsedPublisher(Fabrikam)];

        // Act
        var standings = policy.Standings();

        // Assert
        Assert.Equal(expected, standings);
    }

    [Fact]
    public void Standings_ShowsABlockedUnknownKeyWithoutAName()
    {
        // Arrange
        var policy = TrustPolicy.Of([], PublisherList.Empty, UserTrust.Empty.Block("0123456789abcdef"));

        // Act
        var standings = policy.Standings();

        // Assert
        Assert.Equal([new BlockedPublisher("0123456789abcdef", Option<string>.None)], standings);
    }
}

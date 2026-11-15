using ArcaPhylax.PluginPackaging.Tests.Fakes;
using Xunit;

namespace ArcaPhylax.PluginPackaging.Tests;

public sealed class PublisherTests
{
    private static readonly Publisher Contoso = new("Contoso", Keys.NewPublicKey(), ["Contoso", "Fabrikam.Tools"]);

    [Theory]
    [InlineData("Contoso")]
    [InlineData("Contoso.Hello")]
    [InlineData("Contoso.Hello.World")]
    [InlineData("contoso.hello")]
    [InlineData("Fabrikam.Tools.Scan")]
    public void Owns_GivenThePrefixOrAnIdBelowIt_ReturnsTrue(string pluginId)
    {
        // Act
        var owns = Contoso.Owns(pluginId);

        // Assert
        Assert.True(owns);
    }

    [Theory]
    [InlineData("ContosoX")]
    [InlineData("ContosoX.Hello")]
    [InlineData("Fabrikam")]
    [InlineData("Fabrikam.ToolsX")]
    [InlineData("ArcaPhylax.Contoso")]
    public void Owns_GivenAnIdOutsideItsPrefixes_ReturnsFalse(string pluginId)
    {
        // Act
        var owns = Contoso.Owns(pluginId);

        // Assert
        Assert.False(owns);
    }
}

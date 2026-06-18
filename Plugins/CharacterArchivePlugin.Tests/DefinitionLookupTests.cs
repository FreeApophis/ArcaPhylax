using CharacterArchivePlugin.Data;
using Funcky.Monads;
using Xunit;

namespace CharacterArchivePlugin.Tests;

public sealed class DefinitionLookupTests
{
    [Fact]
    public void ForKey_GivenKnownSourceKey_ReturnsTableColumnAndPrimaryKey()
    {
        // Arrange
        var key = "chub:12345";

        // Act
        var result = DefinitionLookup.ForKey(key);

        // Assert
        Assert.Equal(Option.Some(new DefinitionLookup("chub_character_def", "increment", "12345")), result);
    }

    [Fact]
    public void ForKey_GivenKeyWhosePrimaryKeyContainsColons_KeepsTheRemainderAsPrimaryKey()
    {
        // Arrange
        var key = "generic:ab:cd";

        // Act
        var result = DefinitionLookup.ForKey(key);

        // Assert
        Assert.Equal(Option.Some(new DefinitionLookup("generic_character_def", "card_data_hash", "ab:cd")), result);
    }

    [Theory]
    [InlineData("unknown:1")]   // source not in the whitelist
    [InlineData("nocolon")]     // no separator
    [InlineData(":1")]          // empty source
    [InlineData("chub:")]       // empty primary key
    public void ForKey_GivenMalformedOrUnknownKey_ReturnsNone(string key)
    {
        // Act
        var result = DefinitionLookup.ForKey(key);

        // Assert
        Assert.Equal(Option<DefinitionLookup>.None, result);
    }

    [Fact]
    public void ForKey_GivenNull_ReturnsNone()
    {
        // Act
        var result = DefinitionLookup.ForKey(null);

        // Assert
        Assert.Equal(Option<DefinitionLookup>.None, result);
    }
}

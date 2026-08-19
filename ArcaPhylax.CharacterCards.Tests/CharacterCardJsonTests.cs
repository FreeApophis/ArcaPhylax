using System.Collections.Immutable;
using System.Text.Json;
using ArcaPhylax.CharacterCards.Cards;
using Xunit;

namespace ArcaPhylax.CharacterCards.Tests;

public sealed class CharacterCardJsonTests
{
    [Fact]
    public void Read_WhenV2Card_ReadsCoreFieldsFromTheDataObject()
    {
        // Arrange
        var json = """
            {"spec":"chara_card_v2","data":{"name":"Ada","description":"An analyst","first_mes":"Hello","tags":["science","math"]}}
            """;

        // Act
        var card = CharacterCardJson.Read(json);

        // Assert
        Assert.Equal(CardSpec.V2, card.Spec);
        Assert.Equal("Ada", card.Name);
        Assert.Equal("Hello", card.FirstMessage);
        Assert.Equal(new[] { "science", "math" }, card.Tags.ToArray());
    }

    [Fact]
    public void Read_WhenV1Card_ReadsCoreFieldsFromTheFlatRoot()
    {
        // Arrange
        var json = """{"name":"Bo","description":"A guide","personality":"kind","first_mes":"Hi"}""";

        // Act
        var card = CharacterCardJson.Read(json);

        // Assert
        Assert.Equal(CardSpec.V1, card.Spec);
        Assert.Equal("Bo", card.Name);
        Assert.Equal("kind", card.Personality);
    }

    [Fact]
    public void Read_WhenDefinitionIsNull_ReturnsEmpty()
    {
        // Act
        var card = CharacterCardJson.Read(null);

        // Assert
        Assert.Equal(CharacterCard.Empty, card);
    }

    [Fact]
    public void Apply_WhenV2Card_WritesEditsIntoDataAndPreservesUnknownFields()
    {
        // Arrange
        var original = """
            {"spec":"chara_card_v2","data":{"name":"Old","mes_example":"keep me","extensions":{"x":1}}}
            """;
        var edited = CharacterCard.Empty with { Spec = CardSpec.V2, Name = "New", Description = "Fresh" };

        // Act
        var result = CharacterCardJson.Apply(original, edited);

        // Assert
        using var document = JsonDocument.Parse(result);
        var data = document.RootElement.GetProperty("data");
        Assert.Equal("New", data.GetProperty("name").GetString());
        Assert.Equal("Fresh", data.GetProperty("description").GetString());
        Assert.Equal("keep me", data.GetProperty("mes_example").GetString());
        Assert.Equal(1, data.GetProperty("extensions").GetProperty("x").GetInt32());
    }

    [Fact]
    public void Apply_WhenV1Card_WritesEditsAtTheRoot()
    {
        // Arrange
        var original = """{"name":"Old","custom":"kept"}""";
        var edited = CharacterCard.Empty with { Spec = CardSpec.V1, Name = "New" };

        // Act
        var result = CharacterCardJson.Apply(original, edited);

        // Assert
        using var document = JsonDocument.Parse(result);
        Assert.Equal("New", document.RootElement.GetProperty("name").GetString());
        Assert.Equal("kept", document.RootElement.GetProperty("custom").GetString());
    }

    [Fact]
    public void Apply_ThenRead_RoundTripsEditedCoreFields()
    {
        // Arrange
        var original = """{"spec":"chara_card_v2","data":{"name":"Old"}}""";
        var edited = new CharacterCard(
            CardSpec.V2,
            "Cass",
            "desc",
            "curious",
            "a lab",
            "greeting",
            "1.2",
            ["alpha", "beta"]);

        // Act
        var reread = CharacterCardJson.Read(CharacterCardJson.Apply(original, edited));

        // Assert
        // Compared field-by-field: a record's auto Equals uses ImmutableArray's reference equality,
        // so two equal-content Tags would otherwise report unequal.
        Assert.Equal(edited with { Tags = [] }, reread with { Tags = [] });
        Assert.Equal(edited.Tags.ToArray(), reread.Tags.ToArray());
    }
}

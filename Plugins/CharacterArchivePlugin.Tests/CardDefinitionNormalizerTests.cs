using System.Text.Json;
using CharacterArchivePlugin.Data;
using Xunit;

namespace CharacterArchivePlugin.Tests;

public sealed class CardDefinitionNormalizerTests
{
    [Fact]
    public void Normalize_GivenV2CardWithNullCharacterBook_RemovesTheField()
    {
        // Arrange
        var json = """{"spec":"chara_card_v2","data":{"name":"Ada","character_book":null}}""";

        // Act
        var result = CardDefinitionNormalizer.Normalize(json);

        // Assert
        using var document = JsonDocument.Parse(result);
        Assert.False(document.RootElement.GetProperty("data").TryGetProperty("character_book", out _));
    }

    [Fact]
    public void Normalize_GivenV2CardWithNullArrays_CoercesThemToEmptyArrays()
    {
        // Arrange
        var json = """{"spec":"chara_card_v2","data":{"name":"Ada","tags":null,"alternate_greetings":null}}""";

        // Act
        var result = CardDefinitionNormalizer.Normalize(json);

        // Assert
        var data = JsonDocument.Parse(result).RootElement.GetProperty("data");
        Assert.Equal(JsonValueKind.Array, data.GetProperty("tags").ValueKind);
        Assert.Equal(JsonValueKind.Array, data.GetProperty("alternate_greetings").ValueKind);
        Assert.Equal(0, data.GetProperty("tags").GetArrayLength());
    }

    [Fact]
    public void Normalize_GivenV2CardWithNullExtensions_CoercesItToAnEmptyObject()
    {
        // Arrange
        var json = """{"spec":"chara_card_v2","data":{"name":"Ada","extensions":null}}""";

        // Act
        var result = CardDefinitionNormalizer.Normalize(json);

        // Assert
        var data = JsonDocument.Parse(result).RootElement.GetProperty("data");
        Assert.Equal(JsonValueKind.Object, data.GetProperty("extensions").ValueKind);
    }

    [Fact]
    public void Normalize_GivenValidV2Card_LeavesPopulatedFieldsUntouched()
    {
        // Arrange
        var json = """{"spec":"chara_card_v2","data":{"name":"Ada","tags":["x"],"character_book":{"entries":[]}}}""";

        // Act
        var result = CardDefinitionNormalizer.Normalize(json);

        // Assert
        var data = JsonDocument.Parse(result).RootElement.GetProperty("data");
        Assert.Equal("x", data.GetProperty("tags")[0].GetString());
        Assert.Equal(JsonValueKind.Object, data.GetProperty("character_book").ValueKind);
    }

    [Fact]
    public void Normalize_GivenV1Card_ReturnsItUnchanged()
    {
        // Arrange — a flat V1 card has no data wrapper and must not be rewritten.
        var json = """{"name":"Ada","description":"d","character_book":null}""";

        // Act
        var result = CardDefinitionNormalizer.Normalize(json);

        // Assert
        Assert.Equal(json, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("{ not json")]
    public void Normalize_GivenEmptyOrUnparseableInput_ReturnsItUnchanged(string? json)
    {
        // Act
        var result = CardDefinitionNormalizer.Normalize(json);

        // Assert
        Assert.Equal(json ?? string.Empty, result);
    }
}

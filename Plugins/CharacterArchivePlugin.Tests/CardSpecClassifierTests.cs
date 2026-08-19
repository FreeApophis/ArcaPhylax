using ArcaPhylax.CharacterCards.Cards;
using Xunit;

namespace CharacterArchivePlugin.Tests;

public sealed class CardSpecClassifierTests
{
    [Fact]
    public void Classify_GivenV2WrapperWithDataAndSpec_ReturnsV2()
    {
        // Arrange
        var json = """{"spec":"chara_card_v2","spec_version":"2.0","data":{"name":"Ada"}}""";

        // Act
        var result = CardSpecClassifier.Classify(json);

        // Assert
        Assert.Equal(CardSpec.V2, result);
    }

    [Fact]
    public void Classify_GivenFlatV1Card_ReturnsV1()
    {
        // Arrange
        var json = """{"name":"Ada","description":"d","personality":"p","first_mes":"hi"}""";

        // Act
        var result = CardSpecClassifier.Classify(json);

        // Assert
        Assert.Equal(CardSpec.V1, result);
    }

    [Fact]
    public void Classify_GivenV3Wrapper_ReadsAsV2()
    {
        // Arrange — v3 also nests under data; for the two-state cue it counts as the modern spec.
        var json = """{"spec":"chara_card_v3","data":{"name":"Ada"}}""";

        // Act
        var result = CardSpecClassifier.Classify(json);

        // Assert
        Assert.Equal(CardSpec.V2, result);
    }

    [Fact]
    public void Classify_GivenDataObjectButNoSpecField_ReturnsV1()
    {
        // Arrange — without the spec marker we cannot claim V2.
        var json = """{"data":{"name":"Ada"}}""";

        // Act
        var result = CardSpecClassifier.Classify(json);

        // Assert
        Assert.Equal(CardSpec.V1, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("{ this is not json")]
    [InlineData("\"just a string\"")]
    public void Classify_GivenEmptyOrUnparseableJson_ReturnsV1(string? json)
    {
        // Act
        var result = CardSpecClassifier.Classify(json);

        // Assert
        Assert.Equal(CardSpec.V1, result);
    }
}

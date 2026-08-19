using System.Text.Json;
using ArcaPhylax.CharacterCards.Cards;
using ArcaPhylax.CharacterCards.Png;
using Xunit;

namespace CharacterArchivePlugin.Tests;

/// <summary>
/// Regression coverage anchored to <c>NotWorking.png</c> — a real exported card a strict V2 editor
/// rejected. Its embedding is fine (tEXt/chara, spec chara_card_v2); the defect is <c>data.character_book:
/// null</c> in the stored definition. The export path must scrub that before embedding.
/// </summary>
public sealed class NotWorkingCardExportTests
{
    [Fact]
    public void TheRejectedCard_CarriesANullCharacterBook()
    {
        // Act
        var data = JsonDocument.Parse(CharaOf(RejectedCard())).RootElement.GetProperty("data");

        // Assert — documents the exact defect the fix targets.
        Assert.True(data.TryGetProperty("character_book", out var book));
        Assert.Equal(JsonValueKind.Null, book.ValueKind);
    }

    [Fact]
    public void Exporting_RunsTheDefinitionThroughNormalisation_DroppingTheNullCharacterBook()
    {
        // Arrange
        var original = CharaOf(RejectedCard());

        // Act — the exact pipeline BuildCardAsync runs: normalise, then embed.
        var exported = CharacterCardPng.EmbedCharacterJson(RejectedCard(), CardDefinitionNormalizer.Normalize(original));
        var data = JsonDocument.Parse(CharaOf(exported)).RootElement.GetProperty("data");

        // Assert — no null character_book, real content preserved.
        Assert.False(data.TryGetProperty("character_book", out _));
        Assert.Equal(JsonValueKind.Array, data.GetProperty("tags").ValueKind);
        Assert.Equal(JsonValueKind.Array, data.GetProperty("alternate_greetings").ValueKind);
    }

    [Fact]
    public void Exporting_KeepsTheCardAValidV2Png()
    {
        // Act
        var exported = CharacterCardPng.EmbedCharacterJson(RejectedCard(), CardDefinitionNormalizer.Normalize(CharaOf(RejectedCard())));

        // Assert
        Assert.Equal("chara_card_v2", JsonDocument.Parse(CharaOf(exported)).RootElement.GetProperty("spec").GetString());
        using var image = SixLabors.ImageSharp.Image.Load(exported);
        Assert.True(image.Width > 0 && image.Height > 0);
    }

    private static byte[] RejectedCard() =>
    File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "NotWorking.png"));

    private static string CharaOf(byte[] card) =>
        CharacterCardPng.ReadCharacterJson(card)
            .GetOrElse(() => throw new Xunit.Sdk.XunitException("card had no chara chunk"));
}

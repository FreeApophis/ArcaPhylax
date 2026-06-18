using CharacterArchivePlugin.Services;
using FsCheck;
using FsCheck.Xunit;
using Funcky.Monads;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace CharacterArchivePlugin.Tests;

public sealed class CharacterCardWriterTests
{
    [Fact]
    public void EmbedCharacterJson_GivenJson_RoundTripsThroughTheCharaChunk()
    {
        // Arrange
        var image = SamplePng();
        var json = """{"name":"Ada","spec":"chara_card_v2"}""";

        // Act
        var card = CharacterCardWriter.EmbedCharacterJson(image, json);
        var read = CharacterCardWriter.ReadCharacterJson(card);

        // Assert
        Assert.Equal(Option.Some(json), read);
    }

    [Fact]
    public void EmbedCharacterJson_GivenImageThatAlreadyCarriesChara_ReplacesItWithTheNewJson()
    {
        // Arrange
        var first = CharacterCardWriter.EmbedCharacterJson(SamplePng(), """{"version":1}""");

        // Act
        var second = CharacterCardWriter.EmbedCharacterJson(first, """{"version":2}""");

        // Assert
        Assert.Equal(Option.Some("""{"version":2}"""), CharacterCardWriter.ReadCharacterJson(second));
    }

    [Fact]
    public void ReadCharacterJson_GivenPngWithoutCharaChunk_ReturnsNone()
    {
        // Arrange
        var plain = SamplePng();

        // Act
        var read = CharacterCardWriter.ReadCharacterJson(plain);

        // Assert
        Assert.Equal(Option<string>.None, read);
    }

    [Property]
    public void EmbedCharacterJson_GivenAnyNonEmptyJsonText_IsRecoverableFromTheCard(NonEmptyString json)
    {
        // Act
        var card = CharacterCardWriter.EmbedCharacterJson(SamplePng(), json.Get);

        // Assert
        Assert.Equal(Option.Some(json.Get), CharacterCardWriter.ReadCharacterJson(card));
    }

    [Fact]
    public void EmbedCharacterJson_GivenJsonLargerThanTheCompressionThreshold_WritesUncompressedTextChunk()
    {
        // Arrange — base64 of this far exceeds ImageSharp's 1024-char text-compression threshold, so a
        // naive SaveAsPng emits a zTXt chunk that TavernCard loaders cannot read. We require tEXt.
        var json = "{\"spec\":\"chara_card_v2\",\"data\":{\"description\":\"" + new string('x', 4000) + "\"}}";

        // Act
        var card = CharacterCardWriter.EmbedCharacterJson(SamplePng(), json);
        var chunks = PngChunkTypes(card);

        // Assert
        Assert.Contains("tEXt", chunks);
        Assert.DoesNotContain("zTXt", chunks);
    }

    [Fact]
    public void EmbedCharacterJson_GivenJson_StoresTheCharaKeywordWithBase64JsonInTheTextChunk()
    {
        // Arrange
        var json = "{\"spec\":\"chara_card_v2\",\"data\":{\"name\":\"" + new string('y', 4000) + "\"}}";

        // Act
        var card = CharacterCardWriter.EmbedCharacterJson(SamplePng(), json);
        var (keyword, value) = TextChunk(card);

        // Assert
        Assert.Equal("chara", keyword);
        Assert.Equal(json, System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value)));
    }

    private static byte[] SamplePng()
    {
        using var image = new Image<Rgba32>(2, 2);
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    // Minimal PNG chunk walker: 8-byte signature, then [length:4 BE][type:4][data:length][crc:4].
    private static IReadOnlyList<string> PngChunkTypes(byte[] png)
    {
        var types = new List<string>();
        var offset = 8;
        while (offset + 8 <= png.Length)
        {
            var length = (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];
            var type = System.Text.Encoding.ASCII.GetString(png, offset + 4, 4);
            types.Add(type);
            offset += 12 + length;
            if (type == "IEND")
            {
                break;
            }
        }

        return types;
    }

    private static (string Keyword, string Value) TextChunk(byte[] png)
    {
        var offset = 8;
        while (offset + 8 <= png.Length)
        {
            var length = (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];
            var type = System.Text.Encoding.ASCII.GetString(png, offset + 4, 4);
            if (type == "tEXt")
            {
                var data = png.AsSpan(offset + 8, length);
                var separator = data.IndexOf((byte)0);
                var keyword = System.Text.Encoding.ASCII.GetString(data[..separator]);
                var value = System.Text.Encoding.ASCII.GetString(data[(separator + 1)..]);
                return (keyword, value);
            }

            offset += 12 + length;
        }

        throw new InvalidOperationException("No tEXt chunk found.");
    }
}

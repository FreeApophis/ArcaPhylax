using System.Text;
using System.Text.Json;
using CharacterArchivePlugin.Data;
using CharacterArchivePlugin.Services;
using Funcky.Monads;
using Xunit;

namespace CharacterArchivePlugin.Tests;

/// <summary>
/// End-to-end coverage anchored to <c>Holodeck.png</c>, a known-good TavernCard V2 export. These
/// tests deliberately read the embedded data with an independent PNG walker — base64 out of an
/// uncompressed <c>tEXt</c>/<c>chara</c> chunk, exactly as SillyTavern and card editors do — rather
/// than ImageSharp's own reader, which would happily decode a <c>zTXt</c> chunk that real tooling
/// cannot. That is the difference that made earlier exports unopenable.
/// </summary>
public sealed class HolodeckCardExportTests
{
    private static byte[] ReferenceCard() =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Holodeck.png"));

    [Fact]
    public void ReadCharacterJson_GivenRealReferenceCard_ExtractsTheV2Definition()
    {
        // Act
        var json = CharacterCardWriter.ReadCharacterJson(ReferenceCard())
            .GetOrElse(() => throw new Xunit.Sdk.XunitException("reference card had no chara chunk"));

        // Assert
        using var document = JsonDocument.Parse(json);
        Assert.Equal("chara_card_v2", document.RootElement.GetProperty("spec").GetString());
        Assert.Equal("Holodeck", document.RootElement.GetProperty("data").GetProperty("name").GetString());
    }

    [Fact]
    public void Classify_GivenTheRealReferenceCardDefinition_ReturnsV2()
    {
        // Arrange
        var json = CharacterCardWriter.ReadCharacterJson(ReferenceCard())
            .GetOrElse(() => throw new Xunit.Sdk.XunitException("reference card had no chara chunk"));

        // Act
        var spec = CardSpecClassifier.Classify(json);

        // Assert
        Assert.Equal(CardSpec.V2, spec);
    }

    [Fact]
    public void ExportedCard_IsReadableByAnIndependentTextChunkParser()
    {
        // Arrange — the JSON a real, openable card carries.
        var expected = CharacterCardWriter.ReadCharacterJson(ReferenceCard())
            .GetOrElse(() => throw new Xunit.Sdk.XunitException("reference card had no chara chunk"));

        // Act — re-export it through the writer the app uses, then read it back the way tooling does.
        var exported = CharacterCardWriter.EmbedCharacterJson(ReferenceCard(), expected);
        var roundTripped = CharaFromTextChunk(exported);

        // Assert
        Assert.Equal(expected, roundTripped);
    }

    [Fact]
    public void ExportedCard_StaysAValidPngImage()
    {
        // Arrange
        var json = CharacterCardWriter.ReadCharacterJson(ReferenceCard())
            .GetOrElse(() => throw new Xunit.Sdk.XunitException("reference card had no chara chunk"));

        // Act
        var exported = CharacterCardWriter.EmbedCharacterJson(ReferenceCard(), json);

        // Assert — decodes as an image (the picture half of the card is intact).
        using var image = SixLabors.ImageSharp.Image.Load(exported);
        Assert.True(image.Width > 0 && image.Height > 0);
    }

    [Fact]
    public void ExportedCard_LeavesEveryNonCharaChunkOfThePngSourceUntouched()
    {
        // Arrange
        var json = CharacterCardWriter.ReadCharacterJson(ReferenceCard())
            .GetOrElse(() => throw new Xunit.Sdk.XunitException("reference card had no chara chunk"));

        // Act — re-exporting must not re-encode the image (which would drop sRGB, rewrite eXIf, etc.).
        var exported = CharacterCardWriter.EmbedCharacterJson(ReferenceCard(), json);

        // Assert — every image/ancillary chunk survives byte-for-byte and in order.
        Assert.Equal(NonCharaChunks(ReferenceCard()), NonCharaChunks(exported));
    }

    [Fact]
    public void ExportedCard_PlacesTheCharaChunkAfterTheImageData()
    {
        // Arrange
        var json = CharacterCardWriter.ReadCharacterJson(ReferenceCard())
            .GetOrElse(() => throw new Xunit.Sdk.XunitException("reference card had no chara chunk"));

        // Act
        var types = ChunkTypes(CharacterCardWriter.EmbedCharacterJson(ReferenceCard(), json));

        // Assert — card tools append chara after IDAT; some editors only scan there.
        Assert.True(types.IndexOf("tEXt") > types.LastIndexOf("IDAT"), "chara tEXt must follow IDAT");
    }

    private static List<string> ChunkTypes(byte[] png) =>
        Chunks(png).Select(chunk => chunk.Type).ToList();

    private static List<(string Type, string Data)> NonCharaChunks(byte[] png) =>
        Chunks(png)
            .Where(chunk => !(chunk.Type == "tEXt" && chunk.Data.StartsWith("chara\0", StringComparison.Ordinal)))
            .Select(chunk => (chunk.Type, Convert.ToBase64String(Encoding.Latin1.GetBytes(chunk.Data))))
            .ToList();

    private static IEnumerable<(string Type, string Data)> Chunks(byte[] png)
    {
        var offset = 8;
        while (offset + 8 <= png.Length)
        {
            var length = (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            yield return (type, Encoding.Latin1.GetString(png, offset + 8, length));
            offset += 12 + length;
            if (type == "IEND")
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Independent reader mirroring third-party loaders: walk PNG chunks, take the first uncompressed
    /// <c>tEXt</c> chunk keyed <c>chara</c>, and base64-decode its value. No zTXt/iTXt support — if the
    /// writer ever emits a compressed chunk again, this throws and the test fails.
    /// </summary>
    private static string CharaFromTextChunk(byte[] png)
    {
        var offset = 8; // PNG signature
        while (offset + 8 <= png.Length)
        {
            var length = (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            if (type == "tEXt")
            {
                var body = png.AsSpan(offset + 8, length);
                var separator = body.IndexOf((byte)0);
                var keyword = Encoding.ASCII.GetString(body[..separator]);
                if (keyword == "chara")
                {
                    var value = Encoding.ASCII.GetString(body[(separator + 1)..]);
                    return Encoding.UTF8.GetString(Convert.FromBase64String(value));
                }
            }

            offset += 12 + length;
        }

        throw new Xunit.Sdk.XunitException("no tEXt 'chara' chunk in exported card");
    }
}

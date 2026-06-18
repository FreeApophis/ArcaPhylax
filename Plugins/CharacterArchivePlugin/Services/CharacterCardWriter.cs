using System.Buffers.Binary;
using System.Text;
using Funcky.Monads;
using SixLabors.ImageSharp;

namespace CharacterArchivePlugin.Services;

/// <summary>
/// Writes and reads the TavernCard PNG encoding: the character <c>definition</c> JSON is base64
/// encoded and stored in a PNG <c>tEXt</c> chunk keyed <c>chara</c>. TavernCard V1 and V2 differ only
/// in the shape of that JSON — the PNG embedding is identical — so the stored definition is embedded
/// verbatim. Base64 keeps the chunk value within the Latin-1 alphabet a <c>tEXt</c> chunk requires.
/// </summary>
/// <remarks>
/// When the source image is already a PNG the original bytes are left untouched and the <c>chara</c>
/// chunk is spliced in just before <c>IEND</c> — exactly how real card tools build a card. Re-encoding
/// the whole image through an imaging library instead (dropping <c>sRGB</c>, rewriting <c>eXIf</c>,
/// reordering chunks so <c>tEXt</c> lands before <c>IDAT</c>) produced cards that loaded in SillyTavern
/// but not in stricter editors. Only non-PNG sources are transcoded, and then the same splice runs.
/// </remarks>
internal static class CharacterCardWriter
{
    private const string CharaKeyword = "chara";

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// Returns a PNG card carrying <paramref name="characterJson"/> in its <c>chara</c> chunk,
    /// replacing any existing one and preserving every other chunk of an already-PNG source verbatim.
    /// </summary>
    public static byte[] EmbedCharacterJson(byte[] imageBytes, string characterJson)
    {
        var png = IsPng(imageBytes) ? imageBytes : ToPng(imageBytes);

        var chunks = Parse(png).Where(chunk => !IsCharaText(chunk)).ToList();
        var charaChunk = new PngChunk("tEXt", CharaChunkData(characterJson));

        var iend = chunks.FindIndex(chunk => chunk.Type == "IEND");
        chunks.Insert(iend < 0 ? chunks.Count : iend, charaChunk);

        return Serialize(chunks);
    }

    /// <summary>The decoded character JSON from a card's <c>chara</c> chunk, or None if absent.</summary>
    public static Option<string> ReadCharacterJson(byte[] pngBytes) =>
        Parse(pngBytes)
            .Where(IsCharaText)
            .Select(chunk => Decode(ChunkValue(chunk)))
            .FirstOrDefault() is { } json
            ? Option.Some(json)
            : Option<string>.None;

    private static byte[] ToPng(byte[] imageBytes)
    {
        using var image = Image.Load(imageBytes);
        using var output = new MemoryStream();
        image.SaveAsPng(output);
        return output.ToArray();
    }

    private static bool IsPng(byte[] bytes) =>
        bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(PngSignature);

    // tEXt chunk body: keyword (Latin-1) + NUL + value. Base64 keeps the value pure ASCII.
    private static byte[] CharaChunkData(string json)
    {
        var keyword = Encoding.ASCII.GetBytes(CharaKeyword);
        var value = Encoding.ASCII.GetBytes(Encode(json));

        var data = new byte[keyword.Length + 1 + value.Length];
        keyword.CopyTo(data, 0);
        data[keyword.Length] = 0;
        value.CopyTo(data, keyword.Length + 1);
        return data;
    }

    private static bool IsCharaText(PngChunk chunk)
    {
        if (chunk.Type is not ("tEXt" or "zTXt" or "iTXt"))
        {
            return false;
        }

        var separator = Array.IndexOf(chunk.Data, (byte)0);
        return separator > 0 && Encoding.ASCII.GetString(chunk.Data, 0, separator) == CharaKeyword;
    }

    private static string ChunkValue(PngChunk chunk)
    {
        var separator = Array.IndexOf(chunk.Data, (byte)0);
        return Encoding.ASCII.GetString(chunk.Data, separator + 1, chunk.Data.Length - separator - 1);
    }

    private static List<PngChunk> Parse(byte[] png)
    {
        var chunks = new List<PngChunk>();
        var offset = PngSignature.Length;
        while (offset + 8 <= png.Length)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset, 4));
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            var data = png[(offset + 8)..(offset + 8 + length)];
            chunks.Add(new PngChunk(type, data));

            offset += 12 + length; // length + type + data + CRC
            if (type == "IEND")
            {
                break;
            }
        }

        return chunks;
    }

    private static byte[] Serialize(IEnumerable<PngChunk> chunks)
    {
        using var output = new MemoryStream();
        output.Write(PngSignature);

        Span<byte> word = stackalloc byte[4];
        foreach (var chunk in chunks)
        {
            var type = Encoding.ASCII.GetBytes(chunk.Type);

            BinaryPrimitives.WriteInt32BigEndian(word, chunk.Data.Length);
            output.Write(word);
            output.Write(type);
            output.Write(chunk.Data);

            BinaryPrimitives.WriteUInt32BigEndian(word, Crc32(type, chunk.Data));
            output.Write(word);
        }

        return output.ToArray();
    }

    // PNG chunk CRC-32 (polynomial 0xEDB88320) over the chunk type followed by its data.
    private static uint Crc32(byte[] type, byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        crc = Accumulate(crc, type);
        crc = Accumulate(crc, data);
        return crc ^ 0xFFFFFFFFu;
    }

    private static uint Accumulate(uint crc, byte[] bytes)
    {
        foreach (var b in bytes)
        {
            crc ^= b;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc >> 1) ^ (0xEDB88320u & (uint)-(int)(crc & 1));
            }
        }

        return crc;
    }

    private static string Encode(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

    private static string Decode(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value));

    private sealed record PngChunk(string Type, byte[] Data);
}

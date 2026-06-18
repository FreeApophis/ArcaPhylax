using Avalonia.Media.Imaging;
using CharacterArchivePlugin.Configuration;
using Funcky.Monads;

namespace CharacterArchivePlugin.Services;

/// <summary>
/// Maps a character image hash to a file in the sharded <c>hashed-data</c> tree. The shard prefix
/// supplies the leading md5 nibbles, so a 32-char md5 <c>H</c> lives at <c>root/H0/H1/H2/H[3..]</c>.
/// </summary>
public sealed class HashedImageStore(CharacterArchiveSettings settings) : IImageStore
{
    public Option<string> PathForHash(string hash) =>
        RelativePathForHash(hash)
            .Select(relative => Path.Combine(settings.ImageRoot, relative))
            .Where(File.Exists);

    public Task<Bitmap?> LoadThumbnailAsync(string hash, int decodeWidth, CancellationToken cancellationToken = default) =>
        DecodeAsync(hash, stream => Bitmap.DecodeToWidth(stream, decodeWidth), cancellationToken);

    public Task<Bitmap?> LoadFullAsync(string hash, CancellationToken cancellationToken = default) =>
        DecodeAsync(hash, stream => new Bitmap(stream), cancellationToken);

    public Task<byte[]?> ReadBytesAsync(string hash, CancellationToken cancellationToken = default) =>
        PathForHash(hash).Match(
            none: () => Task.FromResult<byte[]?>(null),
            some: path => ReadAllBytesOrNull(path, cancellationToken));

    private static async Task<byte[]?> ReadAllBytesOrNull(string path, CancellationToken cancellationToken)
    {
        try
        {
            return await File.ReadAllBytesAsync(path, cancellationToken);
        }
        catch
        {
            // Unreadable file (e.g. removed between the existence check and the read).
            return null;
        }
    }

    private Task<Bitmap?> DecodeAsync(string hash, Func<Stream, Bitmap> decode, CancellationToken cancellationToken) =>
        PathForHash(hash).Match(
            none: () => Task.FromResult<Bitmap?>(null),
            some: path => Task.Run<Bitmap?>(() =>
            {
                try
                {
                    using var stream = File.OpenRead(path);
                    return decode(stream);
                }
                catch
                {
                    // Unreadable/undecodable image (e.g. unsupported format) shows as a blank tile.
                    return null;
                }
            }, cancellationToken));

    /// <summary>
    /// Pure shard derivation: a 32-char md5 hex maps to the relative path <c>H0/H1/H2/H[3..]</c>.
    /// Returns None for anything that is not exactly 32 hex characters.
    /// </summary>
    internal static Option<string> RelativePathForHash(string? hash)
    {
        if (hash is null)
        {
            return Option<string>.None;
        }

        var normalized = hash.Trim().ToLowerInvariant();
        return IsMd5Hex(normalized)
            ? Option.Some(Path.Combine(normalized[0..1], normalized[1..2], normalized[2..3], normalized[3..]))
            : Option<string>.None;
    }

    private static bool IsMd5Hex(string value) =>
        value.Length == 32 && value.All(Uri.IsHexDigit);
}

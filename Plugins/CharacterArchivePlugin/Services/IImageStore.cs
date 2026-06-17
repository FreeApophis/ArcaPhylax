using Avalonia.Media.Imaging;
using Funcky.Monads;

namespace CharacterArchivePlugin.Services;

/// <summary>
/// Resolves character image hashes to files in the sharded <c>hashed-data</c> tree and decodes them.
/// </summary>
public interface IImageStore
{
    /// <summary>Absolute path of the image for <paramref name="hash"/>, if the file exists.</summary>
    Option<string> PathForHash(string hash);

    /// <summary>Decodes the image at a reduced width for grid thumbnails. Null if missing or undecodable.</summary>
    Task<Bitmap?> LoadThumbnailAsync(string hash, int decodeWidth, CancellationToken cancellationToken = default);

    /// <summary>Decodes the full-resolution image. Null if missing or undecodable.</summary>
    Task<Bitmap?> LoadFullAsync(string hash, CancellationToken cancellationToken = default);
}

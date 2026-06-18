using Avalonia.Media.Imaging;
using CharacterArchivePlugin.Services;
using Funcky.Monads;

namespace CharacterArchivePlugin.Tests.Fakes;

/// <summary>Inert <see cref="IImageStore"/>: no images, so thumbnails resolve to null in tests.</summary>
internal sealed class FakeImageStore : IImageStore
{
    public Option<string> PathForHash(string hash) => Option<string>.None;

    public Task<Bitmap?> LoadThumbnailAsync(string hash, int decodeWidth, CancellationToken cancellationToken = default) =>
        Task.FromResult<Bitmap?>(null);

    public Task<Bitmap?> LoadFullAsync(string hash, CancellationToken cancellationToken = default) =>
        Task.FromResult<Bitmap?>(null);

    public Task<byte[]?> ReadBytesAsync(string hash, CancellationToken cancellationToken = default) =>
        Task.FromResult<byte[]?>(null);
}

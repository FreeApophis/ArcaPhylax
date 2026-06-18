using Avalonia.Media.Imaging;
using CharacterArchivePlugin.Data;
using CharacterArchivePlugin.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CharacterArchivePlugin.ViewModels;

/// <summary>
/// One result-grid item: name plus a thumbnail decoded lazily off the UI thread.
/// </summary>
public sealed partial class CharacterTileVm(CharacterTile tile, IImageStore images) : ObservableObject
{
    private readonly IImageStore _images = images;

    [ObservableProperty]
    public partial Bitmap? Thumbnail { get; set; }

    public string Key { get; } = tile.Key;

    public string Name { get; } = tile.Name;

    public string Hash { get; } = tile.ImageHash;

    public async Task LoadThumbnailAsync(int width, CancellationToken cancellationToken) =>
        Thumbnail = await _images.LoadThumbnailAsync(Hash, width, cancellationToken);
}

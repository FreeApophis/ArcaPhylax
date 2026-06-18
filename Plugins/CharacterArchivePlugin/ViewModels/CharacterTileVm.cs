using Avalonia.Media.Imaging;
using CharacterArchivePlugin.Data;
using CharacterArchivePlugin.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CharacterArchivePlugin.ViewModels;

/// <summary>
/// One result-grid item: name plus a thumbnail decoded lazily off the UI thread.
/// </summary>
public sealed partial class CharacterTileVm : ObservableObject
{
    private readonly IImageStore _images;

    [ObservableProperty]
    public partial Bitmap? Thumbnail { get; set; }

    public CharacterTileVm(CharacterTile tile, IImageStore images)
    {
        _images = images;
        Key = tile.Key;
        Name = tile.Name;
        Hash = tile.ImageHash;
    }

    public string Key { get; }
    public string Name { get; }
    public string Hash { get; }

    public async Task LoadThumbnailAsync(int width, CancellationToken cancellationToken) =>
        Thumbnail = await _images.LoadThumbnailAsync(Hash, width, cancellationToken);
}

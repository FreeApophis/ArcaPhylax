using Avalonia.Media.Imaging;
using CharacterArchivePlugin.Data;
using CharacterArchivePlugin.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CharacterArchivePlugin.ViewModels;

/// <summary>
/// One result-grid item: the representative of a stack of characters sharing an image, plus a
/// thumbnail decoded lazily off the UI thread. <see cref="IsStack"/> drives the count badge.
/// </summary>
public sealed partial class CharacterTileVm(CharacterStack stack, IImageStore images) : ObservableObject
{
    private readonly IImageStore _images = images;

    [ObservableProperty]
    public partial Bitmap? Thumbnail { get; set; }

    public string Key { get; } = stack.Key;

    public string Name { get; } = stack.Name;

    public string Hash { get; } = stack.ImageHash;

    /// <summary>How many characters share this image; 1 for a lone character.</summary>
    public int VariantCount { get; } = stack.VariantCount;

    /// <summary>True when this tile collapses more than one character (shows the badge).</summary>
    public bool IsStack => VariantCount > 1;

    /// <summary>Count badge for a stack, e.g. <c>×5</c>.</summary>
    public string CountLabel => $"×{VariantCount}";

    public async Task LoadThumbnailAsync(int width, CancellationToken cancellationToken) =>
        Thumbnail = await _images.LoadThumbnailAsync(Hash, width, cancellationToken);
}

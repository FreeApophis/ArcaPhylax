using CommunityToolkit.Mvvm.ComponentModel;
using NAPS2.Images;
using NAPS2.Scan;

namespace ScannerPlugin.Models;

/// <summary>Page sizes offered by a scanner profile. Mapped to NAPS2 page sizes on the scan page.</summary>
public enum ScanPageSize
{
    A4,
    A5,
    A3,
    Letter,
    Legal,
}

/// <summary>
/// A named bundle of scan settings. This is an editable, two-way-bound model rather than a record
/// (it needs INPC so the configuration UI can edit it in place), hence <see cref="ObservableObject"/>.
/// </summary>
public sealed partial class ScannerProfile : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = "New profile";

    [ObservableProperty]
    public partial int Dpi { get; set; } = 300;

    [ObservableProperty]
    public partial ScanPageSize PageSize { get; set; } = ScanPageSize.A4;

    [ObservableProperty]
    public partial PaperSource PaperSource { get; set; } = PaperSource.Flatbed;

    [ObservableProperty]
    public partial BitDepth BitDepth { get; set; } = BitDepth.Color;

    public override string ToString() => Name;
}

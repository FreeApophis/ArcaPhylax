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
    private string _name = "New profile";

    [ObservableProperty]
    private int _dpi = 300;

    [ObservableProperty]
    private ScanPageSize _pageSize = ScanPageSize.A4;

    [ObservableProperty]
    private PaperSource _paperSource = PaperSource.Flatbed;

    [ObservableProperty]
    private BitDepth _bitDepth = BitDepth.Color;

    public override string ToString() => Name;
}

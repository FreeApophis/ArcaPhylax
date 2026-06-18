using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using NAPS2.Images;
using NAPS2.Scan;
using ScannerPlugin.Models;

namespace ScannerPlugin.Services;

/// <summary>
/// Session-wide scanner state shared between the configuration section and (later) the scan page.
/// Registered as a single instance so the selected device and profile are visible everywhere.
/// Session-only for now; persistence is a later step.
/// </summary>
public sealed partial class ScannerSettings : ObservableObject
{
    public ObservableCollection<ScannerProfile> Profiles { get; } = new();

    [ObservableProperty]
    public partial ScanDevice? SelectedDevice { get; set; }

    [ObservableProperty]
    public partial ScannerProfile? SelectedProfile { get; set; }

    public ScannerSettings()
    {
        Profiles.Add(new ScannerProfile
        {
            Name = "Document (b/w 300dpi)",
            Dpi = 300,
            PageSize = ScanPageSize.A4,
            PaperSource = PaperSource.Feeder,
            BitDepth = BitDepth.BlackAndWhite,
        });
        Profiles.Add(new ScannerProfile
        {
            Name = "Photo (color 600dpi)",
            Dpi = 600,
            PageSize = ScanPageSize.A4,
            PaperSource = PaperSource.Flatbed,
            BitDepth = BitDepth.Color,
        });

        SelectedProfile = Profiles.FirstOrDefault();
    }
}

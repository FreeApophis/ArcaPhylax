namespace DemiurgPlaygroundPlugin.ViewModels;

/// <summary>
/// Filled SVG-path geometry for the Demiurg Playground nav entry. Lives inside the plugin assembly
/// so the plugin owns its own icon rather than depending on the host.
/// </summary>
public static class DemiurgPlaygroundIcons
{
    /// <summary>
    /// An AI brain: a lobed brain silhouette with a central seam and circuit nodes punched out as
    /// holes (even-odd fill, <c>F0</c>), suggesting a brain wired with conductor tracks.
    /// </summary>
    // Path segments: brain outline, then the central seam and circuit-node holes (even-odd cut-outs).
    public const string Brain =
        "F0 " +
        "M12 3.5 " +
        "C 9.5 3.5 7.6 4.9 7 6.9 C 5.2 6.6 3.6 8 3.6 9.9 " +
        "C 2.6 10.6 2.2 11.9 2.7 13.1 C 2.3 14.4 3 15.8 4.2 16.3 " +
        "C 4.6 18.2 6.4 19.5 8.3 19.2 C 9.1 20.4 10.8 20.9 12 20.2 " +
        "C 13.2 20.9 14.9 20.4 15.7 19.2 C 17.6 19.5 19.4 18.2 19.8 16.3 " +
        "C 21 15.8 21.7 14.4 21.3 13.1 C 21.8 11.9 21.4 10.6 20.4 9.9 " +
        "C 20.4 8 18.8 6.6 17 6.9 C 16.4 4.9 14.5 3.5 12 3.5 Z " +
        "M11.7 6.4 C 12.7 8.4 11 10.5 12 12.5 C 13 14.5 11.3 16 12.1 18 " +
        "L 12.5 17.9 C 11.7 16 13.4 14.5 12.4 12.5 C 11.4 10.5 13.1 8.4 12.1 6.4 Z " +
        "M8 9 A1 1 0 1 0 8 11 A1 1 0 1 0 8 9 Z " +
        "M16 9 A1 1 0 1 0 16 11 A1 1 0 1 0 16 9 Z " +
        "M8.6 14.4 A1 1 0 1 0 8.6 16.4 A1 1 0 1 0 8.6 14.4 Z " +
        "M15.4 14.4 A1 1 0 1 0 15.4 16.4 A1 1 0 1 0 15.4 14.4 Z";
}

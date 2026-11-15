namespace ArcaPhylax.ViewModels;

/// <summary>
/// Filled SVG-path geometries for the navigation rail and the kebab toggle. Kept here so the
/// nav items are pure code and the XAML stays free of long path strings. (Material-style icons.)
/// Detail is cut from the body via overlapping sub-paths under the even-odd fill rule, as in
/// <see cref="About"/> and <see cref="Configure"/>.
/// </summary>
public static class NavigationIcons
{
    /// <summary>
    /// Two rectangles side by side — the dashboard. The left one is slightly narrower to create.
    /// </summary>
    public const string Dashboard =
        "M3,5 H10 V19 H3 Z M12,5 H21 V19 H12 Z";

    /// <summary>Dog-eared page with text lines — documents.</summary>
    public const string Documents =
        "M6,2 H14 L20,8 V22 H6 Z M14,2 V8 H20 Z " +
        "M8,12 H16 V13.5 H8 Z M8,15 H16 V16.5 H8 Z M8,18 H13 V19.5 H8 Z";

    /// <summary>Framed picture with a sun and a mountain — images.</summary>
    public const string Images =
        "M3,5 H21 V19 H3 Z M5,7 H19 V17 H5 Z " +
        "M8.5,8.5 A1.5,1.5 0 1 0 8.5,11.5 A1.5,1.5 0 1 0 8.5,8.5 Z " +
        "M6,17 L11,11 L13.5,13.5 L17,9 L19,17 Z";

    /// <summary>Rounded frame with a play triangle — videos.</summary>
    public const string Videos =
        "M4,5 H20 A1,1 0 0 1 21,6 V18 A1,1 0 0 1 20,19 H4 A1,1 0 0 1 3,18 V6 A1,1 0 0 1 4,5 Z " +
        "M10,9 V15 L15,12 Z";

    /// <summary>Speech bubble with a tail and three dots — chat.</summary>
    public const string Chat =
        "M4,3 H20 A1,1 0 0 1 21,4 V16 A1,1 0 0 1 20,17 H9 L5,21 V17 H4 A1,1 0 0 1 3,16 V4 A1,1 0 0 1 4,3 Z " +
        "M7,9 H9.4 V11 H7 Z M10.8,9 H13.2 V11 H10.8 Z M14.6,9 H17 V11 H14.6 Z";

    /// <summary>Price tag with an eyelet — tags.</summary>
    public const string Tags =
        "M21.4,11.6 L12.4,2.6 A2,2 0 0 0 11,2 H4 A2,2 0 0 0 2,4 V11 A2,2 0 0 0 2.6,12.4 " +
        "L11.6,21.4 A2,2 0 0 0 14.4,21.4 L21.4,14.4 A2,2 0 0 0 21.4,11.6 Z " +
        "M5.5,4 A1.5,1.5 0 1 0 5.5,7 A1.5,1.5 0 1 0 5.5,4 Z";

    /// <summary>Cog — configuration.</summary>
    public const string Configure =
        "M19.4,12.9 C19.4,12.6 19.5,12.3 19.5,12 C19.5,11.7 19.4,11.4 19.4,11.1 L21.5,9.5 " +
        "C21.6,9.4 21.7,9.1 21.6,9 L19.6,5.5 C19.5,5.3 19.3,5.3 19.1,5.3 L16.6,6.3 " +
        "C16.1,5.9 15.5,5.6 14.9,5.3 L14.5,2.7 C14.5,2.5 14.3,2.4 14.1,2.4 L10.1,2.4 " +
        "C9.9,2.4 9.7,2.5 9.7,2.7 L9.3,5.3 C8.7,5.6 8.1,5.9 7.6,6.3 L5.1,5.3 " +
        "C4.9,5.3 4.7,5.3 4.6,5.5 L2.6,9 C2.5,9.1 2.6,9.4 2.7,9.5 L4.8,11.1 " +
        "C4.8,11.4 4.7,11.7 4.7,12 C4.7,12.3 4.8,12.6 4.8,12.9 L2.7,14.5 " +
        "C2.6,14.6 2.5,14.9 2.6,15 L4.6,18.5 C4.7,18.7 4.9,18.7 5.1,18.7 L7.6,17.7 " +
        "C8.1,18.1 8.7,18.4 9.3,18.7 L9.7,21.3 C9.7,21.5 9.9,21.6 10.1,21.6 L14.1,21.6 " +
        "C14.3,21.6 14.5,21.5 14.5,21.3 L14.9,18.7 C15.5,18.4 16.1,18.1 16.6,17.7 L19.1,18.7 " +
        "C19.3,18.7 19.5,18.7 19.6,18.5 L21.6,15 C21.7,14.9 21.6,14.6 21.5,14.5 Z " +
        "M12,15.5 A3.5,3.5 0 1 1 12,8.5 A3.5,3.5 0 0 1 12,15.5 Z";

    /// <summary>Puzzle piece — plugins.</summary>
    public const string Plugins =
        "M20.5,11 H19 V7 A2,2 0 0 0 17,5 H13 V3.5 A2.5,2.5 0 0 0 8,3.5 V5 H4 A2,2 0 0 0 2,7 V10.8 H3.5 " +
        "A2.7,2.7 0 0 1 3.5,16.2 H2 V20 A2,2 0 0 0 4,22 H7.8 V20.5 A2.7,2.7 0 0 1 13.2,20.5 V22 H17 " +
        "A2,2 0 0 0 19,20 V16 H20.5 A2.5,2.5 0 0 0 20.5,11 Z";

    /// <summary>Info circle — the about page.</summary>
    public const string About =
        "M12,2 A10,10 0 1 0 12,22 A10,10 0 0 0 12,2 Z M11,7 H13 V9 H11 Z M11,11 H13 V17 H11 Z";
}

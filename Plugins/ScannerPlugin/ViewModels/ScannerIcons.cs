namespace ArcaPhylax.ViewModels;

/// <summary>
/// Filled SVG-path geometries for the ScannerPlugin's nav entry. Lives inside the plugin
/// assembly so the plugin owns its own icons rather than depending on the host.
/// </summary>
public static class ScannerIcons
{
    /// <summary>Corner brackets framing a horizontal scan beam — a document scanner.</summary>
    public const string Scan =
        "M3,3 H9 V5 H5 V9 H3 Z " +       // top-left bracket
        "M21,3 V9 H19 V5 H15 V3 Z " +    // top-right bracket
        "M3,21 V15 H5 V19 H9 V21 Z " +   // bottom-left bracket
        "M21,21 H15 V19 H19 V15 H21 Z " +// bottom-right bracket
        "M5,11.4 H19 V12.6 H5 Z";        // scan beam
}

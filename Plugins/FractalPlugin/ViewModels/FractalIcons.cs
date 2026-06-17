namespace FractalPlugin.ViewModels;

/// <summary>
/// Filled SVG-path geometries for the FractalPlugin's nav entry. Lives inside the plugin
/// assembly so the plugin owns its own icons rather than depending on the host. Detail is cut
/// via overlapping sub-paths under the even-odd fill rule.
/// </summary>
public static class FractalIcons
{
    /// <summary>A two-level Sierpiński triangle — the self-similar fractal mark.</summary>
    public const string Fractal =
        "M12,2 L22,20 H2 Z " +          // outer triangle
        "M7,11 H17 L12,20 Z " +         // level-1 centre (cut)
        "M9.5,6.5 H14.5 L12,11 Z " +    // top sub-triangle (cut)
        "M4.5,15.5 H9.5 L7,20 Z " +     // bottom-left sub-triangle (cut)
        "M14.5,15.5 H19.5 L17,20 Z";    // bottom-right sub-triangle (cut)
}

namespace NutritionPlugin.ViewModels;

/// <summary>
/// Filled SVG-path geometry for the Nutrition nav entry. Lives inside the plugin assembly so the
/// plugin owns its own icon rather than depending on the host.
/// </summary>
public static class NutritionIcons
{
    /// <summary>A stylised apple with a leaf — the calorie/nutrition tab glyph.</summary>
    public const string Apple =
        "M12,6 C10.5,3.5 7.5,3 6,4.5 C3.5,7 4,12 6.5,16 C8,18.5 9.5,20 12,20 " +
        "C14.5,20 16,18.5 17.5,16 C20,12 20.5,7 18,4.5 C16.5,3 13.5,3.5 12,6 Z " +
        "M12.5,5.5 C13,3.5 15,2.5 16.5,3 C16,5 14.5,6 12.5,5.5 Z";
}

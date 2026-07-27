namespace NutritionPlugin.Data;

/// <summary>Total calories a person consumed on a single day — one point of the calorie graph.</summary>
public sealed record DailyTotal(DateOnly Date, int Calories);

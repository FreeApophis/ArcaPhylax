namespace NutritionPlugin.Data;

/// <summary>A tracked individual. Multiple people can be logged independently in the same database.</summary>
public sealed record Person(long Id, string Name);

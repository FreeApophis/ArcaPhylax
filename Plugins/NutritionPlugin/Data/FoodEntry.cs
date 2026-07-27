namespace NutritionPlugin.Data;

/// <summary>
/// A single logged intake for a person on a given day. A day can hold many entries; their calories
/// sum into that day's total (see <see cref="Domain.NutritionMath.DailyCalorieTotals"/>).
/// Property order matches the <c>SELECT</c> column order the repository uses for positional binding.
/// </summary>
public sealed record FoodEntry(long Id, long PersonId, DateOnly LoggedOn, string Description, int Calories);

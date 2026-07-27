using NutritionPlugin.Data;

namespace NutritionPlugin.Domain;

/// <summary>
/// Pure aggregation over logged entries. No I/O, no clock — every function is a total mapping from
/// its inputs to a new sequence, so it is exhaustively unit-testable.
/// </summary>
public static class NutritionMath
{
    /// <summary>
    /// Collapses food entries into one <see cref="DailyTotal"/> per day, summing calories, ordered
    /// chronologically. An empty input yields an empty sequence.
    /// </summary>
    public static IEnumerable<DailyTotal> DailyCalorieTotals(IEnumerable<FoodEntry> entries) =>
        entries
            .GroupBy(entry => entry.LoggedOn)
            .Select(day => new DailyTotal(day.Key, day.Sum(entry => entry.Calories)))
            .OrderBy(total => total.Date);
}

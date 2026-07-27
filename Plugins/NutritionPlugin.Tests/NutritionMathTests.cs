using NutritionPlugin.Data;
using NutritionPlugin.Domain;
using Xunit;

namespace NutritionPlugin.Tests;

public sealed class NutritionMathTests
{
    [Fact]
    public void DailyCalorieTotals_WhenMultipleEntriesShareADay_SumsTheirCalories()
    {
        // Arrange
        var day = new DateOnly(2026, 7, 26);
        var entries = new[]
        {
            new FoodEntry(1, 1, day, "Porridge", 350),
            new FoodEntry(2, 1, day, "Apple", 95),
            new FoodEntry(3, 1, day, "Coffee", 5),
        };

        // Act
        var totals = NutritionMath.DailyCalorieTotals(entries).ToList();

        // Assert
        Assert.Equal(new DailyTotal(day, 450), Assert.Single(totals));
    }

    [Fact]
    public void DailyCalorieTotals_WhenNoEntries_ReturnsEmptySequence()
    {
        // Arrange
        var entries = Array.Empty<FoodEntry>();

        // Act
        var totals = NutritionMath.DailyCalorieTotals(entries);

        // Assert
        Assert.Empty(totals);
    }

    [Fact]
    public void DailyCalorieTotals_WhenDaysAreUnordered_ReturnsThemChronologically()
    {
        // Arrange
        var entries = new[]
        {
            new FoodEntry(1, 1, new DateOnly(2026, 7, 26), "Lunch", 600),
            new FoodEntry(2, 1, new DateOnly(2026, 7, 24), "Lunch", 600),
            new FoodEntry(3, 1, new DateOnly(2026, 7, 25), "Lunch", 600),
        };

        // Act
        var dates = NutritionMath.DailyCalorieTotals(entries).Select(total => total.Date).ToList();

        // Assert
        Assert.Equal(
            [new DateOnly(2026, 7, 24), new DateOnly(2026, 7, 25), new DateOnly(2026, 7, 26)],
            dates);
    }
}

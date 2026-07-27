namespace NutritionPlugin.Controls;

/// <summary>
/// One plottable point in chart space: <paramref name="X"/> is typically a day number
/// (<see cref="System.DateOnly.DayNumber"/>) and <paramref name="Y"/> the measured value. The chart
/// normalises both axes to its own pixel bounds, so the units are arbitrary.
/// </summary>
public readonly record struct ChartPoint(double X, double Y);

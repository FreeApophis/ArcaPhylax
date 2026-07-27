namespace NutritionPlugin.Data;

/// <summary>
/// A person's measured weight on a given day. At most one measurement is kept per person per day
/// (a same-day re-entry overwrites the previous value). Property order matches the repository's
/// <c>SELECT</c> column order for positional binding.
/// </summary>
public sealed record WeightEntry(long Id, long PersonId, DateOnly MeasuredOn, double Kilograms);

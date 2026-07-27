namespace NutritionPlugin.Data;

/// <summary>
/// Persistence for people and their logged calorie/weight entries. All reads are scoped to a single
/// person; writes return the persisted row (with its assigned <c>Id</c>).
/// </summary>
public interface INutritionRepository
{
    /// <summary>Every tracked person, ordered by name.</summary>
    IReadOnlyList<Person> People();

    /// <summary>Adds a person and returns them with their new id.</summary>
    Person AddPerson(string name);

    /// <summary>Every food entry for a person, most recent day first.</summary>
    IReadOnlyList<FoodEntry> FoodEntries(long personId);

    /// <summary>Logs an intake and returns the persisted entry.</summary>
    FoodEntry AddFoodEntry(long personId, DateOnly loggedOn, string description, int calories);

    /// <summary>Every weight measurement for a person, oldest first (chart order).</summary>
    IReadOnlyList<WeightEntry> WeightEntries(long personId);

    /// <summary>
    /// Records a weight for a day, replacing any existing measurement for that person and day, and
    /// returns the persisted entry.
    /// </summary>
    WeightEntry AddWeightEntry(long personId, DateOnly measuredOn, double kilograms);
}

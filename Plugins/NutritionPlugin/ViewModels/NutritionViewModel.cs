using System.Collections.ObjectModel;
using ArcaPhylax.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutritionPlugin.Controls;
using NutritionPlugin.Data;
using NutritionPlugin.Domain;

namespace NutritionPlugin.ViewModels;

/// <summary>
/// The Nutrition tab: pick (or add) a person, log calorie intake and weight for a day, and see both
/// plotted over time. Reads and writes go through <see cref="INutritionRepository"/>; the two graph
/// series are derived from the loaded entries via the pure <see cref="NutritionMath"/> helpers.
/// </summary>
// Derives from ObservableObject directly (not the shared ViewModelBase) so the source-generated
// [ObservableProperty]/[RelayCommand] members bind to the plugin's own CommunityToolkit type — see
// the cross-AssemblyLoadContext note in CLAUDE.md.
public sealed partial class NutritionViewModel : ObservableObject, ITabContentViewModel
{
    private readonly INutritionRepository _repository;

    public NutritionViewModel(INutritionRepository repository)
    {
        _repository = repository;

        foreach (var person in repository.People())
        {
            People.Add(person);
        }

        NewFoodDate = DateTimeOffset.Now;
        NewWeightDate = DateTimeOffset.Now;
        SelectedPerson = People.FirstOrDefault();
    }

    public string Title => "Nutrition";

    public string Icon => NutritionIcons.Apple;

    public int NavigationWeight => 40;

    public ObservableCollection<Person> People { get; } = [];

    public ObservableCollection<FoodEntry> FoodEntries { get; } = [];

    public ObservableCollection<WeightEntry> WeightEntries { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddFoodCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddWeightCommand))]
    public partial Person? SelectedPerson { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<ChartPoint> CalorieSeries { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<ChartPoint> WeightSeries { get; set; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPersonCommand))]
    public partial string NewPersonName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddFoodCommand))]
    public partial string NewFoodDescription { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddFoodCommand))]
    public partial decimal? NewFoodCalories { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? NewFoodDate { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddWeightCommand))]
    public partial decimal? NewWeightKilograms { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? NewWeightDate { get; set; }

    private bool CanAddPerson => !string.IsNullOrWhiteSpace(NewPersonName);

    private bool CanAddFood =>
        SelectedPerson is not null && !string.IsNullOrWhiteSpace(NewFoodDescription) && NewFoodCalories is > 0;

    private bool CanAddWeight => SelectedPerson is not null && NewWeightKilograms is > 0;

    [RelayCommand(CanExecute = nameof(CanAddPerson))]
    private void AddPerson()
    {
        var person = _repository.AddPerson(NewPersonName.Trim());

        People.Add(person);
        NewPersonName = string.Empty;
        SelectedPerson = person;
    }

    [RelayCommand(CanExecute = nameof(CanAddFood))]
    private void AddFood()
    {
        if (SelectedPerson is not { } person || NewFoodCalories is not { } calories)
        {
            return;
        }

        _repository.AddFoodEntry(person.Id, DateOf(NewFoodDate), NewFoodDescription.Trim(), (int)calories);

        NewFoodDescription = string.Empty;
        NewFoodCalories = null;
        Reload(person);
    }

    [RelayCommand(CanExecute = nameof(CanAddWeight))]
    private void AddWeight()
    {
        if (SelectedPerson is not { } person || NewWeightKilograms is not { } kilograms)
        {
            return;
        }

        _repository.AddWeightEntry(person.Id, DateOf(NewWeightDate), (double)kilograms);

        Reload(person);
    }

    partial void OnSelectedPersonChanged(Person? value)
    {
        if (value is { } person)
        {
            Reload(person);
        }
        else
        {
            Clear();
        }
    }

    private void Reload(Person person)
    {
        var food = _repository.FoodEntries(person.Id);
        var weight = _repository.WeightEntries(person.Id);

        Replace(FoodEntries, food);
        Replace(WeightEntries, weight);

        CalorieSeries = NutritionMath.DailyCalorieTotals(food)
            .Select(total => new ChartPoint(total.Date.DayNumber, total.Calories))
            .ToList();
        WeightSeries = weight
            .Select(entry => new ChartPoint(entry.MeasuredOn.DayNumber, entry.Kilograms))
            .ToList();
    }

    private void Clear()
    {
        FoodEntries.Clear();
        WeightEntries.Clear();
        CalorieSeries = [];
        WeightSeries = [];
    }

    private static void Replace<T>(ObservableCollection<T> target, IReadOnlyList<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    // The picker never clears to null in the UI, but guard it anyway; "today" is the sensible default.
    private static DateOnly DateOf(DateTimeOffset? picked) =>
        DateOnly.FromDateTime((picked ?? DateTimeOffset.Now).LocalDateTime);
}

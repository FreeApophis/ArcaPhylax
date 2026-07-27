using NutritionPlugin.Data;
using NutritionPlugin.Tests.Fakes;
using Xunit;

namespace NutritionPlugin.Tests;

public sealed class SqliteNutritionRepositoryTests : IDisposable
{
    private readonly InMemoryNutritionConnectionFactory _factory = new();
    private readonly SqliteNutritionRepository _sut;

    public SqliteNutritionRepositoryTests()
    {
        _sut = new SqliteNutritionRepository(_factory);
    }

    [Fact]
    public void AddPerson_GivenName_ReturnsPersonWithAssignedId()
    {
        // Arrange

        // Act
        var person = _sut.AddPerson("Ada");

        // Assert
        Assert.True(person.Id > 0);
        Assert.Equal("Ada", person.Name);
    }

    [Fact]
    public void People_AfterAddingTwo_ReturnsBothOrderedByName()
    {
        // Arrange
        _sut.AddPerson("Grace");
        _sut.AddPerson("Ada");

        // Act
        var names = _sut.People().Select(person => person.Name).ToList();

        // Assert
        Assert.Equal(["Ada", "Grace"], names);
    }

    [Fact]
    public void AddFoodEntry_WhenLogged_IsReadableForThatPerson()
    {
        // Arrange
        var person = _sut.AddPerson("Ada");
        var day = new DateOnly(2026, 7, 26);

        // Act
        _sut.AddFoodEntry(person.Id, day, "Porridge", 350);

        // Assert
        var entry = Assert.Single(_sut.FoodEntries(person.Id));
        Assert.Equal(new FoodEntry(entry.Id, person.Id, day, "Porridge", 350), entry);
    }

    [Fact]
    public void FoodEntries_WhenTwoPeopleLog_AreScopedToTheRequestedPerson()
    {
        // Arrange
        var ada = _sut.AddPerson("Ada");
        var grace = _sut.AddPerson("Grace");
        _sut.AddFoodEntry(ada.Id, new DateOnly(2026, 7, 26), "Apple", 95);
        _sut.AddFoodEntry(grace.Id, new DateOnly(2026, 7, 26), "Banana", 105);

        // Act
        var adaEntries = _sut.FoodEntries(ada.Id);

        // Assert
        Assert.Equal("Apple", Assert.Single(adaEntries).Description);
    }

    [Fact]
    public void AddWeightEntry_WhenSameDayLoggedTwice_ReplacesTheValue()
    {
        // Arrange
        var person = _sut.AddPerson("Ada");
        var day = new DateOnly(2026, 7, 26);
        _sut.AddWeightEntry(person.Id, day, 70.0);

        // Act
        _sut.AddWeightEntry(person.Id, day, 69.4);

        // Assert
        var entry = Assert.Single(_sut.WeightEntries(person.Id));
        Assert.Equal(69.4, entry.Kilograms);
    }

    public void Dispose() => _factory.Dispose();
}

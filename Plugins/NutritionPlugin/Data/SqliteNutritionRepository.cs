using System.Collections.Immutable;
using System.Globalization;
using Dapper;

namespace NutritionPlugin.Data;

/// <summary>
/// Dapper-over-SQLite implementation. The schema is created on first construction — the repository
/// is the I/O boundary, so this one-time side effect belongs here rather than in a pure domain type.
/// </summary>
/// <remarks>
/// Dates are stored and read as ISO <c>yyyy-MM-dd</c> text (which sorts chronologically). Dapper's
/// record-constructor materialisation can't bind that text straight onto a <see cref="DateOnly"/>
/// parameter, so reads land in row DTOs whose types mirror the columns exactly and are then mapped to
/// the domain records; writes pass the pre-formatted string.
/// </remarks>
public sealed class SqliteNutritionRepository : INutritionRepository
{
    private const string IsoDate = "yyyy-MM-dd";

    private const string Schema = """
        CREATE TABLE IF NOT EXISTS Person (
            Id   INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT    NOT NULL
        );

        CREATE TABLE IF NOT EXISTS FoodEntry (
            Id          INTEGER PRIMARY KEY AUTOINCREMENT,
            PersonId    INTEGER NOT NULL REFERENCES Person(Id) ON DELETE CASCADE,
            LoggedOn    TEXT    NOT NULL,
            Description TEXT    NOT NULL,
            Calories    INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS WeightEntry (
            Id         INTEGER PRIMARY KEY AUTOINCREMENT,
            PersonId   INTEGER NOT NULL REFERENCES Person(Id) ON DELETE CASCADE,
            MeasuredOn TEXT    NOT NULL,
            Kilograms  REAL    NOT NULL,
            UNIQUE (PersonId, MeasuredOn)
        );

        CREATE INDEX IF NOT EXISTS IX_FoodEntry_Person   ON FoodEntry   (PersonId);
        CREATE INDEX IF NOT EXISTS IX_WeightEntry_Person ON WeightEntry (PersonId);
        """;

    private readonly INutritionConnectionFactory _factory;

    public SqliteNutritionRepository(INutritionConnectionFactory factory)
    {
        _factory = factory;

        using var connection = factory.Create();
        connection.Execute(Schema);
    }

    public IReadOnlyList<Person> People()
    {
        using var connection = _factory.Create();
        return connection.Query<Person>("SELECT Id, Name FROM Person ORDER BY Name;").AsList();
    }

    public Person AddPerson(string name)
    {
        const string sql = "INSERT INTO Person (Name) VALUES (@name) RETURNING Id, Name;";
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("name", name)
            .ToDynamicParameters();

        using var connection = _factory.Create();
        return connection.QuerySingle<Person>(sql, parameters);
    }

    public IReadOnlyList<FoodEntry> FoodEntries(long personId)
    {
        const string sql =
            "SELECT Id, PersonId, LoggedOn, Description, Calories FROM FoodEntry " +
            "WHERE PersonId = @personId ORDER BY LoggedOn DESC, Id DESC;";
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("personId", personId)
            .ToDynamicParameters();

        using var connection = _factory.Create();
        return connection.Query<FoodRow>(sql, parameters).Select(Map).ToList();
    }

    public FoodEntry AddFoodEntry(long personId, DateOnly loggedOn, string description, int calories)
    {
        const string sql =
            "INSERT INTO FoodEntry (PersonId, LoggedOn, Description, Calories) " +
            "VALUES (@personId, @loggedOn, @description, @calories) " +
            "RETURNING Id, PersonId, LoggedOn, Description, Calories;";
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("personId", personId)
            .Add("loggedOn", Iso(loggedOn))
            .Add("description", description)
            .Add("calories", calories)
            .ToDynamicParameters();

        using var connection = _factory.Create();
        return Map(connection.QuerySingle<FoodRow>(sql, parameters));
    }

    public IReadOnlyList<WeightEntry> WeightEntries(long personId)
    {
        const string sql =
            "SELECT Id, PersonId, MeasuredOn, Kilograms FROM WeightEntry " +
            "WHERE PersonId = @personId ORDER BY MeasuredOn;";
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("personId", personId)
            .ToDynamicParameters();

        using var connection = _factory.Create();
        return connection.Query<WeightRow>(sql, parameters).Select(Map).ToList();
    }

    public WeightEntry AddWeightEntry(long personId, DateOnly measuredOn, double kilograms)
    {
        const string sql =
            "INSERT INTO WeightEntry (PersonId, MeasuredOn, Kilograms) " +
            "VALUES (@personId, @measuredOn, @kilograms) " +
            "ON CONFLICT (PersonId, MeasuredOn) DO UPDATE SET Kilograms = excluded.Kilograms " +
            "RETURNING Id, PersonId, MeasuredOn, Kilograms;";
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("personId", personId)
            .Add("measuredOn", Iso(measuredOn))
            .Add("kilograms", kilograms)
            .ToDynamicParameters();

        using var connection = _factory.Create();
        return Map(connection.QuerySingle<WeightRow>(sql, parameters));
    }

    private static FoodEntry Map(FoodRow row) =>
        new(row.Id, row.PersonId, Date(row.LoggedOn), row.Description, (int)row.Calories);

    private static WeightEntry Map(WeightRow row) =>
        new(row.Id, row.PersonId, Date(row.MeasuredOn), row.Kilograms);

    private static string Iso(DateOnly date) => date.ToString(IsoDate, CultureInfo.InvariantCulture);

    private static DateOnly Date(string iso) => DateOnly.ParseExact(iso, IsoDate, CultureInfo.InvariantCulture);

    // Column-order-matched projections of the reader rows (dates as text, integers as INTEGER/Int64).
    private sealed record FoodRow(long Id, long PersonId, string LoggedOn, string Description, long Calories);

    private sealed record WeightRow(long Id, long PersonId, string MeasuredOn, double Kilograms);
}

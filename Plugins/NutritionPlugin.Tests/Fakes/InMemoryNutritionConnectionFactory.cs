using Microsoft.Data.Sqlite;
using NutritionPlugin.Data;

namespace NutritionPlugin.Tests.Fakes;

/// <summary>
/// A <see cref="INutritionConnectionFactory"/> over a shared-cache in-memory database. Each instance
/// gets a uniquely named database and holds one connection open for its lifetime — a shared in-memory
/// SQLite database only survives while at least one connection to it is open — while handing out fresh
/// connections to the repository, matching how the real factory opens a connection per operation.
/// </summary>
public sealed class InMemoryNutritionConnectionFactory : INutritionConnectionFactory, IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection _keepAlive;

    public InMemoryNutritionConnectionFactory()
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"nutrition-test-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
        }.ToString();

        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    public SqliteConnection Create()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        return connection;
    }

    public void Dispose() => _keepAlive.Dispose();
}

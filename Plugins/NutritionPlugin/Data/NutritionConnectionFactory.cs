using Microsoft.Data.Sqlite;

namespace NutritionPlugin.Data;

/// <summary>
/// Opens connections to <c>nutrition.db</c> beside the application. Mirrors the host's
/// <c>SqliteConnectionFactory</c>: per-connection PRAGMAs (foreign_keys and busy_timeout are
/// per-connection in SQLite and must be re-issued on every open; WAL is db-wide but cheap to repeat).
/// </summary>
public sealed class NutritionConnectionFactory(string dataSource) : INutritionConnectionFactory
{
    private const string ConnectionPragmas = """
        PRAGMA journal_mode = WAL;
        PRAGMA foreign_keys = ON;
        PRAGMA busy_timeout = 5000;
        PRAGMA synchronous = NORMAL;
        """;

    private readonly string _connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = dataSource,
        Mode = SqliteOpenMode.ReadWriteCreate,
    }.ToString();

    public NutritionConnectionFactory()
        : this(Path.Combine(AppContext.BaseDirectory, "nutrition.db"))
    {
    }

    public SqliteConnection Create()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText = ConnectionPragmas;
        pragma.ExecuteNonQuery();

        return connection;
    }
}

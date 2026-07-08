using Microsoft.Data.Sqlite;

namespace ArcaPhylax.Data;

public sealed class SqliteConnectionFactory(string dataSource) : ISqliteConnectionFactory
{
    // Per-connection PRAGMAs. foreign_keys and busy_timeout are per-connection in SQLite (default
    // off / 0), so they MUST be set on every open — journal_mode=WAL is persisted db-wide but is
    // cheap and idempotent to re-issue. WAL gives the UI and the IndexingWorker reader/writer
    // concurrency without shared-cache, so we no longer set Cache=Shared.
    private const string ConnectionPragmas = """
        PRAGMA journal_mode = WAL;
        PRAGMA foreign_keys = ON;
        PRAGMA busy_timeout = 5000;
        PRAGMA synchronous = NORMAL;
        """;

    private readonly string _cs = new SqliteConnectionStringBuilder
    {
        DataSource = dataSource,
        Mode = SqliteOpenMode.ReadWriteCreate,
    }.ToString();

    public SqliteConnectionFactory()
        : this("app.db")
    {
    }

    public SqliteConnection Create()
    {
        var c = new SqliteConnection(_cs);
        c.Open();

        using var pragma = c.CreateCommand();
        pragma.CommandText = ConnectionPragmas;
        pragma.ExecuteNonQuery();

        return c;
    }
}

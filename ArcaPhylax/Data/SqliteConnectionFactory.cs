using Microsoft.Data.Sqlite;

namespace ArcaPhylax.Data;

public sealed class SqliteConnectionFactory(string dataSource) : ISqliteConnectionFactory
{
    private readonly string _cs = new SqliteConnectionStringBuilder
    {
        DataSource = dataSource,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Cache = SqliteCacheMode.Shared,
    }.ToString();

    public SqliteConnectionFactory()
        : this("app.db")
    {
    }

    public SqliteConnection Create()
    {
        var c = new SqliteConnection(_cs);
        c.Open();
        return c;
    }
}

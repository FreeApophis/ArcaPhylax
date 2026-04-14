using Microsoft.Data.Sqlite;

namespace ArcaPhylax.Data;

public interface ISqliteConnectionFactory
{
    SqliteConnection Create();
}

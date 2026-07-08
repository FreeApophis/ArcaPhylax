using ArcaPhylax.Data;
using ArcaPhylax.Infrastructure.Migrations;
using Microsoft.Data.Sqlite;

namespace ArcaPhylax.Infrastructure.Tests.Fakes;

/// <summary>
/// A throwaway file-backed SQLite database in a unique temp directory. File-backed (not
/// <c>:memory:</c>) because the runner opens its own connection and takes a WAL backup, both of which
/// need a shared on-disk file.
/// </summary>
public sealed class TempDatabase : IDisposable
{
    private readonly string _directory;

    public TempDatabase()
    {
        _directory = Path.Combine(Path.GetTempPath(), "arca-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        DbPath = Path.Combine(_directory, "test.db");
        Factory = new SqliteConnectionFactory(DbPath);
    }

    public string DbPath { get; }

    public SqliteConnectionFactory Factory { get; }

    public string BackupsDirectory => Path.Combine(_directory, "backups");

    public MigrationRunner Runner() => new(Factory, new DatabaseBackup(Factory));

    public long Scalar(string sql)
    {
        using var connection = Factory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    public void Dispose()
    {
        // Microsoft.Data.Sqlite pools connections; clear the pool so the file handles release before
        // we delete the temp directory.
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // A lingering handle on a WAL sidecar is not worth failing a test over.
        }
    }
}

using ArcaPhylax.Data;

namespace ArcaPhylax.Infrastructure.Migrations;

public sealed class DatabaseBackup(ISqliteConnectionFactory db) : IDatabaseBackup
{
    public string BackupTo(string destinationPath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(destinationPath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = db.Create();
        using var command = connection.CreateCommand();

        // VACUUM INTO cannot run inside a transaction; the fresh connection has none open.
        command.CommandText = "VACUUM INTO $dest;";
        var dest = command.CreateParameter();
        dest.ParameterName = "$dest";
        dest.Value = destinationPath;
        command.Parameters.Add(dest);
        command.ExecuteNonQuery();

        return destinationPath;
    }
}

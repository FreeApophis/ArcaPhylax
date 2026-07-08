namespace ArcaPhylax.Infrastructure.Migrations;

public interface IDatabaseBackup
{
    /// <summary>
    /// Writes a single, consistent, defragmented copy of the live database to
    /// <paramref name="destinationPath"/> using <c>VACUUM INTO</c> — the supported way to back up a
    /// WAL database as one file. Returns the destination path.
    /// </summary>
    string BackupTo(string destinationPath);
}

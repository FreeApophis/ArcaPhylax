using System.Collections.Immutable;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using ArcaPhylax.Data;
using ArcaPhylax.Extensions;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ArcaPhylax.Infrastructure.Migrations;

/// <summary>
/// Forward-only, checksum-guarded migration runner. Core migrations are embedded <c>.sql</c>
/// resources under <c>Migrations/Core/NNNN_name.sql</c>; each is applied once, in its own
/// transaction, tracked in <c>schema_migrations</c> keyed by (module, version). Before applying any
/// pending scripts the current database is snapshotted with <c>VACUUM INTO</c> so a failed batch has
/// a single-file recovery point.
/// </summary>
public sealed class MigrationRunner(ISqliteConnectionFactory db, IDatabaseBackup backup)
{
    public const string CoreModule = "core";

    private const string CoreResourceMarker = ".Migrations.Core.";

    private static readonly Assembly ResourceAssembly = typeof(MigrationRunner).Assembly;

    /// <summary>Applies every pending core migration; a no-op once the database is at head.</summary>
    public void RunCore() => Run(CoreModule, CoreScripts());

    /// <summary>The embedded core migration scripts, ordered by version.</summary>
    public static IReadOnlyList<MigrationScript> CoreScripts() => LoadScripts(CoreResourceMarker);

    public void Run(string module, IReadOnlyList<MigrationScript> scripts)
    {
        using var connection = db.Create();

        EnsureMigrationsTable(connection);
        var applied = LoadApplied(connection, module);
        VerifyAppliedChecksums(module, scripts, applied);

        var maxApplied = applied.Count == 0 ? 0 : applied.Keys.Max();
        var pending = scripts.Where(s => s.Version > maxApplied).OrderBy(s => s.Version).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        BackupBeforeMigrate(connection, module);

        foreach (var script in pending)
        {
            ApplyOne(connection, module, script);
        }

        if (module == CoreModule)
        {
            AssertNoForeignKeyViolations(connection);
        }
    }

    private static void EnsureMigrationsTable(SqliteConnection connection)
        => connection.Execute("""
            CREATE TABLE IF NOT EXISTS schema_migrations (
              module     TEXT NOT NULL,
              version    INTEGER NOT NULL,
              name       TEXT NOT NULL,
              checksum   TEXT NOT NULL,
              applied_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
              PRIMARY KEY (module, version)
            );
            """);

    private static ImmutableDictionary<int, string> LoadApplied(SqliteConnection connection, string module)
    {
        var parameters = ImmutableDictionary<string, object?>.Empty
            .Add("module", module)
            .ToDynamicParameters();

        var builder = ImmutableDictionary.CreateBuilder<int, string>();
        foreach (var row in connection.Query(
            "SELECT version, checksum FROM schema_migrations WHERE module = @module;", parameters))
        {
            builder[(int)(long)row.version] = (string)row.checksum;
        }

        return builder.ToImmutable();
    }

    private static void VerifyAppliedChecksums(
        string module,
        IReadOnlyList<MigrationScript> scripts,
        ImmutableDictionary<int, string> applied)
    {
        foreach (var script in scripts)
        {
            if (applied.TryGetValue(script.Version, out var recorded)
                && !string.Equals(recorded, script.Checksum, StringComparison.OrdinalIgnoreCase))
            {
                throw new MigrationException(
                    $"Migration '{module}' v{script.Version} ({script.Name}) was already applied with a "
                    + "different checksum — an already-applied migration script must never be edited.");
            }
        }
    }

    private void BackupBeforeMigrate(SqliteConnection connection, string module)
    {
        var source = connection.DataSource;
        if (string.IsNullOrEmpty(source) || source.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(source)) ?? ".";
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
        backup.BackupTo(Path.Combine(directory, "backups", $"pre-migrate-{module}-{stamp}.db"));
    }

    private static void ApplyOne(SqliteConnection connection, string module, MigrationScript script)
    {
        try
        {
            using var transaction = connection.BeginTransaction();

            using (var ddl = connection.CreateCommand())
            {
                ddl.Transaction = transaction;
                ddl.CommandText = script.Sql;
                ddl.ExecuteNonQuery();
            }

            using (var record = connection.CreateCommand())
            {
                record.Transaction = transaction;
                record.CommandText =
                    "INSERT INTO schema_migrations(module, version, name, checksum) "
                    + "VALUES($module, $version, $name, $checksum);";
                AddParameter(record, "$module", module);
                AddParameter(record, "$version", script.Version);
                AddParameter(record, "$name", script.Name);
                AddParameter(record, "$checksum", script.Checksum);
                record.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch (Exception ex) when (ex is not MigrationException)
        {
            throw new MigrationException(
                $"Migration '{module}' v{script.Version} ({script.Name}) failed and was rolled back.", ex);
        }
    }

    private static void AssertNoForeignKeyViolations(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_key_check;";

        var violations = new List<string>();
        using (var reader = command.ExecuteReader())
        {
            // Columns: table, rowid, referred-table, fk-index.
            while (reader.Read())
            {
                violations.Add($"{reader.GetValue(0)} (rowid {reader.GetValue(1)}) -> {reader.GetValue(2)}");
            }
        }

        if (violations.Count > 0)
        {
            throw new MigrationException(
                "Enabling foreign keys revealed pre-existing violations; fix or clear these rows:\n"
                + string.Join("\n", violations));
        }
    }

    private static void AddParameter(SqliteCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static IReadOnlyList<MigrationScript> LoadScripts(string marker)
        => ResourceAssembly.GetManifestResourceNames()
            .Where(name => name.Contains(marker, StringComparison.Ordinal)
                && name.EndsWith(".sql", StringComparison.Ordinal))
            .Select(name => ParseResource(name, marker))
            .OrderBy(script => script.Version)
            .ToList();

    private static MigrationScript ParseResource(string resourceName, string marker)
    {
        // e.g. "...Migrations.Core.0001_baseline.sql" -> file "0001_baseline.sql".
        var file = resourceName[(resourceName.IndexOf(marker, StringComparison.Ordinal) + marker.Length)..];
        var underscore = file.IndexOf('_');
        if (underscore < 0 || !int.TryParse(file[..underscore], out var version))
        {
            throw new MigrationException(
                $"Migration resource '{resourceName}' must be named NNNN_name.sql (numeric prefix).");
        }

        var name = file[(underscore + 1)..^".sql".Length];
        var sql = ReadResource(resourceName);
        return new MigrationScript(version, name, sql, Sha256Hex(sql));
    }

    private static string ReadResource(string resourceName)
    {
        using var stream = ResourceAssembly.GetManifestResourceStream(resourceName)
            ?? throw new MigrationException($"Embedded migration resource '{resourceName}' could not be opened.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string Sha256Hex(string text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}

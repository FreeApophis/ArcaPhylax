namespace ArcaPhylax.Infrastructure.Migrations;

/// <summary>
/// One forward-only migration for a module ("core" or a plugin id). <see cref="Checksum"/> is the
/// SHA-256 of <see cref="Sql"/> and lets the runner detect a script whose text changed after it was
/// already applied.
/// </summary>
public sealed record MigrationScript(int Version, string Name, string Sql, string Checksum);

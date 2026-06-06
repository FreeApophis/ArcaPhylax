namespace ArcaPhylax.Data.Postgres.Provisioning;

public sealed record PostgresInstallationStatus(
    bool IsInstalled,
    string? Version,
    string DetectionDetails);

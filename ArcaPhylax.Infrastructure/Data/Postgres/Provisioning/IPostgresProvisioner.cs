namespace ArcaPhylax.Data.Postgres.Provisioning;

public interface IPostgresProvisioner
{
    Task<PostgresInstallationStatus> EnsureInstalledAsync(
        CancellationToken cancellationToken = default);

    Task<PostgresInstallationStatus> EnsureInstalledAsync(
        PostgresProvisioningOptions options,
        CancellationToken cancellationToken = default);
}

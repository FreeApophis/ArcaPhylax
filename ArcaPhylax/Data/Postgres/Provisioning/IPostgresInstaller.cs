namespace ArcaPhylax.Data.Postgres.Provisioning;

public interface IPostgresInstaller
{
    Task InstallAsync(PostgresProvisioningOptions options, CancellationToken cancellationToken = default);
}

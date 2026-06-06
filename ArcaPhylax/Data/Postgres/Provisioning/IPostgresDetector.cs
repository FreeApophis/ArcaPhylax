namespace ArcaPhylax.Data.Postgres.Provisioning;

public interface IPostgresDetector
{
    Task<PostgresInstallationStatus> DetectAsync(CancellationToken cancellationToken = default);
}

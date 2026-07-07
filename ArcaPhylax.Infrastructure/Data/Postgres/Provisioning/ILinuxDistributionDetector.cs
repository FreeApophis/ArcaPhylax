namespace ArcaPhylax.Data.Postgres.Provisioning;

public interface ILinuxDistributionDetector
{
    Task<LinuxDistributionInfo?> DetectAsync(CancellationToken cancellationToken = default);
}

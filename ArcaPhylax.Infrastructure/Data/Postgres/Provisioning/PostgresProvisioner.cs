namespace ArcaPhylax.Data.Postgres.Provisioning;

public sealed class PostgresProvisioner(
    IPostgresDetector detector,
    IPostgresInstaller installer,
    PostgresProvisioningOptions options) : IPostgresProvisioner
{
    public Task<PostgresInstallationStatus> EnsureInstalledAsync(CancellationToken cancellationToken = default)
        => EnsureInstalledAsync(options, cancellationToken);

    public async Task<PostgresInstallationStatus> EnsureInstalledAsync(
        PostgresProvisioningOptions provisioningOptions,
        CancellationToken cancellationToken = default)
    {
        var before = await detector.DetectAsync(cancellationToken).ConfigureAwait(false);
        if (before.IsInstalled)
        {
            EnsurePreferredVersionIfNeeded(before, provisioningOptions);
            return before;
        }

        await installer.InstallAsync(provisioningOptions, cancellationToken).ConfigureAwait(false);

        var after = await detector.DetectAsync(cancellationToken).ConfigureAwait(false);
        if (!after.IsInstalled)
        {
            throw new InvalidOperationException(
                "PostgreSQL installation command completed, but PostgreSQL still could not be detected.");
        }

        EnsurePreferredVersionIfNeeded(after, provisioningOptions);
        return after;
    }

    private static void EnsurePreferredVersionIfNeeded(
        PostgresInstallationStatus status,
        PostgresProvisioningOptions options)
    {
        if (!options.EnforcePreferredVersion || string.IsNullOrWhiteSpace(options.PreferredVersion))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(status.Version) || !status.Version.StartsWith(options.PreferredVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Detected PostgreSQL version '{status.Version ?? "unknown"}' does not satisfy preferred major version '{options.PreferredVersion}'.");
        }
    }
}

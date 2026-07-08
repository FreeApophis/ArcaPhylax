using ArcaPhylax.Data.Postgres.Provisioning;
using ArcaPhylax.Infrastructure.Migrations;

namespace ArcaPhylax.Data;

public sealed class DbInitializer(MigrationRunner migrations) : IDbInitializer
{
    public void EnsureCreatedAndSeed()
    {
        // Postgres provisioning is background, best-effort infrastructure (it handles its own
        // exceptions); SQLite is the path the app actually depends on, so run its migrations
        // synchronously before the UI touches the database.
        _ = EnsurePostgresAsync();
        migrations.RunCore();
    }

    private static async Task EnsurePostgresAsync()
    {
        var os = new RuntimeOperatingSystemInfo();
        var runner = new ProcessCommandRunner();
        var detector = new PostgresDetector(os, runner);
        var distributionDetector = new LinuxDistributionDetector(os);
        var installer = new CompositePostgresInstaller(os, distributionDetector, runner);

        var options = new PostgresProvisioningOptions
        {
            PreferredVersion = "16",
            UseSudo = true,
            UseDefaultLinuxPackage = true,
            Windows = new WindowsInstallerOptions { SuperuserPassword = "ArcaPhylax" },
        };

        var provisioner = new PostgresProvisioner(detector, installer, options);

        try
        {
            var status = await provisioner.EnsureInstalledAsync(options);

            Console.WriteLine("PostgreSQL is installed.");
            Console.WriteLine($"Version: {status.Version ?? "unknown"}");
            Console.WriteLine($"Details: {status.DetectionDetails}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to ensure PostgreSQL is installed.");
            Console.Error.WriteLine(ex);
            Environment.ExitCode = 1;
        }
    }
}

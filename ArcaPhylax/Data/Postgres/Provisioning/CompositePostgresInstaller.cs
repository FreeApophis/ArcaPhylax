namespace ArcaPhylax.Data.Postgres.Provisioning;

public sealed class CompositePostgresInstaller(
    IOperatingSystemInfo operatingSystemInfo,
    ILinuxDistributionDetector linuxDistributionDetector,
    ICommandRunner runner) : IPostgresInstaller
{
    public async Task InstallAsync(PostgresProvisioningOptions options, CancellationToken cancellationToken = default)
    {
        if (operatingSystemInfo.IsLinux)
        {
            await InstallOnLinuxAsync(options, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (operatingSystemInfo.IsMacOs)
        {
            await InstallOnMacAsync(options, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (operatingSystemInfo.IsWindows)
        {
            await InstallOnWindowsAsync(options, cancellationToken).ConfigureAwait(false);
            return;
        }

        throw new PlatformNotSupportedException("Unsupported OS for PostgreSQL installation.");
    }

    internal static string CombineWindowsPath(string baseDirectory, string? version)
        => string.IsNullOrWhiteSpace(version) ? baseDirectory : Path.Combine(baseDirectory, version);

    internal static string EscapeWindowsArgument(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private async Task InstallOnLinuxAsync(PostgresProvisioningOptions options, CancellationToken cancellationToken)
    {
        var distribution = await linuxDistributionDetector.DetectAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new PlatformNotSupportedException("Could not detect Linux distribution.");

        if (distribution.IsDebianFamily)
        {
            var aptExists = await runner.RunAsync("which", "apt", cancellationToken).ConfigureAwait(false);
            if (!aptExists.Succeeded)
            {
                throw new PlatformNotSupportedException("Detected Debian-family distribution, but apt was not found.");
            }

            var prefix = options.UseSudo ? "sudo " : string.Empty;
            await RunShellAsync($"{prefix}apt update", cancellationToken).ConfigureAwait(false);
            await RunShellAsync($"{prefix}apt install -y {ResolveLinuxPackageName(options)}", cancellationToken).ConfigureAwait(false);
            return;
        }

        if (distribution.IsRhelFamily)
        {
            var packageManager = await ResolveRhelPackageManagerAsync(cancellationToken).ConfigureAwait(false);
            var prefix = options.UseSudo ? "sudo " : string.Empty;
            await RunShellAsync($"{prefix}{packageManager} install -y postgresql-server postgresql", cancellationToken).ConfigureAwait(false);
            return;
        }

        throw new PlatformNotSupportedException(
            $"Linux distribution '{distribution.Id}' is not supported by the built-in installer strategies.");
    }

    private async Task<string> ResolveRhelPackageManagerAsync(CancellationToken cancellationToken)
    {
        foreach (var candidate in new[] { "dnf", "yum" })
        {
            var result = await runner.RunAsync("which", candidate, cancellationToken).ConfigureAwait(false);
            if (result.Succeeded)
            {
                return candidate;
            }
        }

        throw new PlatformNotSupportedException("Neither dnf nor yum was found on this RHEL-family Linux system.");
    }

    private static string ResolveLinuxPackageName(PostgresProvisioningOptions options)
    {
        if (options.UseDefaultLinuxPackage || string.IsNullOrWhiteSpace(options.PreferredVersion))
        {
            return "postgresql";
        }

        return $"postgresql-{options.PreferredVersion}";
    }

    private async Task InstallOnMacAsync(PostgresProvisioningOptions options, CancellationToken cancellationToken)
    {
        var brewExists = await runner.RunAsync("which", "brew", cancellationToken).ConfigureAwait(false);
        if (!brewExists.Succeeded)
        {
            throw new InvalidOperationException(
                "Homebrew was not found. Install Homebrew first, or replace the macOS installer strategy.");
        }

        var formula = string.IsNullOrWhiteSpace(options.PreferredVersion)
            ? "postgresql"
            : $"postgresql@{options.PreferredVersion}";

        await runner.RunAsync("brew", $"install {formula}", cancellationToken, throwOnNonZeroExit: true)
            .ConfigureAwait(false);
    }

    private async Task InstallOnWindowsAsync(PostgresProvisioningOptions options, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(options.Windows.OfflineInstallerPath))
        {
            await InstallOnWindowsUsingEdbInstallerAsync(options, cancellationToken).ConfigureAwait(false);
            return;
        }

        var wingetExists = await runner.RunAsync("where", "winget", cancellationToken).ConfigureAwait(false);
        if (!wingetExists.Succeeded)
        {
            throw new InvalidOperationException(
                "No Windows installation strategy is available. Provide Windows:OfflineInstallerPath or install winget.");
        }

        var packageId = string.IsNullOrWhiteSpace(options.Windows.WingetPackageId)
            ? "PostgreSQL.PostgreSQL"
            : options.Windows.WingetPackageId;

        var args =
            $"install --id {packageId} --exact --silent " +
            "--accept-package-agreements --accept-source-agreements";

        await runner.RunAsync("winget", args, cancellationToken, throwOnNonZeroExit: true)
            .ConfigureAwait(false);
    }

    private async Task InstallOnWindowsUsingEdbInstallerAsync(PostgresProvisioningOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Windows.SuperuserPassword))
        {
            throw new InvalidOperationException(
                "Windows unattended EDB installation requires Windows:SuperuserPassword to be configured.");
        }

        var installerPath = options.Windows.OfflineInstallerPath!;
        if (!File.Exists(installerPath))
        {
            throw new FileNotFoundException("The configured Windows PostgreSQL installer was not found.", installerPath);
        }

        var installDir = CombineWindowsPath(options.Windows.InstallationDirectory, options.PreferredVersion);
        var dataDir = options.Windows.DataDirectory;

        var arguments = string.Join(' ', new[]
        {
            "--mode unattended",
            $"--superpassword \"{EscapeWindowsArgument(options.Windows.SuperuserPassword)}\"",
            $"--servicename \"{EscapeWindowsArgument(options.Windows.ServiceName)}\"",
            $"--serviceaccount \"{EscapeWindowsArgument(options.Windows.SuperuserName)}\"",
            $"--serverport {options.Windows.Port}",
            $"--prefix \"{EscapeWindowsArgument(installDir)}\"",
            $"--datadir \"{EscapeWindowsArgument(dataDir)}\"",
        });

        await runner.RunAsync(installerPath, arguments, cancellationToken, throwOnNonZeroExit: true)
            .ConfigureAwait(false);
    }

    private Task RunShellAsync(string command, CancellationToken cancellationToken) =>
        runner.RunAsync(
            "/bin/bash",
            $"-lc \"{EscapeForDoubleQuotedBash(command)}\"",
            cancellationToken,
            throwOnNonZeroExit: true);

    private static string EscapeForDoubleQuotedBash(string text) =>
        text.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

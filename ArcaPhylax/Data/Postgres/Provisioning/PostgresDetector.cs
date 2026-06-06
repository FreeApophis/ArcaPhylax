

namespace ArcaPhylax.Data.Postgres.Provisioning;

public sealed class PostgresDetector(
    IOperatingSystemInfo operatingSystemInfo,
    ICommandRunner runner) : IPostgresDetector
{
    public async Task<PostgresInstallationStatus> DetectAsync(CancellationToken cancellationToken = default)
    {
        var candidateCommands = operatingSystemInfo.IsWindows
            ? new[] { ("where", "psql"), ("where", "postgres") }
            : new[] { ("which", "psql"), ("which", "postgres") };

        foreach (var (probe, executable) in candidateCommands)
        {
            var probeResult = await runner.RunAsync(probe, executable, cancellationToken).ConfigureAwait(false);
            if (!probeResult.Succeeded || string.IsNullOrWhiteSpace(probeResult.StdOut))
            {
                continue;
            }

            var versionResult = await TryReadVersionAsync(executable, cancellationToken).ConfigureAwait(false);
            if (versionResult is not null)
            {
                return versionResult;
            }
        }

        foreach (var executable in new[] { "psql", "postgres" })
        {
            var versionResult = await TryReadVersionAsync(executable, cancellationToken).ConfigureAwait(false);
            if (versionResult is not null)
            {
                return versionResult;
            }
        }

        return new PostgresInstallationStatus(false, null, "Neither psql nor postgres was found on PATH.");
    }

    private async Task<PostgresInstallationStatus?> TryReadVersionAsync(
        string executable,
        CancellationToken cancellationToken)
    {
        var result = await runner.RunAsync(executable, "--version", cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return null;
        }

        var output = string.IsNullOrWhiteSpace(result.StdOut) ? result.StdErr : result.StdOut;
        var version = ParseVersion(output);
        return new PostgresInstallationStatus(true, version, output.Trim());
    }

    internal static string? ParseVersion(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            if (char.IsDigit(token[0]))
            {
                return token.Trim();
            }
        }

        return null;
    }
}

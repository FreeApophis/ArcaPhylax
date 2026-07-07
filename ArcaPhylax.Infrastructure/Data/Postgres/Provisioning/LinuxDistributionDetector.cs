namespace ArcaPhylax.Data.Postgres.Provisioning;

public sealed class LinuxDistributionDetector(
    IOperatingSystemInfo operatingSystemInfo) : ILinuxDistributionDetector
{
    public Task<LinuxDistributionInfo?> DetectAsync(CancellationToken cancellationToken = default)
    {
        if (!operatingSystemInfo.IsLinux)
        {
            return Task.FromResult<LinuxDistributionInfo?>(null);
        }

        const string osReleasePath = "/etc/os-release";
        if (!File.Exists(osReleasePath))
        {
            return Task.FromResult<LinuxDistributionInfo?>(null);
        }

        var pairs = File.ReadAllLines(osReleasePath)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#", StringComparison.Ordinal))
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(
                parts => parts[0],
                parts => parts[1].Trim().Trim('"'),
                StringComparer.OrdinalIgnoreCase);

        pairs.TryGetValue("ID", out var id);
        pairs.TryGetValue("VERSION_ID", out var versionId);
        pairs.TryGetValue("PRETTY_NAME", out var prettyName);

        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult<LinuxDistributionInfo?>(null);
        }

        var info = new LinuxDistributionInfo(id.ToLowerInvariant(), versionId, prettyName);
        return Task.FromResult<LinuxDistributionInfo?>(info);
    }
}

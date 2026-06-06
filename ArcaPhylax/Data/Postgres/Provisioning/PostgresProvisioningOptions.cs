namespace ArcaPhylax.Data.Postgres.Provisioning;

public sealed class PostgresProvisioningOptions
{
    /// <summary>
    /// Preferred major version, for example "16" or "17".
    /// Null means use the platform default package or installer.
    /// </summary>
    public string? PreferredVersion { get; set; }

    /// <summary>
    /// Whether Linux install commands should be prefixed with sudo.
    /// </summary>
    public bool UseSudo { get; set; } = true;

    /// <summary>
    /// Whether apt based Linux installs should use the distro default package name (postgresql)
    /// instead of a version-specific package (for example postgresql-16).
    /// </summary>
    public bool UseDefaultLinuxPackage { get; set; } = true;

    /// <summary>
    /// Whether the provisioner should verify the detected version against <see cref="PreferredVersion"/>.
    /// </summary>
    public bool EnforcePreferredVersion { get; set; }

    public WindowsInstallerOptions Windows { get; set; } = new();
}

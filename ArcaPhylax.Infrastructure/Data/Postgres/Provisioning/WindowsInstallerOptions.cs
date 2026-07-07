namespace ArcaPhylax.Data.Postgres.Provisioning;

public sealed class WindowsInstallerOptions
{
    /// <summary>
    /// If set, the provisioner uses this local installer path instead of winget.
    /// Expected to be an EDB PostgreSQL Windows installer executable.
    /// </summary>
    public string? OfflineInstallerPath { get; set; }

    /// <summary>
    /// Superuser password required by unattended EDB installs.
    /// </summary>
    public string? SuperuserPassword { get; set; }

    /// <summary>
    /// PostgreSQL service name.
    /// </summary>
    public string ServiceName { get; set; } = "postgresql-x64";

    /// <summary>
    /// Installation directory for the EDB installer.
    /// </summary>
    public string InstallationDirectory { get; set; } = @"C:\Program Files\PostgreSQL";

    /// <summary>
    /// Data directory for the EDB installer.
    /// </summary>
    public string DataDirectory { get; set; } = @"C:\Program Files\PostgreSQL\data";

    /// <summary>
    /// Port used by PostgreSQL.
    /// </summary>
    public int Port { get; set; } = 5432;

    /// <summary>
    /// Locale / superuser account name used during install.
    /// The EDB installer commonly expects "postgres" as the service account name.
    /// </summary>
    public string SuperuserName { get; set; } = "postgres";

    /// <summary>
    /// Additional installer source for winget, defaults to PostgreSQL.PostgreSQL when omitted.
    /// </summary>
    public string? WingetPackageId { get; set; }
}

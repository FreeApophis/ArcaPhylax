namespace ArcaPhylax.Data.Postgres.Provisioning;

public interface IOperatingSystemInfo
{
    bool IsWindows { get; }

    bool IsLinux { get; }

    bool IsMacOs { get; }
}

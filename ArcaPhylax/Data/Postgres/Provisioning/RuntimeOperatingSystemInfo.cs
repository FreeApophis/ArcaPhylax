using System.Runtime.InteropServices;

namespace ArcaPhylax.Data.Postgres.Provisioning;

public sealed class RuntimeOperatingSystemInfo : IOperatingSystemInfo
{
    public bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public bool IsMacOs => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
}

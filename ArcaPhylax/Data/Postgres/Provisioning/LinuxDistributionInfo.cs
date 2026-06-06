namespace ArcaPhylax.Data.Postgres.Provisioning;

public sealed record LinuxDistributionInfo(
    string Id,
    string? VersionId,
    string? PrettyName)
{
    public bool IsDebianFamily =>
        Id is "debian" or "ubuntu" or "linuxmint" or "pop" or "neon" or "elementary";

    public bool IsRhelFamily =>
        Id is "rhel" or "centos" or "rocky" or "almalinux" or "fedora";
}

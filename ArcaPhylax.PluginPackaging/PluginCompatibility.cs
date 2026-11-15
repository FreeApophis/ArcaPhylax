namespace ArcaPhylax.PluginPackaging;

/// <summary>
/// Whether a plugin can run on this host: same platform, and an SDK the host can serve: the same major
/// version (breaking changes bump the major) and a minor no newer than the host's (a plugin built
/// against a newer minor may use contract members this host does not have).
/// </summary>
public static class PluginCompatibility
{
    public static Either<InstallError, PluginManifest> Check(PluginManifest manifest, HostEnvironment host)
        => (SamePlatform(manifest, host), CanServe(Version.Parse(manifest.SdkVersion), host.SdkVersion)) switch
        {
            (false, _) => Either<InstallError, PluginManifest>.Left(new WrongPlatform(manifest.Platform, host.Platform)),
            (true, false) => Either<InstallError, PluginManifest>.Left(new IncompatibleSdk(manifest.SdkVersion, host.SdkVersion.ToString(3))),
            (true, true) => Either<InstallError, PluginManifest>.Right(manifest),
        };

    private static bool SamePlatform(PluginManifest manifest, HostEnvironment host)
        => string.Equals(manifest.Platform, host.Platform, StringComparison.OrdinalIgnoreCase);

    private static bool CanServe(Version pluginSdk, Version hostSdk)
        => pluginSdk.Major == hostSdk.Major && pluginSdk.Minor <= hostSdk.Minor;
}

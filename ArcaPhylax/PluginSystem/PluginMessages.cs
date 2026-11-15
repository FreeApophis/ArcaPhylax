using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.PluginSystem;

/// <summary>User-facing texts for install results and the reasons a plugin is refused.</summary>
public static class PluginMessages
{
    public static string Of(InstallReport report) => report switch
    {
        PluginActivated activated => $"{activated.Manifest.Name} {activated.Manifest.Version} was installed and is ready.",
        RestartRequired restart => $"{restart.Manifest.Name} {restart.Manifest.Version} was installed. Restart ArcaPhylax to use it.",
        InstallRejected rejected => $"Not installed: {Of(rejected.Error)}",
        LoadFailed failed => $"{failed.Manifest.Name} was installed but failed to load: {failed.Message}",
    };

    public static string Of(InstallError error) => error switch
    {
        InvalidPackage invalid => Of(invalid.Error),
        Untrusted untrusted => Of(untrusted.Error),
        WrongPlatform platform => $"the package is for {platform.PluginPlatform}, but this computer needs {platform.HostPlatform}.",
        IncompatibleSdk sdk => $"it was built for plugin SDK {sdk.PluginSdkVersion}; this ArcaPhylax provides {sdk.HostSdkVersion}.",
        AlreadyInstalled installed => $"{installed.Id} {installed.Version} is already installed.",
        NotInstalled notInstalled => $"{notInstalled.Id} is not installed.",
    };

    public static string Of(PackageError error) => error switch
    {
        NotAPackage => "the file is not an ArcaPhylax plugin package.",
        MissingEntry missing when missing.Path == PluginPackage.SignatureFileName => "the package is not signed.",
        MissingEntry missing => $"the package is missing '{missing.Path}'.",
        UnsafeEntryPath unsafePath => $"the package contains an unsafe file path ('{unsafePath.Path}').",
        InvalidManifest manifest => $"its plugin.json is invalid: {manifest.Reason}",
        InvalidSignature => "its signature does not match its contents; the file may have been tampered with.",
    };

    public static string Of(TrustError error) => error switch
    {
        UnknownPublisher unknown => $"it is signed by an unknown publisher (key {unknown.Key.KeyId}).",
        NotOwner notOwner => $"{notOwner.SignerName} may not publish the plugin id '{notOwner.PluginId}'.",
        PublisherBlocked blocked => $"you blocked its publisher (key {blocked.KeyId}).",
        PublisherRevoked revoked => $"its publisher's key {revoked.KeyId} was revoked by ArcaPhylax.",
    };
}

namespace ArcaPhylax.PluginPackaging;

/// <summary>Why a plugin could not be installed or removed. Closed: callers switch over it exhaustively.</summary>
public closed record InstallError;

/// <summary>The package itself was rejected (signature, layout, manifest).</summary>
public sealed record InvalidPackage(PackageError Error) : InstallError;

/// <summary>The package was published for a different platform than the running host.</summary>
public sealed record WrongPlatform(string PluginPlatform, string HostPlatform) : InstallError;

/// <summary>The plugin was built against an SDK version this host cannot load.</summary>
public sealed record IncompatibleSdk(string PluginSdkVersion, string HostSdkVersion) : InstallError;

/// <summary>Exactly this plugin version is already installed.</summary>
public sealed record AlreadyInstalled(string Id, string Version) : InstallError;

/// <summary>No installed plugin has this id.</summary>
public sealed record NotInstalled(string Id) : InstallError;

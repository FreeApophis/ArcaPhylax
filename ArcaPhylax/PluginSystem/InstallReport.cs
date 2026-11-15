using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.PluginSystem;

/// <summary>What happened when the user installed a package, as far as the running app is concerned. Closed.</summary>
public closed record InstallReport;

/// <summary>Installed and loaded; its tabs are available now.</summary>
public sealed record PluginActivated(PluginManifest Manifest) : InstallReport;

/// <summary>Installed, but it replaces a plugin that is already loaded; takes effect at the next start.</summary>
public sealed record RestartRequired(PluginManifest Manifest) : InstallReport;

/// <summary>Not installed.</summary>
public sealed record InstallRejected(InstallError Error) : InstallReport;

/// <summary>Installed, but loading it failed (the plugin itself threw).</summary>
public sealed record LoadFailed(PluginManifest Manifest, string Message) : InstallReport;

using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.PluginSystem;

/// <summary>Where a loaded plugin came from. Closed.</summary>
public closed record PluginOrigin;

/// <summary>Shipped next to the application (developer builds, built-in plugins).</summary>
public sealed record Bundled(string Directory) : PluginOrigin;

/// <summary>Installed from a signed <c>.arcaplugin</c> package into the per-user store.</summary>
public sealed record FromStore(InstalledPlugin Plugin) : PluginOrigin;

namespace ArcaPhylax.PluginPackaging;

/// <summary>What a successful install means for the running app. Closed.</summary>
public closed record InstallOutcome(InstalledPlugin Plugin);

/// <summary>A plugin that was not installed before: it can be loaded right away.</summary>
public sealed record InstalledNow(InstalledPlugin Plugin) : InstallOutcome(Plugin);

/// <summary>A new version of an installed plugin: it replaces the old one at the next start.</summary>
public sealed record UpdateStaged(InstalledPlugin Plugin) : InstallOutcome(Plugin);

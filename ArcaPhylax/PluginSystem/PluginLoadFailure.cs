namespace ArcaPhylax.PluginSystem;

/// <summary>A plugin that could not be loaded at startup; shown in the Plugins tab instead of crashing the app.</summary>
public sealed record PluginLoadFailure(string Name, PluginOrigin Origin, string Message);

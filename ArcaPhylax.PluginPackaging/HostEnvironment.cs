namespace ArcaPhylax.PluginPackaging;

/// <summary>What the running host offers plugins: its platform (RID) and plugin SDK version.</summary>
public sealed record HostEnvironment(string Platform, Version SdkVersion);

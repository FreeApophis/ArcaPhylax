namespace ArcaPhylax.PluginPackaging;

/// <summary>A key the user chose to trust on this computer, with the name they know it by.</summary>
public sealed record UserTrustedKey(string Name, PublisherKey Key);

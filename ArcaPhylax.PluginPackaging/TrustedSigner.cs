namespace ArcaPhylax.PluginPackaging;

/// <summary>Why a signer is trusted. Closed.</summary>
public closed record TrustedSigner;

/// <summary>Built into ArcaPhylax or endorsed by the signed publisher list.</summary>
public sealed record EndorsedSigner(Publisher Publisher) : TrustedSigner;

/// <summary>Trusted by the user on this computer only.</summary>
public sealed record UserTrustedSigner(UserTrustedKey Key) : TrustedSigner;

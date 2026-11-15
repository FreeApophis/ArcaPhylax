namespace ArcaPhylax.PluginPackaging;

/// <summary>Why a correctly signed plugin is still not trusted. Closed: callers switch over it exhaustively.</summary>
public closed record TrustError;

/// <summary>Neither endorsed nor trusted by the user. Carries the key so the user can choose to trust it.</summary>
public sealed record UnknownPublisher(PublisherKey Key) : TrustError;

/// <summary>The signer may not publish this plugin id: it belongs to another publisher, or outside the signer's prefixes.</summary>
public sealed record NotOwner(string PluginId, string SignerName) : TrustError;

/// <summary>The user blocked this publisher on this computer.</summary>
public sealed record PublisherBlocked(string KeyId) : TrustError;

/// <summary>The publisher list revoked this key (e.g. it was compromised). Users cannot override a revocation.</summary>
public sealed record PublisherRevoked(string KeyId) : TrustError;

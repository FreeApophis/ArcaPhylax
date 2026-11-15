namespace ArcaPhylax.PluginPackaging;

/// <summary>A publisher as the user sees it in the trust settings. Closed.</summary>
public closed record PublisherStanding;

/// <summary>Built in or endorsed by the publisher list (and not blocked by the user).</summary>
public sealed record EndorsedPublisher(Publisher Publisher) : PublisherStanding;

/// <summary>Trusted by the user on this computer.</summary>
public sealed record UserTrustedPublisher(UserTrustedKey Key) : PublisherStanding;

/// <summary>Blocked by the user; <paramref name="Name"/> is known when the key belongs to an endorsed publisher.</summary>
public sealed record BlockedPublisher(string KeyId, Option<string> Name) : PublisherStanding;

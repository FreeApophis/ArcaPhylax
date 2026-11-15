using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.PluginSystem;

/// <summary>
/// The trust compiled into ArcaPhylax. Compiled in on purpose: a key list on disk could be edited by
/// anything that can write the user's profile. Everything else (third-party publishers, revocations) comes
/// from the publisher list, which only counts when <see cref="Root"/> signed it.
/// </summary>
public static class TrustAnchors
{
    /// <summary>
    /// The ArcaPhylax root key (ECDSA P-256). It signs publishers.json and nothing else; its private half
    /// is kept offline, never in CI.
    /// </summary>
    public static PublisherKey Root { get; } = new(
        "63d077039583a53a",
        "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE4u4tJe9ykuuKmaswvCYwP6HY5JgwNxNCxx+UJSKiTDFtjkI88UU9HW2RNgj7DOp9CWgmH8rQL52rTpvYLG/jHw==");

    /// <summary>
    /// First-party publishers, trusted even before any publisher list was downloaded. The signed list can
    /// still revoke them or change their plugin prefixes.
    /// </summary>
    public static IReadOnlyList<Publisher> BuiltIn { get; } =
    [
        new(
            "ArcaPhylax",
            new PublisherKey(
                "87753d6914f01b2e",
                "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE/skWFnClXeotVQFK+Si9GGna3HkJbeBGJsNHVojz+yvr7YuwsW/qnbNaTD7qDrt9gcLVjmdt6pTPyv7zcve7ZA=="),
            ["ArcaPhylax"]),
    ];
}

using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.PluginSystem;

/// <summary>
/// Publisher keys whose signed <c>.arcaplugin</c> packages this host installs. Compiled in on purpose:
/// a key list on disk could be edited by anything that can write the user's profile.
/// </summary>
public static class TrustedPublisherKeys
{
    public static IReadOnlyCollection<PublisherKey> All { get; } =
    [
        // ArcaPhylax official plugins (ECDSA P-256).
        new("87753d6914f01b2e", "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE/skWFnClXeotVQFK+Si9GGna3HkJbeBGJsNHVojz+yvr7YuwsW/qnbNaTD7qDrt9gcLVjmdt6pTPyv7zcve7ZA=="),
    ];
}

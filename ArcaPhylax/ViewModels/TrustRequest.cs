using ArcaPhylax.PluginPackaging;
using ArcaPhylax.PluginSystem;

namespace ArcaPhylax.ViewModels;

/// <summary>An install that stopped at an unknown publisher; <paramref name="Retry"/> installs again once the user trusts <paramref name="Key"/>.</summary>
public sealed record TrustRequest(PublisherKey Key, Func<Task<InstallReport>> Retry)
{
    public string KeyId => Key.KeyId;
}

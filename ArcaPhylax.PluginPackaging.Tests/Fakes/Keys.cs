using System.Security.Cryptography;

namespace ArcaPhylax.PluginPackaging.Tests.Fakes;

/// <summary>Fresh ECDSA P-256 keys for tests.</summary>
public static class Keys
{
    public static ECDsa NewKey() => ECDsa.Create(ECCurve.NamedCurves.nistP256);

    public static PublisherKey NewPublicKey()
    {
        using var key = NewKey();
        return PublisherKey.Of(key);
    }
}

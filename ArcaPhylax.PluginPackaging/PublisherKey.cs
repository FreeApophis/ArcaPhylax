using System.Security.Cryptography;

namespace ArcaPhylax.PluginPackaging;

/// <summary>
/// A trusted publisher's public key (ECDSA P-256, SubjectPublicKeyInfo, base64). The host ships the
/// keys it trusts; a package names its key by <see cref="KeyId"/>.
/// </summary>
public sealed record PublisherKey(string KeyId, string PublicKey)
{
    public static PublisherKey FromPublicKey(byte[] subjectPublicKeyInfo)
        => new(KeyIdOf(subjectPublicKeyInfo), Convert.ToBase64String(subjectPublicKeyInfo));

    public static PublisherKey Of(ECDsa key) => FromPublicKey(key.ExportSubjectPublicKeyInfo());

    private static string KeyIdOf(byte[] subjectPublicKeyInfo)
        => Convert.ToHexStringLower(SHA256.HashData(subjectPublicKeyInfo))[..16];
}

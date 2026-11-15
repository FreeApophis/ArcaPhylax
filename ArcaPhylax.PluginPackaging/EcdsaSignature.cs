using System.Security.Cryptography;

namespace ArcaPhylax.PluginPackaging;

/// <summary>ECDSA P-256 / SHA-256 signatures in IEEE P1363 form, base64-encoded. Used for packages and the publisher list.</summary>
internal static class EcdsaSignature
{
    public const string Algorithm = "ECDSA-P256-SHA256";

    private const DSASignatureFormat Format = DSASignatureFormat.IeeeP1363FixedFieldConcatenation;

    public static string Sign(byte[] data, ECDsa key)
        => Convert.ToBase64String(key.SignData(data, HashAlgorithmName.SHA256, Format));

    /// <summary>Whether <paramref name="signature"/> over <paramref name="data"/> was made by <paramref name="key"/>.</summary>
    public static bool IsValid(byte[] data, string signature, PublisherKey key)
        => (Bytes(signature), Bytes(key.PublicKey)) switch
        {
            ({ } signatureBytes, { } publicKey) => Verifies(data, signatureBytes, publicKey),
            _ => false,
        };

    private static bool Verifies(byte[] data, byte[] signature, byte[] subjectPublicKeyInfo)
    {
        using var verifier = ECDsa.Create();

        // A malformed public key is "not a valid signature", not a crash.
        try
        {
            verifier.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out _);
        }
        catch (CryptographicException)
        {
            return false;
        }

        return verifier.VerifyData(data, signature, HashAlgorithmName.SHA256, Format);
    }

    private static byte[]? Bytes(string base64)
    {
        var buffer = new byte[base64.Length];
        return Convert.TryFromBase64String(base64, buffer, out var written) ? buffer[..written] : null;
    }
}

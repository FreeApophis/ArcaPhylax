using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ArcaPhylax.PluginPackaging;

/// <summary>The <c>signature.json</c> entry: which key signed the content listing, and the signature.</summary>
internal sealed record PackageSignature(string KeyId, string Algorithm, string Signature)
{
    public const string EcdsaP256Sha256 = "ECDSA-P256-SHA256";

    private const DSASignatureFormat Format = DSASignatureFormat.IeeeP1363FixedFieldConcatenation;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    public static PackageSignature Create(string contentListing, ECDsa signingKey)
        => new(
            PublisherKey.Of(signingKey).KeyId,
            EcdsaP256Sha256,
            Convert.ToBase64String(signingKey.SignData(Bytes(contentListing), HashAlgorithmName.SHA256, Format)));

    public static Option<PackageSignature> Parse(string json)
    {
        // A malformed signature file is simply "no usable signature"; the caller reports InvalidSignature.
        try
        {
            return Option.FromNullable(JsonSerializer.Deserialize<PackageSignature>(json, JsonOptions));
        }
        catch (JsonException)
        {
            return Option<PackageSignature>.None;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>Whether this signature over <paramref name="contentListing"/> was made by <paramref name="key"/>.</summary>
    public bool IsValidFor(string contentListing, PublisherKey key)
    {
        using var verifier = ECDsa.Create();
        verifier.ImportSubjectPublicKeyInfo(Convert.FromBase64String(key.PublicKey), out _);

        return Algorithm == EcdsaP256Sha256
            && SignatureBytes().Match(
                none: false,
                some: signature => verifier.VerifyData(Bytes(contentListing), signature, HashAlgorithmName.SHA256, Format));
    }

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private Option<byte[]> SignatureBytes()
    {
        var buffer = new byte[Signature.Length];
        return Convert.TryFromBase64String(Signature, buffer, out var written)
            ? buffer[..written]
            : Option<byte[]>.None;
    }
}

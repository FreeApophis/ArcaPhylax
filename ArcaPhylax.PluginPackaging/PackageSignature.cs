using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ArcaPhylax.PluginPackaging;

/// <summary>
/// The <c>signature.json</c> entry: the signing key (id and public key) and the signature over the content
/// listing. The package proves who signed it; whether that signer is trusted is decided by <see cref="TrustPolicy"/>.
/// </summary>
internal sealed record PackageSignature(string KeyId, string Algorithm, string Signature, string PublicKey)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    public static PackageSignature Create(string contentListing, ECDsa signingKey)
    {
        var key = PublisherKey.Of(signingKey);
        return new(key.KeyId, EcdsaSignature.Algorithm, EcdsaSignature.Sign(Bytes(contentListing), signingKey), key.PublicKey);
    }

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

    /// <summary>The signing key, if the embedded public key is well-formed and really has <see cref="KeyId"/>.</summary>
    public Option<PublisherKey> Signer()
    {
        var buffer = new byte[PublicKey.Length];
        return Convert.TryFromBase64String(PublicKey, buffer, out var written)
            ? Option.Some(PublisherKey.FromPublicKey(buffer[..written])).Where(key => key.KeyId == KeyId)
            : Option<PublisherKey>.None;
    }

    /// <summary>Whether this signature over <paramref name="contentListing"/> was made by <paramref name="key"/>.</summary>
    public bool IsValidFor(string contentListing, PublisherKey key)
        => Algorithm == EcdsaSignature.Algorithm && EcdsaSignature.IsValid(Bytes(contentListing), Signature, key);

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);
}

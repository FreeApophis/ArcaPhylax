using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ArcaPhylax.PluginPackaging;

/// <summary>
/// <c>publishers.json</c>: the publishers the ArcaPhylax root key endorses, and the keys it revoked. It is
/// signed with the root key; <see cref="Sequence"/> only grows, so a host can refuse an older copy that
/// would bring back a revoked publisher.
/// </summary>
public sealed record PublisherList(long Sequence, ImmutableList<Publisher> Publishers, ImmutableList<string> RevokedKeyIds)
{
    public const string FileName = "publishers.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    public static PublisherList Empty { get; } = new(0, [], []);

    /// <summary>The list inside a signed <c>publishers.json</c>, if <paramref name="root"/> signed it.</summary>
    public static Either<string, PublisherList> Verify(string signedJson, PublisherKey root)
        => from envelope in Deserialize<SignedEnvelope>(signedJson)
           from payload in SignedBy(envelope, root)
           from list in Deserialize<PublisherList>(Encoding.UTF8.GetString(payload))
           select list;

    /// <summary>The signed <c>publishers.json</c> content.</summary>
    public string Sign(ECDsa root)
    {
        var payload = Encoding.UTF8.GetBytes(ToJson());
        var envelope = new SignedEnvelope(
            Convert.ToBase64String(payload),
            PublisherKey.Of(root).KeyId,
            EcdsaSignature.Algorithm,
            EcdsaSignature.Sign(payload, root));
        return JsonSerializer.Serialize(envelope, JsonOptions);
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>This list with <paramref name="publisher"/> added (or replacing the entry for its key).</summary>
    public PublisherList Endorse(Publisher publisher)
        => new(
            Sequence + 1,
            Publishers
                .Where(existing => existing.Key.KeyId != publisher.Key.KeyId)
                .Append(publisher)
                .OrderBy(existing => existing.Name, StringComparer.OrdinalIgnoreCase)
                .ToImmutableList(),
            RevokedKeyIds.Remove(publisher.Key.KeyId));

    /// <summary>This list without the publisher of <paramref name="keyId"/>, which is revoked from now on.</summary>
    public PublisherList Revoke(string keyId)
        => new(
            Sequence + 1,
            Publishers.RemoveAll(existing => existing.Key.KeyId == keyId),
            RevokedKeyIds.Remove(keyId).Add(keyId));

    private static Either<string, byte[]> SignedBy(SignedEnvelope envelope, PublisherKey root)
    {
        var buffer = new byte[envelope.Payload.Length];
        return envelope.KeyId == root.KeyId
            && envelope.Algorithm == EcdsaSignature.Algorithm
            && Convert.TryFromBase64String(envelope.Payload, buffer, out var written)
            && EcdsaSignature.IsValid(buffer[..written], envelope.Signature, root)
                ? Either<string, byte[]>.Right(buffer[..written])
                : Either<string, byte[]>.Left("the publisher list is not signed by the ArcaPhylax root key.");
    }

    private static Either<string, T> Deserialize<T>(string json)
        where T : class
    {
        // Malformed input is reported, not thrown: the list comes from the network or a cache file.
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions) is { } value
                ? Either<string, T>.Right(value)
                : Either<string, T>.Left("the publisher list is empty.");
        }
        catch (JsonException exception)
        {
            return Either<string, T>.Left($"the publisher list is not valid: {exception.Message}");
        }
    }
}

using System.Collections.Immutable;
using System.Text.Json;

namespace ArcaPhylax.PluginPackaging;

/// <summary>
/// The user's own overrides: keys they trust although nobody endorsed them (e.g. their own development key),
/// and keys they block although they are endorsed.
/// </summary>
public sealed record UserTrust(ImmutableList<UserTrustedKey> Trusted, ImmutableList<string> BlockedKeyIds)
{
    public const string FileName = "trust.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    public static UserTrust Empty { get; } = new([], []);

    /// <summary>The stored overrides; a missing or unreadable file means "no overrides".</summary>
    public static UserTrust Parse(string json)
    {
        // An unreadable file must not lock the user out of the app; it falls back to "no overrides".
        try
        {
            return JsonSerializer.Deserialize<UserTrust>(json, JsonOptions) ?? Empty;
        }
        catch (JsonException)
        {
            return Empty;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public UserTrust Trust(string name, PublisherKey key)
        => Forget(key.KeyId) with { Trusted = Forget(key.KeyId).Trusted.Add(new UserTrustedKey(name, key)) };

    public UserTrust Block(string keyId)
        => Forget(keyId) with { BlockedKeyIds = Forget(keyId).BlockedKeyIds.Add(keyId) };

    /// <summary>Removes any trust or block for <paramref name="keyId"/>.</summary>
    public UserTrust Forget(string keyId)
        => new(
            Trusted.RemoveAll(trusted => trusted.Key.KeyId == keyId),
            BlockedKeyIds.RemoveAll(blocked => blocked == keyId));
}

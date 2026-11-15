using System.Security.Cryptography;
using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.PluginPack;

/// <summary>
/// <c>endorse</c> / <c>revoke</c>: edit and re-sign <c>publishers.json</c> with the ArcaPhylax root key.
/// Run locally; the root key never goes to CI.
/// </summary>
internal static class PublisherCommands
{
    public static int Endorse(string listPath, IReadOnlyList<string> options)
        => (from pairs in ToolOptions.Pairs(options)
            from root in pairs.Value("--root")
            from name in pairs.Value("--name")
            from publicKey in pairs.Value("--public-key")
            from prefixes in pairs.Value("--plugins")
            select Run(listPath, root, list => PublisherFrom(name, publicKey, prefixes).Select(list.Endorse)))
            .GetOrElse(Usage);

    public static int Revoke(string listPath, IReadOnlyList<string> options)
        => (from pairs in ToolOptions.Pairs(options)
            from root in pairs.Value("--root")
            from keyId in pairs.Value("--key-id")
            select Run(listPath, root, list => Either<string, PublisherList>.Right(list.Revoke(keyId))))
            .GetOrElse(Usage);

    private static int Run(string listPath, string rootKeyPath, Func<PublisherList, Either<string, PublisherList>> change)
    {
        using var root = ECDsa.Create();
        root.ImportFromPem(File.ReadAllText(rootKeyPath));

        return Existing(listPath, PublisherKey.Of(root))
            .SelectMany(change)
            .Match(
                left: error =>
                {
                    Console.Error.WriteLine($"error: {error}");
                    return 1;
                },
                right: list =>
                {
                    File.WriteAllText(listPath, list.Sign(root));
                    Console.WriteLine($"{listPath} signed with root key {PublisherKey.Of(root).KeyId}, sequence {list.Sequence}:");
                    foreach (var publisher in list.Publishers)
                    {
                        Console.WriteLine($"  {publisher.Name} ({publisher.Key.KeyId}): {string.Join(", ", publisher.PluginPrefixes)}");
                    }

                    Console.WriteLine($"  revoked: {(list.RevokedKeyIds.IsEmpty ? "none" : string.Join(", ", list.RevokedKeyIds))}");
                    return 0;
                });
    }

    private static Either<string, PublisherList> Existing(string listPath, PublisherKey root)
        => File.Exists(listPath)
            ? PublisherList.Verify(File.ReadAllText(listPath), root)
            : Either<string, PublisherList>.Right(PublisherList.Empty);

    private static Either<string, Publisher> PublisherFrom(string name, string publicKey, string prefixes)
    {
        var buffer = new byte[publicKey.Length];
        return Convert.TryFromBase64String(publicKey, buffer, out var written) && IsP256PublicKey(buffer[..written])
            ? Either<string, Publisher>.Right(new Publisher(
                name,
                PublisherKey.FromPublicKey(buffer[..written]),
                [.. prefixes.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]))
            : Either<string, Publisher>.Left("--public-key must be the base64 public key that `keygen` printed.");
    }

    private static bool IsP256PublicKey(byte[] subjectPublicKeyInfo)
    {
        using var key = ECDsa.Create();

        // Import throws for anything that is not an EC public key; that is a usage error here.
        try
        {
            key.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out _);
            return key.ExportParameters(includePrivateParameters: false).Curve.Oid.Value == ECCurve.NamedCurves.nistP256.Oid.Value;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static int Usage()
    {
        Console.Error.WriteLine(
            """
            Usage:
              arcaphylax-pluginpack endorse <publishers.json> --root <root-key-file> --name <name> --public-key <base64> --plugins <prefix;prefix>
                  Endorses a publisher for the plugin id prefixes (Contoso owns Contoso and Contoso.*).
              arcaphylax-pluginpack revoke <publishers.json> --root <root-key-file> --key-id <key id>
                  Removes a publisher and revokes its key; installed plugins it signed stop loading.
            """);
        return 2;
    }
}

using System.Security.Cryptography;
using System.Text.Json;
using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.PluginPack;

internal static class KeyGen
{
    public static int Run(string keyPath)
    {
        if (File.Exists(keyPath))
        {
            Console.Error.WriteLine($"Refusing to overwrite existing key '{keyPath}'.");
            return 1;
        }

        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        File.WriteAllText(keyPath, key.ExportPkcs8PrivateKeyPem());

        Console.WriteLine($"Private key written to '{keyPath}'. Keep it secret and out of the repository.");
        Console.WriteLine("Add this public key to the host's trusted plugin keys:");
        Console.WriteLine(JsonSerializer.Serialize(PublisherKey.Of(key), ToolJson.Options));
        return 0;
    }
}

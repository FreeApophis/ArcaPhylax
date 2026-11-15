using System.Runtime.InteropServices;

namespace ArcaPhylax.PluginPack;

internal static class Program
{
    private const string KeyEnvironmentVariable = "ARCAPHYLAX_PLUGIN_KEY";

    private static int Main(string[] args) => args switch
    {
        ["keygen", var keyPath] => KeyGen.Run(keyPath),
        ["pack", var project, .. var options] => PackOptions.Parse(project, options, DefaultKeyPath(), RuntimeInformation.RuntimeIdentifier)
            .Match(none: Usage, some: Pack.Run),
        _ => Usage(),
    };

    private static Option<string> DefaultKeyPath()
        => Option.FromNullable(Environment.GetEnvironmentVariable(KeyEnvironmentVariable));

    private static int Usage()
    {
        Console.Error.WriteLine(
            $"""
            Usage:
              arcaphylax-pluginpack keygen <private-key-file>
                  Creates an ECDSA P-256 signing key (PKCS#8 PEM). Keep it out of the repository.

              arcaphylax-pluginpack pack <plugin.csproj> [--key <private-key-file>] [--rid <rid>] [--out <dir>] [--configuration <cfg>] [--catalog <catalog.json>]
                  Publishes the plugin for <rid> (default: this machine), packs and signs it as .arcaplugin,
                  verifies the result and prints its catalog entry. --key defaults to ${KeyEnvironmentVariable}.
                  --catalog adds the package to that catalog.json (created if missing), linked relative to it.
            """);
        return 2;
    }
}

namespace ArcaPhylax.PluginPack;

internal sealed record PackOptions(string Project, string KeyPath, string RuntimeIdentifier, string OutputDirectory, string Configuration, Option<string> Catalog)
{
    /// <summary>The options, or None when the arguments are incomplete (no key) or malformed.</summary>
    public static Option<PackOptions> Parse(string project, IReadOnlyList<string> options, Option<string> defaultKey, string defaultRid)
        => from pairs in ToolOptions.Pairs(options)
           from key in pairs.Value("--key").OrElse(defaultKey)
           select new PackOptions(
               project,
               key,
               pairs.Value("--rid").GetOrElse(defaultRid),
               pairs.Value("--out").GetOrElse(Path.Combine("artifacts", "plugins")),
               pairs.Value("--configuration").GetOrElse("Release"),
               pairs.Value("--catalog"));
}

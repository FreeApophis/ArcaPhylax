namespace ArcaPhylax.PluginPack;

internal sealed record PackOptions(string Project, string KeyPath, string RuntimeIdentifier, string OutputDirectory, string Configuration, Option<string> Catalog)
{
    /// <summary>The options, or None when the arguments are incomplete (no key) or malformed.</summary>
    public static Option<PackOptions> Parse(string project, IReadOnlyList<string> options, Option<string> defaultKey, string defaultRid)
        => from pairs in Pairs(options)
           from key in Value(pairs, "--key").OrElse(defaultKey)
           select new PackOptions(
               project,
               key,
               Value(pairs, "--rid").GetOrElse(defaultRid),
               Value(pairs, "--out").GetOrElse(Path.Combine("artifacts", "plugins")),
               Value(pairs, "--configuration").GetOrElse("Release"),
               Value(pairs, "--catalog"));

    private static Option<IReadOnlyDictionary<string, string>> Pairs(IReadOnlyList<string> options)
        => options.Count % 2 == 0 && options.Where((_, index) => index % 2 == 0).All(name => name.StartsWith("--", StringComparison.Ordinal))
            ? Option.Some<IReadOnlyDictionary<string, string>>(options
                .Chunk(2)
                .ToDictionary(pair => pair[0], pair => pair[1], StringComparer.OrdinalIgnoreCase))
            : Option<IReadOnlyDictionary<string, string>>.None;

    private static Option<string> Value(IReadOnlyDictionary<string, string> pairs, string name)
        => pairs.GetValueOrNone(name);
}

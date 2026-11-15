namespace ArcaPhylax.PluginPack;

/// <summary>Parses <c>--name value</c> pairs.</summary>
internal static class ToolOptions
{
    /// <summary>The pairs, or None when the arguments are not a list of <c>--name value</c> pairs.</summary>
    public static Option<IReadOnlyDictionary<string, string>> Pairs(IReadOnlyList<string> options)
        => options.Count % 2 == 0 && options.Where((_, index) => index % 2 == 0).All(name => name.StartsWith("--", StringComparison.Ordinal))
            ? Option.Some<IReadOnlyDictionary<string, string>>(options
                .Chunk(2)
                .ToDictionary(pair => pair[0], pair => pair[1], StringComparer.OrdinalIgnoreCase))
            : Option<IReadOnlyDictionary<string, string>>.None;

    public static Option<string> Value(this IReadOnlyDictionary<string, string> pairs, string name)
        => pairs.GetValueOrNone(name);
}

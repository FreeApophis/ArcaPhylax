using System.Collections.Immutable;

namespace ArcaPhylax.PluginPackaging;

/// <summary>An endorsed publisher: display name, signing key and the plugin id prefixes it owns.</summary>
/// <remarks>A plugin prefix <c>Contoso</c> owns <c>Contoso</c> and every <c>Contoso.*</c> id (ignoring case, like the store's folders).</remarks>
public sealed record Publisher(string Name, PublisherKey Key, ImmutableList<string> PluginPrefixes)
{
    public bool Owns(string pluginId)
        => PluginPrefixes.Any(prefix =>
            string.Equals(pluginId, prefix, StringComparison.OrdinalIgnoreCase)
            || pluginId.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase));
}

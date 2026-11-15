using System.Collections.Immutable;
using System.Text.Json;

namespace ArcaPhylax.PluginPackaging;

/// <summary>
/// The <c>catalog.json</c> the plugin browser reads: every published plugin and its packages. It only says
/// what exists and where; trust still comes from each package's signature.
/// </summary>
public sealed record PluginCatalog(int SchemaVersion, ImmutableList<CatalogPlugin> Plugins)
{
    public const string FileName = "catalog.json";

    /// <summary>The newest catalog format this code understands.</summary>
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    public static PluginCatalog Empty { get; } = new(CurrentSchemaVersion, []);

    public static Either<string, PluginCatalog> Parse(string json)
        => Deserialize(json).SelectMany(Supported);

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>This catalog with a freshly packed package added (or replacing the same version and platform).</summary>
    public PluginCatalog With(PluginManifest manifest, string url, long size, string sha256)
    {
        var package = new CatalogPackage(manifest.Version, manifest.Platform, manifest.SdkVersion, url, size, sha256);
        var entry = Plugins.FirstOrNone(plugin => plugin.Id == manifest.Id).Match(
            none: () => CatalogPlugin.Of(manifest, package),
            some: existing => existing.With(manifest, package));

        return this with
        {
            Plugins = Plugins
                .Where(plugin => plugin.Id != manifest.Id)
                .Append(entry)
                .OrderBy(plugin => plugin.Id, StringComparer.Ordinal)
                .ToImmutableList(),
        };
    }

    /// <summary>This catalog plus the plugins of <paramref name="listing"/>; a plugin id may only be listed once.</summary>
    public Either<string, PluginCatalog> Merge(PluginCatalog listing)
        => listing.Plugins
            .FirstOrNone(plugin => Plugins.Any(existing => string.Equals(existing.Id, plugin.Id, StringComparison.OrdinalIgnoreCase)))
            .Match(
                none: () => Either<string, PluginCatalog>.Right(this with
                {
                    Plugins = Plugins.AddRange(listing.Plugins).OrderBy(plugin => plugin.Id, StringComparer.Ordinal).ToImmutableList(),
                }),
                some: duplicate => Either<string, PluginCatalog>.Left($"the plugin id '{duplicate.Id}' is already listed."));

    private static Either<string, PluginCatalog> Deserialize(string json)
    {
        // JsonException is the parser's way of reporting malformed input; translate it at this boundary.
        try
        {
            return JsonSerializer.Deserialize<PluginCatalog>(json, JsonOptions) is { } catalog
                ? Either<string, PluginCatalog>.Right(catalog)
                : Either<string, PluginCatalog>.Left("The catalog is empty.");
        }
        catch (JsonException exception)
        {
            return Either<string, PluginCatalog>.Left($"The catalog is not valid: {exception.Message}");
        }
    }

    private static Either<string, PluginCatalog> Supported(PluginCatalog catalog)
        => catalog.SchemaVersion is > 0 and <= CurrentSchemaVersion
            ? Either<string, PluginCatalog>.Right(catalog)
            : Either<string, PluginCatalog>.Left($"The catalog uses format {catalog.SchemaVersion}; this ArcaPhylax reads up to {CurrentSchemaVersion}. Update ArcaPhylax.");
}

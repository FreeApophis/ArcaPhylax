using System.Text.Json;
using System.Text.RegularExpressions;

namespace ArcaPhylax.PluginPackaging;

/// <summary>The <c>plugin.json</c> at the root of every <c>.arcaplugin</c> package.</summary>
/// <param name="Id">Stable reverse-DNS style identifier, e.g. <c>ArcaPhylax.Fractal</c>.</param>
/// <param name="Version">Plugin version (<c>major.minor.patch</c>).</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">One-paragraph description for the plugin browser.</param>
/// <param name="Author">Author or publisher display name.</param>
/// <param name="Platform">Runtime identifier the payload was published for, e.g. <c>win-x64</c>.</param>
/// <param name="EntryAssembly">File name of the plugin assembly inside <c>payload/</c>.</param>
/// <param name="SdkVersion">Version of <c>ArcaPhylax.Plugin.Sdk</c> the plugin was built against.</param>
public sealed partial record PluginManifest(
    string Id,
    string Version,
    string Name,
    string Description,
    string Author,
    string Platform,
    string EntryAssembly,
    string SdkVersion)
{
    public const string FileName = "plugin.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    public static Either<PackageError, PluginManifest> Parse(string json)
        => Deserialize(json).SelectMany(Validated);

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    private static Either<PackageError, PluginManifest> Deserialize(string json)
    {
        // JsonException is the parser's way of reporting malformed input; translate it at this boundary.
        try
        {
            return JsonSerializer.Deserialize<PluginManifest>(json, JsonOptions) is { } manifest
                ? Either<PackageError, PluginManifest>.Right(manifest)
                : Either<PackageError, PluginManifest>.Left(new InvalidManifest("plugin.json is empty."));
        }
        catch (JsonException exception)
        {
            return Either<PackageError, PluginManifest>.Left(new InvalidManifest(exception.Message));
        }
    }

    private static Either<PackageError, PluginManifest> Validated(PluginManifest manifest)
        => Problems(manifest).FirstOrNone().Match(
            none: () => Either<PackageError, PluginManifest>.Right(manifest),
            some: reason => Either<PackageError, PluginManifest>.Left(new InvalidManifest(reason)));

    private static IEnumerable<string> Problems(PluginManifest manifest)
        => new (bool Valid, string Reason)[]
            {
                (IdPattern().IsMatch(manifest.Id), $"id '{manifest.Id}' must be dot-separated identifiers (letters and digits)."),
                (VersionPattern().IsMatch(manifest.Version), $"version '{manifest.Version}' must be major.minor.patch."),
                (!string.IsNullOrWhiteSpace(manifest.Name), "name must not be empty."),
                (!string.IsNullOrWhiteSpace(manifest.Author), "author must not be empty."),
                (PlatformPattern().IsMatch(manifest.Platform), $"platform '{manifest.Platform}' must be a runtime identifier such as win-x64."),
                (EntryAssemblyPattern().IsMatch(manifest.EntryAssembly), $"entryAssembly '{manifest.EntryAssembly}' must be a plain .dll file name."),
                (VersionPattern().IsMatch(manifest.SdkVersion), $"sdkVersion '{manifest.SdkVersion}' must be major.minor.patch."),
            }
            .Where(check => !check.Valid)
            .Select(check => check.Reason);

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9]*(\.[A-Za-z][A-Za-z0-9]*)*$")]
    private static partial Regex IdPattern();

    [GeneratedRegex(@"^\d+\.\d+\.\d+$")]
    private static partial Regex VersionPattern();

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex PlatformPattern();

    [GeneratedRegex(@"^[A-Za-z0-9_][A-Za-z0-9_.\-]*\.dll$")]
    private static partial Regex EntryAssemblyPattern();
}

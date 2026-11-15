using System.Text.Json;
using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.PluginPack;

/// <summary>The plugin's package metadata as declared in its .csproj (read via <c>dotnet msbuild -getProperty</c>).</summary>
internal sealed record ProjectMetadata(string PluginId, string Title, string Description, string Authors, string Version, string AssemblyName)
{
    public static Either<string, ProjectMetadata> Parse(string getPropertyJson)
    {
        using var document = JsonDocument.Parse(getPropertyJson);
        var properties = document.RootElement.GetProperty("Properties");
        string Property(string name) => properties.GetProperty(name).GetString() ?? string.Empty;

        return Property("PluginId") is { Length: > 0 } pluginId
            ? Either<string, ProjectMetadata>.Right(new ProjectMetadata(
                pluginId,
                Property("Title"),
                Property("Description"),
                Property("Authors"),
                Property("Version"),
                Property("AssemblyName")))
            : Either<string, ProjectMetadata>.Left("The project has no <PluginId>; is it a plugin project?");
    }

    public PluginManifest ToManifest(string runtimeIdentifier, string sdkVersion)
        => new(PluginId, Version, Title, Description, Authors, runtimeIdentifier, $"{AssemblyName}.dll", sdkVersion);
}

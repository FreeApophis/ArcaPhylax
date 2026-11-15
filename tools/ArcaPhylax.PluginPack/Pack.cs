using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.PluginPack;

internal static class Pack
{
    private const string SdkAssemblyName = "ArcaPhylax.Plugin.Sdk";

    public static int Run(PackOptions options)
    {
        var publishDirectory = Path.Combine(Path.GetTempPath(), "arcaphylax-pluginpack", Guid.NewGuid().ToString("N"));
        try
        {
            return Packed(options, publishDirectory).SelectMany(entry => Catalogued(options, entry)).Match(
                left: error =>
                {
                    Console.Error.WriteLine($"error: {error}");
                    return 1;
                },
                right: entry =>
                {
                    Console.WriteLine(JsonSerializer.Serialize(entry, ToolJson.Options));
                    return 0;
                });
        }
        finally
        {
            if (Directory.Exists(publishDirectory))
            {
                Directory.Delete(publishDirectory, recursive: true);
            }
        }
    }

    private static Either<string, PackedPlugin> Catalogued(PackOptions options, PackedPlugin entry)
        => options.Catalog.Match(
            none: () => Either<string, PackedPlugin>.Right(entry),
            some: catalog => CatalogFile.Add(catalog, entry, Path.Combine(options.OutputDirectory, entry.File)).Select(_ => entry));

    private static Either<string, PackedPlugin> Packed(PackOptions options, string publishDirectory)
        => from properties in ProjectProperties(options.Project)
           from published in Published(options, publishDirectory)
           from sdkVersion in SdkVersion(publishDirectory, properties.AssemblyName)
           from manifest in ValidManifest(properties, options.RuntimeIdentifier, sdkVersion)
           select Written(manifest, options, publishDirectory);

    private static Either<string, ProjectMetadata> ProjectProperties(string project)
        => Dotnet(
                "msbuild",
                project,
                "-getProperty:PluginId",
                "-getProperty:Title",
                "-getProperty:Description",
                "-getProperty:Authors",
                "-getProperty:Version",
                "-getProperty:AssemblyName")
            .SelectMany(json => ProjectMetadata.Parse(json));

    private static Either<string, Unit> Published(PackOptions options, string publishDirectory)
        => Dotnet(
                "publish",
                options.Project,
                "-c",
                options.Configuration,
                "-r",
                options.RuntimeIdentifier,
                "--self-contained",
                "false",
                "-o",
                publishDirectory)
            .Select(_ => Unit.Value);

    /// <summary>
    /// The ArcaPhylax.Plugin.Sdk version the plugin was compiled against, read from the plugin assembly's
    /// own reference table (the SDK is host-provided, so it is not in the plugin's deps.json).
    /// </summary>
    private static Either<string, string> SdkVersion(string publishDirectory, string assemblyName)
    {
        using var stream = File.OpenRead(Path.Combine(publishDirectory, $"{assemblyName}.dll"));
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();

        return metadata.AssemblyReferences
            .Select(metadata.GetAssemblyReference)
            .FirstOrNone(reference => metadata.GetString(reference.Name) == SdkAssemblyName)
            .Match(
                none: () => Either<string, string>.Left($"The plugin does not reference {SdkAssemblyName}."),
                some: reference => Either<string, string>.Right(reference.Version.ToString(3)));
    }

    private static Either<string, PluginManifest> ValidManifest(ProjectMetadata properties, string rid, string sdkVersion)
        => PluginManifest.Parse(properties.ToManifest(rid, sdkVersion).ToJson())
            .Match(
                left: error => Either<string, PluginManifest>.Left($"invalid plugin metadata: {error}"),
                right: Either<string, PluginManifest>.Right);

    private static PackedPlugin Written(PluginManifest manifest, PackOptions options, string publishDirectory)
    {
        using var key = ECDsa.Create();
        key.ImportFromPem(File.ReadAllText(options.KeyPath));

        Directory.CreateDirectory(options.OutputDirectory);
        var packagePath = Path.Combine(options.OutputDirectory, $"{manifest.Id}-{manifest.Version}-{manifest.Platform}{PluginPackage.Extension}");
        using (var output = File.Create(packagePath))
        {
            PluginPackage.Write(manifest, publishDirectory, key, output);
        }

        using var written = File.OpenRead(packagePath);
        var verified = PluginPackage.Verify(written).SelectMany(package => package.Signer == PublisherKey.Of(key)
            ? Either<PackageError, VerifiedPackage>.Right(package)
            : Either<PackageError, VerifiedPackage>.Left(new InvalidSignature()));
        written.Position = 0;

        return verified.Match(
            left: error => throw new InvalidOperationException($"The package just written does not verify: {error}"),
            right: _ => new PackedPlugin(
                manifest,
                Path.GetFileName(packagePath),
                written.Length,
                Convert.ToHexStringLower(SHA256.HashData(written))));
    }

    private static Either<string, string> Dotnet(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)!;
        var stdErr = process.StandardError.ReadToEndAsync();
        var stdOut = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return process.ExitCode == 0
            ? Either<string, string>.Right(stdOut)
            : Either<string, string>.Left($"dotnet {string.Join(' ', arguments)} failed ({process.ExitCode}):\n{stdOut}{stdErr.Result}");
    }
}

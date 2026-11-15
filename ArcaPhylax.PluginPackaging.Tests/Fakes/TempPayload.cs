using System.Security.Cryptography;

namespace ArcaPhylax.PluginPackaging.Tests.Fakes;

/// <summary>A throwaway "published plugin" folder plus helpers to package and tamper with it.</summary>
public sealed class TempPayload : IDisposable
{
    public TempPayload()
    {
        Directory = Path.Combine(Path.GetTempPath(), "arca-pkg-tests", Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Path.Combine(Directory, "runtimes", "win-x64", "native"));
        File.WriteAllText(Path.Combine(Directory, "Sample.dll"), "assembly bytes");
        File.WriteAllText(Path.Combine(Directory, "Sample.deps.json"), "{}");
        File.WriteAllText(Path.Combine(Directory, "runtimes", "win-x64", "native", "native.dll"), "native bytes");
    }

    public string Directory { get; }

    public static PluginManifest Manifest => new(
        Id: "ArcaPhylax.Sample",
        Version: "1.2.3",
        Name: "Sample",
        Description: "A sample plugin.",
        Author: "Tests",
        Platform: "win-x64",
        EntryAssembly: "Sample.dll",
        SdkVersion: "1.0.0");

    public MemoryStream Package(ECDsa key) => Package(Manifest, key);

    public MemoryStream Package(PluginManifest manifest, ECDsa key)
    {
        var stream = new MemoryStream();
        PluginPackage.Write(manifest, Directory, key, stream);
        stream.Position = 0;
        return stream;
    }

    public void Dispose()
    {
        try
        {
            System.IO.Directory.Delete(Directory, recursive: true);
        }
        catch (IOException)
        {
            // Not worth failing a test over.
        }
    }
}

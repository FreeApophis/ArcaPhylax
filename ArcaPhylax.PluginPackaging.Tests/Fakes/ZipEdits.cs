using System.IO.Compression;

namespace ArcaPhylax.PluginPackaging.Tests.Fakes;

/// <summary>Raw zip manipulation for tamper tests (bypasses the package writer on purpose).</summary>
public static class ZipEdits
{
    public static MemoryStream Replace(MemoryStream package, string entryName, string newContent)
        => Edit(package, archive =>
        {
            archive.GetEntry(entryName)!.Delete();
            WriteEntry(archive, entryName, newContent);
        });

    public static MemoryStream Add(MemoryStream package, string entryName, string content)
        => Edit(package, archive => WriteEntry(archive, entryName, content));

    public static MemoryStream Remove(MemoryStream package, string entryName)
        => Edit(package, archive => archive.GetEntry(entryName)!.Delete());

    public static string Read(MemoryStream package, string entryName)
    {
        package.Position = 0;
        using var archive = new ZipArchive(package, ZipArchiveMode.Read, leaveOpen: true);
        using var reader = new StreamReader(archive.GetEntry(entryName)!.Open());
        return reader.ReadToEnd();
    }

    public static MemoryStream ZipWith(params (string Name, string Content)[] entries)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                WriteEntry(archive, name, content);
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream Edit(MemoryStream package, Action<ZipArchive> edit)
    {
        var copy = new MemoryStream();
        package.Position = 0;
        package.CopyTo(copy);
        using (var archive = new ZipArchive(copy, ZipArchiveMode.Update, leaveOpen: true))
        {
            edit(archive);
        }

        copy.Position = 0;
        return copy;
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open());
        writer.Write(content);
    }
}

using System.Collections.Immutable;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace ArcaPhylax.PluginPackaging;

/// <summary>
/// The <c>.arcaplugin</c> layout: a zip with <c>plugin.json</c>, the published plugin under
/// <c>payload/</c>, and <c>signature.json</c> (ECDSA P-256 / SHA-256 over <see cref="ContentListing"/>).
/// </summary>
public static class PluginPackage
{
    public const string Extension = ".arcaplugin";
    public const string PayloadPrefix = "payload/";
    public const string SignatureFileName = "signature.json";

    /// <summary>Writes a signed package of <paramref name="payloadDirectory"/> to <paramref name="output"/>.</summary>
    public static void Write(PluginManifest manifest, string payloadDirectory, ECDsa signingKey, Stream output)
    {
        var manifestBytes = Encoding.UTF8.GetBytes(manifest.ToJson());
        var payload = PayloadFiles(payloadDirectory);
        var listing = ContentListing.Of(payload
            .Select(file => new ContentEntry(file.EntryName, Sha256Hex(file.FullPath)))
            .Prepend(new ContentEntry(PluginManifest.FileName, Sha256Hex(manifestBytes))));
        var signature = PackageSignature.Create(listing, signingKey);

        using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        WriteEntry(archive, PluginManifest.FileName, manifestBytes);
        foreach (var file in payload)
        {
            archive.CreateEntryFromFile(file.FullPath, file.EntryName, CompressionLevel.Optimal);
        }

        WriteEntry(archive, SignatureFileName, Encoding.UTF8.GetBytes(signature.ToJson()));
    }

    /// <summary>The verified package, or why it must not be installed.</summary>
    public static Either<PackageError, VerifiedPackage> Verify(Stream package, IReadOnlyCollection<PublisherKey> trustedKeys)
    {
        // ZipArchive reports a non-zip or corrupt archive via InvalidDataException (on open or while
        // reading an entry); translate it at this boundary.
        try
        {
            using var archive = new ZipArchive(package, ZipArchiveMode.Read, leaveOpen: true);
            return Verified(archive, trustedKeys);
        }
        catch (InvalidDataException exception)
        {
            return Either<PackageError, VerifiedPackage>.Left(new NotAPackage(exception.Message));
        }
    }

    /// <summary>
    /// Extracts the payload of a package that <see cref="Verify"/> accepted into <paramref name="targetDirectory"/>.
    /// </summary>
    internal static void ExtractPayload(Stream package, string targetDirectory)
    {
        var target = Path.GetFullPath(targetDirectory) + Path.DirectorySeparatorChar;
        using var archive = new ZipArchive(package, ZipArchiveMode.Read, leaveOpen: true);
        foreach (var entry in archive.Entries.Where(IsPayloadFile))
        {
            var destination = Path.GetFullPath(Path.Combine(target, entry.FullName[PayloadPrefix.Length..]));

            // Verify already rejects unsafe paths; this guards the file system on its own as well.
            if (!destination.StartsWith(target, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Refusing to extract '{entry.FullName}' outside '{target}'.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            entry.ExtractToFile(destination);
        }
    }

    private static bool IsPayloadFile(ZipArchiveEntry entry)
        => entry.FullName.StartsWith(PayloadPrefix, StringComparison.Ordinal) && !entry.FullName.EndsWith('/');

    private static Either<PackageError, VerifiedPackage> Verified(ZipArchive archive, IReadOnlyCollection<PublisherKey> trustedKeys)
        => from files in SafeFileEntries(archive)
           from signature in SignatureOf(archive)
           from key in TrustedKeyFor(signature, trustedKeys)
           from signed in SignatureMatches(files, signature, key)
           from manifest in ManifestOf(archive)
           from entryAssembly in RequiredEntry(files, PayloadPrefix + manifest.EntryAssembly)
           select new VerifiedPackage(manifest, key.KeyId, PayloadPaths(files));

    /// <summary>All file entries, or the first one whose path is outside the allowed layout.</summary>
    private static Either<PackageError, ImmutableList<ZipArchiveEntry>> SafeFileEntries(ZipArchive archive)
        => archive.Entries
            .Where(entry => !IsSafe(entry.FullName))
            .FirstOrNone()
            .Match(
                none: () => Either<PackageError, ImmutableList<ZipArchiveEntry>>.Right(
                    archive.Entries.Where(entry => !entry.FullName.EndsWith('/')).ToImmutableList()),
                some: entry => Either<PackageError, ImmutableList<ZipArchiveEntry>>.Left(new UnsafeEntryPath(entry.FullName)));

    private static bool IsSafe(string entryName)
        => entryName is PluginManifest.FileName or SignatureFileName
            || (entryName.StartsWith(PayloadPrefix, StringComparison.Ordinal)
                && entryName.Length > PayloadPrefix.Length
                && !entryName.Contains('\\')
                && !entryName.Contains(':')
                && entryName[PayloadPrefix.Length..].TrimEnd('/').Split('/').All(IsSafeSegment));

    private static bool IsSafeSegment(string segment)
        => segment is not ("" or "." or "..");

    private static Either<PackageError, PackageSignature> SignatureOf(ZipArchive archive)
        => Option.FromNullable(archive.GetEntry(SignatureFileName)).Match(
            none: () => Either<PackageError, PackageSignature>.Left(new MissingEntry(SignatureFileName)),
            some: entry => PackageSignature.Parse(ReadText(entry)).Match(
                none: () => Either<PackageError, PackageSignature>.Left(new InvalidSignature()),
                some: Either<PackageError, PackageSignature>.Right));

    private static Either<PackageError, PublisherKey> TrustedKeyFor(PackageSignature signature, IReadOnlyCollection<PublisherKey> trustedKeys)
        => trustedKeys.FirstOrNone(key => key.KeyId == signature.KeyId).Match(
            none: () => Either<PackageError, PublisherKey>.Left(new UntrustedKey(signature.KeyId)),
            some: Either<PackageError, PublisherKey>.Right);

    private static Either<PackageError, Unit> SignatureMatches(
        ImmutableList<ZipArchiveEntry> files,
        PackageSignature signature,
        PublisherKey key)
        => signature.IsValidFor(ListingOf(files), key)
            ? Either<PackageError, Unit>.Right(Unit.Value)
            : Either<PackageError, Unit>.Left(new InvalidSignature());

    private static Either<PackageError, PluginManifest> ManifestOf(ZipArchive archive)
        => Option.FromNullable(archive.GetEntry(PluginManifest.FileName)).Match(
            none: () => Either<PackageError, PluginManifest>.Left(new MissingEntry(PluginManifest.FileName)),
            some: entry => PluginManifest.Parse(ReadText(entry)));

    private static Either<PackageError, Unit> RequiredEntry(ImmutableList<ZipArchiveEntry> files, string entryName)
        => files.Any(entry => entry.FullName == entryName)
            ? Either<PackageError, Unit>.Right(Unit.Value)
            : Either<PackageError, Unit>.Left(new MissingEntry(entryName));

    private static string ListingOf(ImmutableList<ZipArchiveEntry> files)
        => ContentListing.Of(files
            .Where(entry => entry.FullName != SignatureFileName)
            .Select(entry => new ContentEntry(entry.FullName, Sha256Hex(entry))));

    private static ImmutableList<string> PayloadPaths(ImmutableList<ZipArchiveEntry> files)
        => files
            .Where(entry => entry.FullName.StartsWith(PayloadPrefix, StringComparison.Ordinal))
            .Select(entry => entry.FullName[PayloadPrefix.Length..])
            .ToImmutableList();

    private static ImmutableList<PayloadFile> PayloadFiles(string payloadDirectory)
        => Directory.EnumerateFiles(payloadDirectory, "*", SearchOption.AllDirectories)
            .Select(fullPath => new PayloadFile(
                fullPath,
                PayloadPrefix + Path.GetRelativePath(payloadDirectory, fullPath).Replace('\\', '/')))
            .OrderBy(file => file.EntryName, StringComparer.Ordinal)
            .ToImmutableList();

    private static string ReadText(ZipArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static void WriteEntry(ZipArchive archive, string entryName, byte[] content)
    {
        using var stream = archive.CreateEntry(entryName, CompressionLevel.Optimal).Open();
        stream.Write(content);
    }

    private static string Sha256Hex(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    private static string Sha256Hex(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    private static string Sha256Hex(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    private sealed record PayloadFile(string FullPath, string EntryName);
}

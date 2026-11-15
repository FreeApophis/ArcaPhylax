using System.Collections.Immutable;
using System.Text.Json;

namespace ArcaPhylax.PluginPackaging;

/// <summary>
/// The per-user plugin directory: <c>&lt;root&gt;/&lt;id&gt;/&lt;version&gt;/</c> holds an extracted payload plus
/// its <c>plugin.json</c>; <c>&lt;root&gt;/pending.json</c> holds changes that wait for the next start.
/// New plugins are usable immediately; updates and removals are staged, because a loaded plugin's
/// files cannot be replaced safely.
/// </summary>
public sealed class PluginStore(string root, HostEnvironment host, IReadOnlyCollection<PublisherKey> trustedKeys)
{
    private const string PendingFileName = "pending.json";
    private const string StagingDirectoryName = ".staging";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public string Root { get; } = root;

    private string PendingPath => Path.Combine(Root, PendingFileName);

    /// <summary>Verifies, checks and extracts a package. The stream must be seekable.</summary>
    public Either<InstallError, InstallOutcome> Install(Stream package)
        => from verified in Verified(package)
           from manifest in PluginCompatibility.Check(verified.Manifest, host)
           from free in VersionIsFree(manifest)
           select Extracted(package, manifest);

    /// <summary>Schedules plugin <paramref name="id"/> for removal at the next start.</summary>
    public Either<InstallError, Unit> ScheduleRemoval(string id)
        => Installed().Any(plugin => plugin.Manifest.Id == id)
            ? Either<InstallError, Unit>.Right(SavePending(Pending().Where(op => op.Id != id).Append(new Remove(id))))
            : Either<InstallError, Unit>.Left(new NotInstalled(id));

    /// <summary>The changes waiting for the next start.</summary>
    public ImmutableList<PendingOperation> Pending()
        => File.Exists(PendingPath)
            ? JsonSerializer.Deserialize<ImmutableList<PendingOperation>>(File.ReadAllText(PendingPath), JsonOptions) ?? []
            : [];

    /// <summary>Applies pending changes. Call at startup, before any installed plugin is loaded.</summary>
    public void ApplyPending()
    {
        foreach (var operation in Pending())
        {
            _ = Applied(operation);
        }

        DeleteFileIfExists(PendingPath);
        DeleteDirectoryIfExists(Path.Combine(Root, StagingDirectoryName));
    }

    /// <summary>Installed plugins (the newest version of each id).</summary>
    public ImmutableList<InstalledPlugin> Installed()
        => Directory.Exists(Root)
            ? Directory.GetDirectories(Root)
                .Where(directory => Path.GetFileName(directory) != StagingDirectoryName)
                .WhereSelect(NewestVersion)
                .ToImmutableList()
            : [];

    private static Option<InstalledPlugin> NewestVersion(string idDirectory)
        => Directory.GetDirectories(idDirectory)
            .WhereSelect(InstalledAt)
            .OrderByDescending(plugin => Version.Parse(plugin.Manifest.Version))
            .FirstOrNone();

    private static Option<InstalledPlugin> InstalledAt(string versionDirectory)
    {
        var manifestPath = Path.Combine(versionDirectory, PluginManifest.FileName);
        return File.Exists(manifestPath)
            ? PluginManifest.Parse(File.ReadAllText(manifestPath)).Match(
                left: _ => Option<InstalledPlugin>.None,
                right: manifest => Option.Some(new InstalledPlugin(manifest, versionDirectory)))
            : Option<InstalledPlugin>.None;
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static Unit DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        return Unit.Value;
    }

    private Either<InstallError, VerifiedPackage> Verified(Stream package)
        => PluginPackage.Verify(package, trustedKeys).Match(
            left: error => Either<InstallError, VerifiedPackage>.Left(new InvalidPackage(error)),
            right: Either<InstallError, VerifiedPackage>.Right);

    private Either<InstallError, Unit> VersionIsFree(PluginManifest manifest)
        => Directory.Exists(VersionDirectory(manifest.Id, manifest.Version))
            ? Either<InstallError, Unit>.Left(new AlreadyInstalled(manifest.Id, manifest.Version))
            : Either<InstallError, Unit>.Right(Unit.Value);

    private InstallOutcome Extracted(Stream package, PluginManifest manifest)
    {
        var replacesInstalledVersion = VersionDirectories(manifest.Id).Any();

        // Extract into a staging folder on the same volume, then move into place in one step, so a
        // half-extracted plugin never appears under its id.
        var staging = Path.Combine(Root, StagingDirectoryName, Guid.NewGuid().ToString("N"));
        package.Position = 0;
        PluginPackage.ExtractPayload(package, staging);
        File.WriteAllText(Path.Combine(staging, PluginManifest.FileName), manifest.ToJson());

        var target = VersionDirectory(manifest.Id, manifest.Version);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        Directory.Move(staging, target);

        var plugin = new InstalledPlugin(manifest, target);
        return replacesInstalledVersion ? Staged(plugin) : new InstalledNow(plugin);
    }

    private UpdateStaged Staged(InstalledPlugin plugin)
    {
        var id = plugin.Manifest.Id;
        SavePending(Pending().Where(op => op.Id != id).Append(new Activate(id, plugin.Manifest.Version)));
        return new UpdateStaged(plugin);
    }

    private Unit SavePending(IEnumerable<PendingOperation> operations)
    {
        Directory.CreateDirectory(Root);
        File.WriteAllText(PendingPath, JsonSerializer.Serialize(operations.ToImmutableList(), JsonOptions));
        return Unit.Value;
    }

    private Unit Applied(PendingOperation operation) => operation switch
    {
        Activate activate => VersionDirectories(activate.Id)
            .Where(directory => Path.GetFileName(directory) != activate.Version)
            .Aggregate(Unit.Value, (_, directory) => DeleteDirectoryIfExists(directory)),
        Remove remove => DeleteDirectoryIfExists(IdDirectory(remove.Id)),
    };

    private string IdDirectory(string id) => Path.Combine(Root, id);

    private string VersionDirectory(string id, string version) => Path.Combine(IdDirectory(id), version);

    private IEnumerable<string> VersionDirectories(string id)
        => Directory.Exists(IdDirectory(id)) ? Directory.GetDirectories(IdDirectory(id)) : [];
}

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using ArcaPhylax.Abstractions;
using ArcaPhylax.Data;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArcaPhylax.ViewModels;

public sealed partial class DocumentsViewModel : ViewModelBase, ITabContentViewModel
{
    private readonly IDroppedItemsRepository _droppedRepo;
    private readonly IIndexingQueue _queue;

    public DocumentsViewModel(IDroppedItemsRepository droppedRepo, IIndexingQueue queue)
    {
        _droppedRepo = droppedRepo;
        _queue = queue;

        RefreshRecent();
        Status = "Ready";
    }

    public ObservableCollection<DroppedItemRowVm> Recent { get; } = new();

    [ObservableProperty]
    public partial string? Status { get; set; }

    public string Title => "Documents";

    public string Icon => NavigationIcons.Documents;

    public int NavigationWeight => 20;

    public async Task OnDroppedAsync(IReadOnlyList<IStorageFile> files, IReadOnlyList<IStorageFolder> folders)
    {
        Status = "Importing…";

        var folderJobs = folders.WhereSelect(FolderJob).ToImmutableList();
        var fileJobs = await FileJobsAsync(files);
        var jobs = folderJobs.AddRange(fileJobs);

        foreach (var job in jobs)
        {
            _droppedRepo.Add(job.Name, job.Source);
            _queue.Enqueue(job);
        }

        RefreshRecent();
        Status = QueuedStatus(jobs.Count, skippedFolders: folders.Count - folderJobs.Count);
    }

    public void RefreshRecent()
    {
        Recent.Clear();

        foreach (var it in _droppedRepo.Latest(100))
        {
            Recent.Add(DroppedItemRowVm.Of(it));
        }
    }

    /// <summary>A job for the folder, or None when it has no local path (nothing to index in place).</summary>
    private static Option<IndexJob> FolderJob(IStorageFolder folder)
        => LocalPath(folder)
            .Select(path => new IndexJob(folder.Name, new LinkedFolder(path)));

    private static async Task<ImmutableList<IndexJob>> FileJobsAsync(IReadOnlyList<IStorageFile> files)
    {
        // Sequential on purpose: cached copies of same-named files target the same cache path.
        var jobs = ImmutableList.CreateBuilder<IndexJob>();
        foreach (var file in files)
        {
            jobs.Add(new IndexJob(file.Name, await FileSourceAsync(file)));
        }

        return jobs.ToImmutable();
    }

    private static Task<DropSource> FileSourceAsync(IStorageFile file)
        => LocalPath(file).Match<Task<DropSource>>(
            none: async () => new CachedFile(await ImportToLocalCacheAsync(file)),
            some: path => Task.FromResult<DropSource>(new LinkedFile(path)));

    private static Option<string> LocalPath(IStorageItem item)
        => Option.FromNullable(item.Path?.LocalPath)
            .Where(path => !string.IsNullOrWhiteSpace(path));

    private static string QueuedStatus(int queued, int skippedFolders)
        => skippedFolders == 0
            ? $"Queued {queued} item(s) for indexing."
            : $"Queued {queued} item(s) for indexing; skipped {skippedFolders} folder(s) without a local path.";

    private static async Task<string> ImportToLocalCacheAsync(IStorageFile file)
    {
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Dropped");
        Directory.CreateDirectory(baseDir);

        var safeName = string.Join("_", file.Name.Split(Path.GetInvalidFileNameChars()));
        var targetPath = Path.Combine(baseDir, safeName);

        await using var src = await file.OpenReadAsync();
        await using var dst = File.Create(targetPath);
        await src.CopyToAsync(dst);

        return targetPath;
    }
}

using System.Collections.ObjectModel;
using ArcaPhylax.Abstractions;
using ArcaPhylax.Data;
using Avalonia.Platform.Storage;

namespace ArcaPhylax.ViewModels;

public sealed class DocumentsViewModel : NotifyBase, ITabContentViewModel
{
    private readonly IDroppedItemsRepository _droppedRepo;
    private readonly IIndexingQueue _queue;

    public ObservableCollection<DroppedItemRowVm> Recent { get; } = new();

    private string? _status;
    public string? Status { get => _status; set => Set(ref _status, value); }

    public string Title => "Documents";
    public string Icon => "📄";


    public DocumentsViewModel(IDroppedItemsRepository droppedRepo, IIndexingQueue queue)
    {
        _droppedRepo = droppedRepo;
        _queue = queue;

        RefreshRecent();
        Status = "Ready";
    }

    public async Task OnDroppedAsync(IReadOnlyList<IStorageFile> files, IReadOnlyList<IStorageFolder> folders)
    {
        Status = "Importing…";

        foreach (var folder in folders)
        {
            var p = folder.Path?.LocalPath;
            _droppedRepo.Add("Folder", folder.Name, originalPath: p, storedPath: null);
            _queue.Enqueue(new IndexJob("Folder", folder.Name, p, null));
        }

        foreach (var file in files)
        {
            var originalPath = file.Path?.LocalPath;

            if (!string.IsNullOrWhiteSpace(originalPath))
            {
                _droppedRepo.Add("File", file.Name, originalPath, storedPath: null);
                _queue.Enqueue(new IndexJob("File", file.Name, originalPath, null));
            }
            else
            {
                var storedPath = await ImportToLocalCacheAsync(file);
                _droppedRepo.Add("File", file.Name, originalPath: null, storedPath: storedPath);
                _queue.Enqueue(new IndexJob("File", file.Name, null, storedPath));
            }
        }

        RefreshRecent();
        Status = $"Queued {files.Count + folders.Count} item(s) for indexing.";
    }

    public void RefreshRecent()
    {
        Recent.Clear();

        foreach (var it in _droppedRepo.Latest(100))
        {
            Recent.Add(ToDroppedItemRowVm(it));
        }
    }

    private static DroppedItemRowVm ToDroppedItemRowVm(DroppedItem it)
    {
        return new DroppedItemRowVm(it.Id, it.Kind, it.Name, it.OriginalPath, it.StoredPath, it.CreatedUtc);
    }

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

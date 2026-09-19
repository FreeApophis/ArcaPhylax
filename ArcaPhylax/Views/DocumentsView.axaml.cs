using ArcaPhylax.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace ArcaPhylax.Views;

public partial class DocumentsView : UserControl
{
    public DocumentsView()
    {
        InitializeComponent();

        // Enable dropping
        DragDrop.SetAllowDrop(DropZone, true);

        // Hook routed events (works reliably in Avalonia 11)
        DropZone.AddHandler(DragDrop.DragOverEvent, DropZone_DragOver, RoutingStrategies.Bubble);
        DropZone.AddHandler(DragDrop.DragLeaveEvent, DropZone_DragLeave, RoutingStrategies.Bubble);
        DropZone.AddHandler(DragDrop.DropEvent, DropZone_Drop, RoutingStrategies.Bubble);
    }

    private void DropZone_DragOver(object? sender, DragEventArgs dragtEvent)
    {
        dragtEvent.DragEffects = DragDropEffects.None;

        if (dragtEvent.DataTransfer.TryGetFiles() is { })
        {
            dragtEvent.DragEffects = DragDropEffects.Copy;
            SetHighlight(true);
        }
    }

    private void DropZone_DragLeave(object? sender, DragEventArgs dragEventArgs)
        => SetHighlight(false);

    private async void DropZone_Drop(object? sender, DragEventArgs dragEventArgs)
    {
        SetHighlight(false);

        var raw = dragEventArgs.DataTransfer.TryGetFiles();
        if (raw is null)
        {
            return;
        }

        var files = raw.OfType<IStorageFile>().ToList();
        var folders = raw.OfType<IStorageFolder>().ToList();

        if (DataContext is DocumentsViewModel vm)
        {
            await vm.OnDroppedAsync(files, folders);
        }
    }

    // The drop-active look is a style class, so its colours stay on the tokens.
    private void SetHighlight(bool on)
        => DropZone.Classes.Set("dropActive", on);
}

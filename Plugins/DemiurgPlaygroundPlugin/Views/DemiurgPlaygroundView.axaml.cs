using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Threading;
using DemiurgPlaygroundPlugin.ViewModels;

namespace DemiurgPlaygroundPlugin.Views;

public partial class DemiurgPlaygroundView : UserControl
{
    public DemiurgPlaygroundView()
    {
        InitializeComponent();
    }

    // Keep the newest line in view as the transcript grows and as tokens stream in.
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is DemiurgPlaygroundViewModel vm)
        {
            vm.Messages.CollectionChanged += OnMessagesChanged;
        }
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        Dispatcher.UIThread.Post(() => this.FindControl<ScrollViewer>("Transcript")?.ScrollToEnd());
}

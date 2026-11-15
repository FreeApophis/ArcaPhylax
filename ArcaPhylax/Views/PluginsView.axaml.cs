using ArcaPhylax.ViewModels;
using Avalonia;
using Avalonia.Controls;

namespace ArcaPhylax.Views;

public partial class PluginsView : UserControl
{
    public PluginsView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Give the view-model a TopLevel so its file picker has a parent window.
        if (DataContext is PluginsViewModel vm)
        {
            vm.HostTopLevel = TopLevel.GetTopLevel(this);
        }
    }
}

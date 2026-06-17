using ArcaPhylax.ViewModels;
using Avalonia.Controls;

namespace ArcaPhylax.Views;

public partial class ScanWorkflowView : UserControl
{
    public ScanWorkflowView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Give the view-model a TopLevel so its save/open file pickers have a parent window.
        if (DataContext is ScanWorkflowViewModel vm)
        {
            vm.HostTopLevel = TopLevel.GetTopLevel(this);
        }
    }
}

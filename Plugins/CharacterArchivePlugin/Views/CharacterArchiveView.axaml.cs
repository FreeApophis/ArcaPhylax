using Avalonia.Controls;
using CharacterArchivePlugin.ViewModels;

namespace CharacterArchivePlugin.Views;

public partial class CharacterArchiveView : UserControl
{
    public CharacterArchiveView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Give the view-model a TopLevel so its download save picker has a parent window, and load
        // the tag picker on first show.
        if (DataContext is CharacterArchiveViewModel vm)
        {
            vm.HostTopLevel = TopLevel.GetTopLevel(this);
            _ = vm.InitializeAsync();
        }
    }
}

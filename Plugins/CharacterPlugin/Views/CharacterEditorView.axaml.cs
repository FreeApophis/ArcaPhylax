using Avalonia.Controls;
using CharacterPlugin.ViewModels;

namespace CharacterPlugin.Views;

public partial class CharacterEditorView : UserControl
{
    public CharacterEditorView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Load the card list on first show.
        if (DataContext is CharacterEditorViewModel vm)
        {
            _ = vm.InitializeAsync();
        }
    }
}

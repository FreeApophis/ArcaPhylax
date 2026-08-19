using Avalonia.Controls;
using CharacterPlugin.ViewModels;

namespace CharacterPlugin.Views;

public partial class CharacterEditorConfigurationView : UserControl
{
    public CharacterEditorConfigurationView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Give the view-model a TopLevel so its folder picker has a parent window.
        if (DataContext is CharacterEditorConfigurationViewModel vm)
        {
            vm.HostTopLevel = TopLevel.GetTopLevel(this);
        }
    }
}

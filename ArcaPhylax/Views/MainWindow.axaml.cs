using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Autofac;
using ArcaPhylax.ViewModels;

namespace ArcaPhylax.Views;

public partial class MainWindow : Window
{
    public MainWindow(IComponentContext ctx)
    {
        InitializeComponent();
        DataContext = ctx.Resolve<MainWindowViewModel>();

        // The accent pill is a single Border that slides to overlay the selected
        // nav item. Selection changes and layout passes (initial container
        // realisation, window resizing) both require repositioning it.
        NavList.SelectionChanged += (_, _) => MoveHighlight();
        NavList.LayoutUpdated += (_, _) => MoveHighlight();
    }

    private void MoveHighlight()
    {
        if (NavList.SelectedItem is null)
        {
            NavHighlight.IsVisible = false;
            return;
        }

        // Container may not be realised yet; leave the pill as-is and a later
        // LayoutUpdated pass will reposition it.
        if (NavList.ContainerFromItem(NavList.SelectedItem) is not Control container
            || container.Bounds.Height == 0
            || container.TranslatePoint(default, NavArea) is not { } top)
        {
            return;
        }

        var transform = (TranslateTransform)NavHighlight.RenderTransform!;

        // Guard each assignment so LayoutUpdated does not thrash layout in a loop.
        if (NavHighlight.Height != container.Bounds.Height) NavHighlight.Height = container.Bounds.Height;
        if (transform.Y != top.Y) transform.Y = top.Y;
        if (!NavHighlight.IsVisible) NavHighlight.IsVisible = true;
    }
}

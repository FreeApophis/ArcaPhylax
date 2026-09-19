using ArcaPhylax.ViewModels;
using Autofac;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace ArcaPhylax.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // The accent pill is a single Border that slides to overlay the selected nav item,
        // wherever it lives. Selection changes and layout passes (initial container realisation,
        // window resizing) both require repositioning it.
        ListBox[] lists = [NavList, NavListBottom];

        foreach (var list in lists)
        {
            list.SelectionChanged += (_, _) => MoveHighlight();
            list.LayoutUpdated += (_, _) => MoveHighlight();
        }
    }

    public MainWindow(IComponentContext ctx)
        : this()
        => DataContext = ctx.Resolve<MainWindowViewModel>();

    // The window draws its own chrome (ExtendClientAreaToDecorationsHint), so the drag strip and
    // the pane header stand in for the system title bar: press-and-drag moves, double-click
    // toggles maximise.
    private void OnChromePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            WindowState = WindowState is WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        else
        {
            BeginMoveDrag(e);
        }
    }

    private void MoveHighlight()
    {
        if (SelectedContainer() is not { } container)
        {
            // Nothing selected -> hide. If something is selected but its container is not realised
            // yet, leave the pill as-is; a later LayoutUpdated pass will reposition it.
            if (NavList.SelectedItem is null && NavListBottom.SelectedItem is null)
            {
                NavHighlight.IsVisible = false;
            }

            return;
        }

        if (container.Bounds.Height == 0 || container.TranslatePoint(default, NavArea) is not { } top)
        {
            return;
        }

        var transform = (TranslateTransform)NavHighlight.RenderTransform!;

        // Guard each assignment so LayoutUpdated does not thrash layout in a loop.
        if (NavHighlight.Height != container.Bounds.Height)
        {
            NavHighlight.Height = container.Bounds.Height;
        }

        if (transform.Y != top.Y)
        {
            transform.Y = top.Y;
        }

        if (!NavHighlight.IsVisible)
        {
            NavHighlight.IsVisible = true;
        }
    }

    private Control? SelectedContainer()
        => SelectedContainer(NavList) ?? SelectedContainer(NavListBottom);

    private static Control? SelectedContainer(ListBox list)
        => list.SelectedItem is { } item && list.ContainerFromItem(item) is Control container
            ? container
            : null;
}

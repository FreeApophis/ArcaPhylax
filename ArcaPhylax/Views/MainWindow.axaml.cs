using Avalonia.Controls;
using Autofac;
using ArcaPhylax.ViewModels;

namespace ArcaPhylax.Views;

public partial class MainWindow : Window
{
    public MainWindow(IComponentContext ctx)
    {
        InitializeComponent();
        DataContext = ctx.Resolve<MainWindowViewModel>();
    }
}

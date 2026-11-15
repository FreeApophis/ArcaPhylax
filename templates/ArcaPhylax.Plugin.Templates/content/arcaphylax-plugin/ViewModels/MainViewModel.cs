using ArcaPhylax.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Company.HelloPlugin.ViewModels;

/// <summary>The plugin's page.</summary>
public sealed partial class MainViewModel : ObservableObject, ITabContentViewModel
{
    /// <summary>Shown in the navigation.</summary>
    public string Title => "HELLO_TITLE";

    /// <summary>The navigation icon: SVG path data on a 24×24 canvas.</summary>
    public string Icon => "M12,2 L22,12 L12,22 L2,12 Z";

    /// <summary>Position in the navigation; lower sits higher.</summary>
    public int NavigationWeight => 100;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Message))]
    public partial int Clicks { get; set; }

    public string Message => Clicks == 0 ? "Hello from your plugin!" : $"You clicked {Clicks} time(s).";

    [RelayCommand]
    private void Click() => Clicks++;
}

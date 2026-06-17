using ArcaPhylax.Abstractions;

namespace FractalPlugin.ViewModels;

public sealed class FractalViewModel : ViewModelBase, ITabContentViewModel
{
    public string Title => "Fractal";
    public string Icon => FractalIcons.Fractal;

    public int MaxIterations => 100;
}

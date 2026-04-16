using ArcaPhylax.Abstractions;

namespace FractalPlugin.ViewModels;

public sealed class FractalViewModel : NotifyBase, ITabContentViewModel
{
    public string Title => "Fractal";
    public string Icon => "◈";

    public int MaxIterations => 100;
}

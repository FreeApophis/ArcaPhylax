using ArcaPhylax.Abstractions;

namespace ArcaPhylax.ViewModels;

public sealed class FractalViewModel : NotifyBase, ITabContentViewModel
{
    public string Title => "Fractal";
    public string Icon => "◈";

    public int MaxIterations => 100;
}

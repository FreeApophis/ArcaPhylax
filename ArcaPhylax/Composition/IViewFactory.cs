using Avalonia.Controls;

namespace ArcaPhylax.Composition;

public interface IViewFactory
{
    Control Create(object viewModel);
}

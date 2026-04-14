using Autofac;
using Avalonia.Controls;

namespace ArcaPhylax.Composition;

public sealed class ViewFactory(IComponentContext ctx) : IViewFactory
{
    private const string ViewModelPostfix = "ViewModel";
    private const string ViewPostfix = "View";

    public Control Create(object viewModel)
    {
        var viewModelType = viewModel.GetType();
        var viewTypeName = ToViewTypeName(viewModelType.FullName);
        var viewType = viewModelType.Assembly.GetType(viewTypeName)
            ?? throw new InvalidOperationException($"No view found for {viewModelType.Name} (expected {viewTypeName}).");

        var view = (Control)ctx.Resolve(viewType);
        view.DataContext = viewModel;
        return view;
    }

    private static string ToViewTypeName(string? viewModelTypeNameOption)
        => viewModelTypeNameOption is { } viewModelTypeName
            ? viewModelTypeName.Replace(ViewModelPostfix, ViewPostfix)
            : string.Empty;
}

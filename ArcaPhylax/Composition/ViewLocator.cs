using ArcaPhylax.Abstractions;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace ArcaPhylax.Composition;

/// <summary>
/// Application-wide <see cref="IDataTemplate"/> that materialises a configuration section's view
/// from its view model via the <c>ViewModel</c>-&gt;<c>View</c> naming convention. Keeping views out
/// of the view models lets Avalonia build a fresh view per presentation, so a section can be shown,
/// hidden, and shown again without tripping "already has a visual parent".
/// </summary>
public sealed class ViewLocator(IViewFactory views) : IDataTemplate
{
    public Control? Build(object? param) => param is null ? null : views.Create(param);

    public bool Match(object? data) => data is IConfigurationSection;
}

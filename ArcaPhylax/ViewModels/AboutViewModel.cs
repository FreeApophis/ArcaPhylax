using System.Reflection;
using ArcaPhylax.Abstractions;

namespace ArcaPhylax.ViewModels;

/// <summary>The About page: what ArcaPhylax is and what it is built on.</summary>
public sealed class AboutViewModel : ViewModelBase, ITabContentViewModel
{
    public string AppName => "ArcaPhylax";

    public string Tagline => "Find similar images and duplicates.";

    public string Version =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0";

    public string Description =>
        "TODO";

    public IReadOnlyList<AboutEntry> TechStack { get; } =
    [
        new("Author", "Thomas Bruderer"),
        new("Framework", ".NET 10"),
        new("UI", "Avalonia 12 · CommunityToolkit.Mvvm"),
    ];

    public string Title => "About";

    public string Icon => NavigationIcons.About;
}

/// <summary>A single labelled fact shown on the About page.</summary>
public sealed record AboutEntry(string Label, string Value);

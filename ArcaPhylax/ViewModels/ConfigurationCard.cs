using Avalonia.Controls;

namespace ArcaPhylax.ViewModels;

/// <summary>A configuration section contributed by the host or a plugin, paired with its view.</summary>
public sealed record ConfigurationCard(string Title, Control View);
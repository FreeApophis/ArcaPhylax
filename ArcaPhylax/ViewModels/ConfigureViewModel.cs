using System.ComponentModel;
using ArcaPhylax.Abstractions;
using ArcaPhylax.Composition;
using ArcaPhylax.PluginSystem;
using ArcaPhylax.Services;
using Avalonia;
using Avalonia.Styling;

namespace ArcaPhylax.ViewModels;

/// <summary>The Configure page: theme plus configuration sections contributed by host/plugins.</summary>
public partial class ConfigureViewModel : ViewModelBase, ITabContentViewModel
{
    public ConfigureViewModel(AppSettings settings, PluginManager pluginManager, IViewFactory views)
    {
        Settings = settings;

        Sections = pluginManager.AllConfigurationSections
            .OrderBy(section => section.Order)
            .Select(section => new ConfigurationCard(section.Title, views.Create(section)))
            .ToList();

        // Apply the chosen theme immediately and whenever it changes.
        Settings.PropertyChanged += OnSettingsChanged;
        ApplyTheme(Settings.Theme);
    }

    public AppSettings Settings { get; }

    /// <summary>Configuration sections rendered below the built-in Appearance card.</summary>
    public IReadOnlyList<ConfigurationCard> Sections { get; }

    public AppTheme[] Themes { get; } = Enum.GetValues<AppTheme>();

    public string Title => "Configuration";

    public string Icon => NavigationIcons.Configure;

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppSettings.Theme))
        {
            ApplyTheme(Settings.Theme);
        }
    }

    private static void ApplyTheme(AppTheme theme)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = theme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}

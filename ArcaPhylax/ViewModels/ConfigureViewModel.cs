using System.ComponentModel;
using ArcaPhylax.Abstractions;
using ArcaPhylax.Services;
using Avalonia;
using Avalonia.Styling;

namespace ArcaPhylax.ViewModels;

/// <summary>The Configure page: theme, default search settings, and index maintenance.</summary>
public partial class ConfigureViewModel : ViewModelBase, ITabContentViewModel
{

    public ConfigureViewModel(AppSettings settings)
    {
        Settings = settings;

        // Apply the chosen theme immediately and whenever it changes.
        Settings.PropertyChanged += OnSettingsChanged;
        ApplyTheme(Settings.Theme);
    }

    public AppSettings Settings { get; }

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

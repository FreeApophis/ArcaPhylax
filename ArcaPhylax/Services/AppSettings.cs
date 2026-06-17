using CommunityToolkit.Mvvm.ComponentModel;

namespace ArcaPhylax.Services
{

    /// <summary>
    /// Session-wide settings shared between pages. A single instance is created by the shell and
    /// handed to every page view-model, so a change made on Configure is seen instantly on Index/Search.
    /// </summary>
    public partial class AppSettings : ObservableObject
    {
        [ObservableProperty]
        private AppTheme _theme = AppTheme.System;
    }
}

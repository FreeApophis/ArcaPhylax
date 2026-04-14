using ArcaPhylax.Abstractions;

namespace ArcaPhylax.ViewModels;
public sealed class SettingsViewModel : NotifyBase, ITabContentViewModel
{
    public string Title => "Settings";
    public string Icon => "⚙";
}

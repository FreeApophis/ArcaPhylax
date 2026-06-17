using ArcaPhylax.Abstractions;
using ArcaPhylax.Composition;
using ArcaPhylax.Data;
using ArcaPhylax.PluginSystem;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ArcaPhylax.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IViewFactory _views;

    [ObservableProperty]
    public partial bool IsPaneOpen { get; set; }

    private Control? _currentContent;
    public Control? CurrentContent { get => _currentContent; private set => SetProperty(ref _currentContent, value); }

    public ObservableCollection<TabButtonViewModel> Tabs { get; }

    public TabButtonViewModel? SelectedTab
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value))
            {
                CurrentContent = value is null ? null : _views.Create(value.Tab);
            }
        }
    }



    public MainWindowViewModel(
        IViewFactory views,
        PluginManager pluginManager)
    {
        _views = views;


        Tabs = new ObservableCollection<TabButtonViewModel>(
            pluginManager.AllTabs.Select(tab => new TabButtonViewModel(tab)));

        SelectedTab = Tabs.FirstOrDefault();
    }


    [RelayCommand]
    private void TogglePane() => IsPaneOpen = !IsPaneOpen;
}

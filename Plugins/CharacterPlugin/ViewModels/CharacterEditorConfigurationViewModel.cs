using ArcaPhylax.Abstractions;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CharacterPlugin.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CharacterPlugin.ViewModels;

/// <summary>
/// Configure-page card for the Character Editor: the folder its cards are read from and saved back to.
/// Persists to <see cref="CharacterEditorSettingsStore"/>.
/// </summary>
public sealed partial class CharacterEditorConfigurationViewModel : ObservableObject, IConfigurationSection
{
    private readonly CharacterEditorSettingsStore _store;

    public CharacterEditorConfigurationViewModel(CharacterEditorSettings settings, CharacterEditorSettingsStore store)
    {
        Settings = settings;
        _store = store;

        _store.Load(settings);
    }

    public string Title => "Character Editor";

    public int Order => 91;

    public CharacterEditorSettings Settings { get; }

    /// <summary>Set by the view so the folder picker has a parent window.</summary>
    public TopLevel? HostTopLevel { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = "Choose the folder your character cards live in.";

    [RelayCommand]
    private void Save()
    {
        _store.Save(Settings);
        Status = "Saved.";
    }

    [RelayCommand]
    private async Task Browse()
    {
        if (HostTopLevel?.StorageProvider is not { } storage)
        {
            Status = "No storage provider available.";
            return;
        }

        var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select cards folder",
            AllowMultiple = false,
        });

        if (folders is [{ } folder, ..])
        {
            Settings.CardsDirectory = folder.Path.LocalPath;
            _store.Save(Settings);
            Status = "Saved.";
        }
    }
}

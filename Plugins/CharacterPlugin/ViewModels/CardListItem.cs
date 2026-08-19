using CommunityToolkit.Mvvm.ComponentModel;

namespace CharacterPlugin.ViewModels;

/// <summary>
/// One entry in the editor's card list: a TavernCard PNG on disk. <see cref="Name"/> is observable so
/// the list reflects a rename the moment an edit is saved.
/// </summary>
public sealed partial class CardListItem(string path, string fileName, string name) : ObservableObject
{
    public string Path { get; } = path;

    public string FileName { get; } = fileName;

    [ObservableProperty]
    public partial string Name { get; set; } = name;

    /// <summary>Card name when the card carries one, otherwise the file name.</summary>
    public string Display => string.IsNullOrWhiteSpace(Name) ? FileName : Name;

    partial void OnNameChanged(string value) => OnPropertyChanged(nameof(Display));
}

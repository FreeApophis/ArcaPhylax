using ArcaPhylax.Abstractions;
using CharacterArchivePlugin.Configuration;
using CharacterArchivePlugin.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CharacterArchivePlugin.ViewModels;

/// <summary>
/// Configure-page card for the Character Archive: the PostgreSQL connection string and the root of
/// the sharded image tree, with a connection test. Persists to <see cref="CharacterArchiveSettingsStore"/>.
/// </summary>
public sealed partial class CharacterArchiveConfigurationViewModel : ObservableObject, IConfigurationSection
{
    private readonly CharacterArchiveSettingsStore _store;
    private readonly INpgsqlConnectionFactory _connectionFactory;
    private readonly SearchIndexBuilder _searchIndex;

    public CharacterArchiveConfigurationViewModel(
        CharacterArchiveSettings settings,
        CharacterArchiveSettingsStore store,
        INpgsqlConnectionFactory connectionFactory,
        SearchIndexBuilder searchIndex)
    {
        Settings = settings;
        _store = store;
        _connectionFactory = connectionFactory;
        _searchIndex = searchIndex;

        _store.Load(settings);
    }

    public string Title => "Character Archive";
    public int Order => 90;

    public CharacterArchiveSettings Settings { get; }

    [ObservableProperty]
    public partial string Status { get; set; } = "Enter a connection string and test it.";

    [RelayCommand]
    private void Save()
    {
        _store.Save(Settings);
        Status = "Saved.";
    }

    [RelayCommand]
    private async Task TestConnection()
    {
        Status = "Connecting…";

        try
        {
            await Task.Run(() =>
            {
                using var connection = _connectionFactory.Create();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                command.ExecuteScalar();
            });

            _store.Save(Settings);
            Status = "Connection succeeded — settings saved.";
        }
        catch (Exception ex)
        {
            Status = $"Connection failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RebuildSearchIndex()
    {
        Status = "Rebuilding search index… (this can take a minute)";

        try
        {
            _store.Save(Settings);
            await _searchIndex.RebuildAsync();
            Status = "Search index rebuilt.";
        }
        catch (Exception ex)
        {
            Status = $"Search index rebuild failed: {ex.Message}";
        }
    }
}

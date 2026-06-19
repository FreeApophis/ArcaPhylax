using ArcaPhylax.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DemiurgPlaygroundPlugin.Configuration;
using DemiurgPlaygroundPlugin.Services;

namespace DemiurgPlaygroundPlugin.ViewModels;

/// <summary>
/// Configure-page card for the Demiurg Playground: the local GGUF model path and inference
/// parameters, with a Load Model action. Persists to <see cref="DemiurgSettingsStore"/>.
/// </summary>
public sealed partial class DemiurgConfigurationViewModel : ObservableObject, IConfigurationSection
{
    private readonly DemiurgSettingsStore _store;
    private readonly ILlamaChatService _chat;

    public DemiurgConfigurationViewModel(DemiurgSettings settings, DemiurgSettingsStore store, ILlamaChatService chat)
    {
        Settings = settings;
        _store = store;
        _chat = chat;

        _store.Load(settings);
    }

    public string Title => "Demiurg Playground";

    public int Order => 95;

    public DemiurgSettings Settings { get; }

    [ObservableProperty]
    public partial string Status { get; set; } = "Select a local .gguf model file and load it.";

    [RelayCommand]
    private void Save()
    {
        _store.Save(Settings);
        Status = "Saved.";
    }

    [RelayCommand]
    private async Task LoadModel()
    {
        _store.Save(Settings);
        Status = "Loading model… (this can take a while on first load)";

        try
        {
            await _chat.LoadModelAsync(Settings, CancellationToken.None);

            var device = _chat.UsesGpu
                ? "GPU (CUDA)"
                : "CPU (set the CUDA runtime folder below to enable GPU)";
            var template = _chat.UsesEmbeddedTemplate
                ? string.Empty
                : " No embedded chat template — using a generic ChatML format; an instruct/chat model works best.";
            Status = $"Model loaded on {device}.{template}";
        }
        catch (Exception ex)
        {
            Status = $"Load failed: {ex.Message}";
        }
    }
}

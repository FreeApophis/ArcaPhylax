using System.Text.Json;

namespace DemiurgPlaygroundPlugin.Configuration;

/// <summary>
/// Loads and saves <see cref="DemiurgSettings"/> to a small JSON file next to the app. The host has
/// no settings persistence yet, so the plugin owns its own (mirrors CharacterArchive).
/// </summary>
public sealed class DemiurgSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;

    public DemiurgSettingsStore()
        : this(Path.Combine(AppContext.BaseDirectory, "demiurg-playground.settings.json"))
    {
    }

    /// <summary>Test seam: persist to an explicit path.</summary>
    public DemiurgSettingsStore(string path) => _path = path;

    public void Load(DemiurgSettings settings)
    {
        if (!File.Exists(_path))
        {
            return;
        }

        var persisted = JsonSerializer.Deserialize<Persisted>(File.ReadAllText(_path));
        if (persisted is null)
        {
            return;
        }

        settings.ModelPath = persisted.ModelPath;
        settings.CudaRuntimePath = persisted.CudaRuntimePath ?? string.Empty;
        settings.ContextSize = persisted.ContextSize;
        settings.GpuLayerCount = persisted.GpuLayerCount;
        settings.Temperature = persisted.Temperature;
        settings.MaxTokens = persisted.MaxTokens;
    }

    public void Save(DemiurgSettings settings) =>
        File.WriteAllText(
            _path,
            JsonSerializer.Serialize(
                new Persisted(settings.ModelPath, settings.CudaRuntimePath, settings.ContextSize, settings.GpuLayerCount, settings.Temperature, settings.MaxTokens),
                JsonOptions));

    private sealed record Persisted(
        string ModelPath,
        string CudaRuntimePath,
        int ContextSize,
        int GpuLayerCount,
        float Temperature,
        int MaxTokens);
}

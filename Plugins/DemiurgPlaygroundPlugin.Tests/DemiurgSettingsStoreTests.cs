using DemiurgPlaygroundPlugin.Configuration;
using Xunit;

namespace DemiurgPlaygroundPlugin.Tests;

public sealed class DemiurgSettingsStoreTests
{
    [Fact]
    public void SaveThenLoad_RoundTripsAllValues()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), $"demiurg-test-{Guid.NewGuid():N}.json");
        var store = new DemiurgSettingsStore(path);
        var saved = new DemiurgSettings
        {
            ModelPath = @"C:\models\holodeck.gguf",
            CudaRuntimePath = @"C:\cuda\bin",
            ContextSize = 8192,
            GpuLayerCount = 42,
            Temperature = 0.55f,
            MaxTokens = 256,
        };

        // Act
        store.Save(saved);
        var loaded = new DemiurgSettings();
        new DemiurgSettingsStore(path).Load(loaded);

        // Assert
        try
        {
            Assert.Multiple(
                () => Assert.Equal(saved.ModelPath, loaded.ModelPath),
                () => Assert.Equal(saved.CudaRuntimePath, loaded.CudaRuntimePath),
                () => Assert.Equal(saved.ContextSize, loaded.ContextSize),
                () => Assert.Equal(saved.GpuLayerCount, loaded.GpuLayerCount),
                () => Assert.Equal(saved.Temperature, loaded.Temperature),
                () => Assert.Equal(saved.MaxTokens, loaded.MaxTokens));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_WhenFileMissing_LeavesDefaults()
    {
        var store = new DemiurgSettingsStore(Path.Combine(Path.GetTempPath(), $"demiurg-missing-{Guid.NewGuid():N}.json"));
        var settings = new DemiurgSettings();

        store.Load(settings);

        Assert.Equal(4096, settings.ContextSize);
    }
}

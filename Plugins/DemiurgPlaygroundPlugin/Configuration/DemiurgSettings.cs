using CommunityToolkit.Mvvm.ComponentModel;

namespace DemiurgPlaygroundPlugin.Configuration;

/// <summary>
/// Session-wide Demiurg Playground settings shared between the chat tab and the configuration
/// section. Persisted by <see cref="DemiurgSettingsStore"/>.
/// </summary>
public sealed partial class DemiurgSettings : ObservableObject
{
    /// <summary>Absolute path to a local GGUF model file.</summary>
    [ObservableProperty]
    public partial string ModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Folder containing the CUDA 12 runtime DLLs (<c>cudart64_12.dll</c>, <c>cublas64_12.dll</c>,
    /// <c>cublasLt64_12.dll</c>). Required for GPU offload because the LLamaSharp CUDA backend does not
    /// bundle them. Leave empty to run on CPU.
    /// </summary>
    [ObservableProperty]
    public partial string CudaRuntimePath { get; set; } = string.Empty;

    /// <summary>Context window size in tokens.</summary>
    [ObservableProperty]
    public partial int ContextSize { get; set; } = 4096;

    /// <summary>Number of model layers to offload to the GPU (999 = offload all).</summary>
    [ObservableProperty]
    public partial int GpuLayerCount { get; set; } = 999;

    /// <summary>Sampling temperature; higher is more random.</summary>
    [ObservableProperty]
    public partial float Temperature { get; set; } = 0.7f;

    /// <summary>Maximum tokens generated per response.</summary>
    [ObservableProperty]
    public partial int MaxTokens { get; set; } = 512;
}

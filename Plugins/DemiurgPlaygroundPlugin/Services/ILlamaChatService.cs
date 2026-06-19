using DemiurgPlaygroundPlugin.Configuration;

namespace DemiurgPlaygroundPlugin.Services;

/// <summary>
/// Stateful boundary over LLamaSharp: owns the loaded model, context, and chat session. All
/// LLamaSharp types stay behind this interface so the rest of the plugin is testable.
/// </summary>
public interface ILlamaChatService
{
    /// <summary>True once a model is loaded and ready to chat.</summary>
    bool IsLoaded { get; }

    /// <summary>True when the loaded model has an embedded chat template; false when the generic
    /// ChatML fallback is in use (base / template-less model).</summary>
    bool UsesEmbeddedTemplate { get; }

    /// <summary>True once llama.cpp has reported a usable CUDA device (inference runs on GPU).</summary>
    bool UsesGpu { get; }

    /// <summary>Loads (or reloads) the GGUF model described by <paramref name="settings"/>.</summary>
    Task LoadModelAsync(DemiurgSettings settings, CancellationToken cancellationToken);

    /// <summary>Clears the conversation, starting a fresh Holodeck session on the loaded model.</summary>
    void ResetSession();

    /// <summary>Streams the model's reply to <paramref name="message"/> token by token.</summary>
    IAsyncEnumerable<string> ChatAsync(string message, CancellationToken cancellationToken);
}

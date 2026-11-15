using System.Runtime.CompilerServices;
using DemiurgPlaygroundPlugin.Chat;
using DemiurgPlaygroundPlugin.Configuration;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using LLama.Transformers;

namespace DemiurgPlaygroundPlugin.Services;

/// <summary>
/// LLamaSharp-backed implementation of <see cref="ILlamaChatService"/>. Loads a GGUF model onto the
/// (CUDA 12) backend and runs a <see cref="ChatSession"/> seeded with the Holodeck system prompt.
/// Registered as a single instance; inference is serialized so the native context is used by one
/// caller at a time.
/// </summary>
public sealed class LlamaChatService : ILlamaChatService, IDisposable
{
    private static readonly string[] CudaRuntimeDlls = ["cudart64_12.dll", "cublas64_12.dll", "cublasLt64_12.dll"];
    private static readonly string[] CpuBackendPreference = ["avx512", "avx2", "avx"];

    private static bool _nativeConfigured;
    private static volatile bool _cudaDeviceDetected;

    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly SemaphoreSlim _inferenceLock = new(1, 1);

    private LLamaWeights? _weights;
    private LLamaContext? _context;
    private ModelParams? _parameters;
    private ChatSession? _session;
    private DemiurgSettings? _settings;
    private IReadOnlyList<string> _antiPrompts = ["User:"];

    public bool IsLoaded => _session is not null;

    /// <summary>True when the loaded model carries an embedded chat template (instruct model); false
    /// when a generic ChatML fallback is in use (base / template-less model).</summary>
    public bool UsesEmbeddedTemplate { get; private set; }

    /// <summary>True once llama.cpp has reported a usable CUDA device (i.e. inference runs on GPU).</summary>
    public bool UsesGpu => _cudaDeviceDetected;

    public async Task LoadModelAsync(DemiurgSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ModelPath) || !File.Exists(settings.ModelPath))
        {
            throw new FileNotFoundException($"Model file not found: '{settings.ModelPath}'.");
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            ConfigureNativeBackend(settings);

            _session = null;
            _context?.Dispose();
            _context = null;
            _weights?.Dispose();
            _weights = null;

            var parameters = new ModelParams(settings.ModelPath)
            {
                ContextSize = (uint)settings.ContextSize,
                GpuLayerCount = settings.GpuLayerCount,
            };

            var (weights, context, session) = await Task.Run(
                () =>
                {
                    var loaded = LLamaWeights.LoadFromFile(parameters);
                    var ctx = loaded.CreateContext(parameters);
                    return (loaded, ctx, NewSession(ctx, loaded));
                },
                cancellationToken);

            _weights = weights;
            _context = context;
            _parameters = parameters;
            _session = session;
            _settings = settings;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public void ResetSession()
    {
        _inferenceLock.Wait();
        try
        {
            if (_weights is null || _parameters is null)
            {
                return;
            }

            _context?.Dispose();
            _context = _weights.CreateContext(_parameters);
            _session = NewSession(_context, _weights);
        }
        finally
        {
            _inferenceLock.Release();
        }
    }

    public async IAsyncEnumerable<string> ChatAsync(string message, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_session is not { } session || _settings is not { } settings)
        {
            throw new InvalidOperationException("No model is loaded. Load a model on the Configure page first.");
        }

        var inferenceParams = new InferenceParams
        {
            MaxTokens = settings.MaxTokens,
            AntiPrompts = _antiPrompts,
            SamplingPipeline = new DefaultSamplingPipeline { Temperature = settings.Temperature },
        };

        await _inferenceLock.WaitAsync(cancellationToken);
        try
        {
            var turn = new ChatHistory.Message(AuthorRole.User, message);
            await foreach (var token in session.ChatAsync(turn, inferenceParams, cancellationToken))
            {
                yield return token;
            }
        }
        finally
        {
            _inferenceLock.Release();
        }
    }

    public void Dispose()
    {
        DisposeModel();
        _loadLock.Dispose();
        _inferenceLock.Dispose();
    }

    private ChatSession NewSession(LLamaContext context, LLamaWeights weights)
    {
        var session = new ChatSession(new InteractiveExecutor(context));

        // Prefer the model's own chat template so an instruct model is prompted in the format it was
        // trained on and emits a stop token the executor recognizes; the default "User:/Assistant:"
        // transform never terminates cleanly. Base / template-less models throw on the embedded
        // template, so fall back to a fixed ChatML format that has its own stop marker. Each path
        // strips its own stop tokens from the streamed output. (Mirrors LLamaSharp's chat examples.)
        if (HasEmbeddedTemplate(weights))
        {
            session.WithHistoryTransform(new PromptTemplateTransformer(weights, withAssistant: true));
            session.WithOutputTransform(new LLamaTransforms.KeywordTextOutputStreamTransform(
                new HashSet<string> { "User:", "�" }, 5, false));
            _antiPrompts = ["User:"];
            UsesEmbeddedTemplate = true;
        }
        else
        {
            session.WithHistoryTransform(new ChatMlHistoryTransform());
            session.WithOutputTransform(new ChatMlOutputTransform());
            _antiPrompts = ["<|im_end|>", "<|im_start|>"];
            UsesEmbeddedTemplate = false;
        }

        session.AddSystemMessage(HolodeckPersona.SystemPrompt);
        return session;
    }

    /// <summary>
    /// Probes whether the model exposes a chat template by applying it to a throwaway message. Models
    /// without one (typically base models) throw <c>llama_model_chat_template_failed</c>.
    /// </summary>
    private static bool HasEmbeddedTemplate(LLamaWeights weights)
    {
        try
        {
            var template = new LLamaTemplate(weights, true);
            template.Add("user", "probe");
            template.Apply();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void DisposeModel()
    {
        _session = null;
        _context?.Dispose();
        _context = null;
        _weights?.Dispose();
        _weights = null;
        _parameters = null;
        _settings = null;
    }

    /// <summary>
    /// One-time native-backend setup (must run before the first LLamaSharp native call). Enables GPU
    /// inference on the LLamaSharp CUDA 12 backend, which on a driver-only machine needs three fixes:
    /// (1) the CUDA backend folder ships no <c>ggml-cpu.dll</c>, which <c>ggml.dll</c> requires — copy
    /// it from a sibling CPU backend folder; (2) the CUDA runtime DLLs are not bundled — copy them from
    /// the configured folder next to <c>ggml-cuda.dll</c>; (3) auto-detect drops CUDA without a Toolkit
    /// install, so point LLamaSharp explicitly at the cuda12 <c>llama.dll</c>. If the CUDA runtime is
    /// unavailable, nothing is forced and LLamaSharp falls back to CPU.
    /// </summary>
    private static void ConfigureNativeBackend(DemiurgSettings settings)
    {
        if (_nativeConfigured)
        {
            return;
        }

        _nativeConfigured = true;

        NativeLibraryConfig.All.WithLogCallback((_, message) =>
        {
            if (message.Contains("CUDA device", StringComparison.OrdinalIgnoreCase))
            {
                _cudaDeviceDetected = true;
            }
        });

        var pluginDir = Path.GetDirectoryName(typeof(LlamaChatService).Assembly.Location);
        if (pluginDir is null)
        {
            return;
        }

        var nativeRoot = Path.Combine(pluginDir, "runtimes", "win-x64", "native");
        var cuda12Dir = Path.Combine(nativeRoot, "cuda12");
        if (!Directory.Exists(cuda12Dir))
        {
            return;
        }

        EnsureGgmlCpu(nativeRoot, cuda12Dir);
        CopyCudaRuntime(settings.CudaRuntimePath, cuda12Dir);

        var llama = Path.Combine(cuda12Dir, "llama.dll");
        if (CudaRuntimeDlls.All(dll => File.Exists(Path.Combine(cuda12Dir, dll))) && File.Exists(llama))
        {
            NativeLibraryConfig.All.WithLibrary(llama, null);
        }
    }

    /// <summary>Copies <c>ggml-cpu.dll</c> into the cuda12 folder from a sibling CPU backend folder if
    /// it is missing there (the CUDA backend package omits it, but <c>ggml.dll</c> needs it).</summary>
    private static void EnsureGgmlCpu(string nativeRoot, string cuda12Dir)
    {
        var target = Path.Combine(cuda12Dir, "ggml-cpu.dll");
        if (File.Exists(target))
        {
            return;
        }

        var source = CpuBackendPreference
            .Select(folder => Path.Combine(nativeRoot, folder, "ggml-cpu.dll"))
            .FirstOrNone(File.Exists).OrElse(() => Directory.EnumerateFiles(nativeRoot, "ggml-cpu.dll", SearchOption.AllDirectories).FirstOrNone());

        if (source is [var notNullSource])
        {
            File.Copy(notNullSource, target, overwrite: true);
        }
    }

    /// <summary>Copies the CUDA 12 runtime DLLs from the user-configured folder next to ggml-cuda.dll.</summary>
    private static void CopyCudaRuntime(string runtimePath, string cuda12Dir)
    {
        if (string.IsNullOrWhiteSpace(runtimePath) || !Directory.Exists(runtimePath))
        {
            return;
        }

        foreach (var dll in CudaRuntimeDlls)
        {
            var source = Path.Combine(runtimePath, dll);
            if (File.Exists(source))
            {
                File.Copy(source, Path.Combine(cuda12Dir, dll), overwrite: true);
            }
        }
    }
}

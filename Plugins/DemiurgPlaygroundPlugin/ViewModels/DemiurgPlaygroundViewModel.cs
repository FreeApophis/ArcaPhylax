using System.Collections.ObjectModel;
using System.Diagnostics;
using ArcaPhylax.Abstractions;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DemiurgPlaygroundPlugin.Chat;
using DemiurgPlaygroundPlugin.Services;

namespace DemiurgPlaygroundPlugin.ViewModels;

/// <summary>
/// Demiurg Playground chat tab. A scrolling transcript over the "Holodeck" character: ordinary lines
/// run inside the loaded simulation, while lines addressed to "Computer" or "Arch" are answered by
/// the ship's computer directly. Inference streams off the UI thread, marshalling tokens back via the
/// dispatcher.
/// </summary>
// Derives from ObservableObject directly (not the shared ViewModelBase) — see the cross-AssemblyLoadContext
// note on CharacterArchiveViewModel.
public sealed partial class DemiurgPlaygroundViewModel(ILlamaChatService chat) : ObservableObject, ITabContentViewModel, IDisposable
{
    private readonly ILlamaChatService _chat = chat;

    private CancellationTokenSource? _turnCts;

    public string Title => "Demiurg Playground";

    public string Icon => DemiurgPlaygroundIcons.Brain;

    public int NavigationWeight => 30;

    public ObservableCollection<ChatMessageVm> Messages { get; } = [];

    [ObservableProperty]
    public partial string Input { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Status { get; set; } =
        "Address the holodeck. Start a line with \"Computer\" or \"Arch\" to speak to the computer directly.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    public partial bool IsBusy { get; set; }

    private bool CanSend => !IsBusy;

    public void Dispose()
    {
        _turnCts?.Cancel();
        _turnCts?.Dispose();
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task Send()
    {
        if (string.IsNullOrWhiteSpace(Input))
        {
            return;
        }

        if (!_chat.IsLoaded)
        {
            Status = "No model loaded. Load a .gguf model on the Configure page first.";
            return;
        }

        var turn = TurnClassifier.Classify(Input);
        Input = string.Empty;

        Messages.Add(new ChatMessageVm(ChatRole.User, turn.Text));
        var reply = new ChatMessageVm(turn.AddressedToComputer ? ChatRole.Computer : ChatRole.Simulation, string.Empty);
        Messages.Add(reply);

        _turnCts?.Cancel();
        _turnCts?.Dispose();
        var cts = new CancellationTokenSource();
        _turnCts = cts;

        IsBusy = true;
        var verb = turn.AddressedToComputer ? "Computer responding" : "Running simulation";
        Status = $"{verb}…";

        // Stopwatch starts on the first token so the rate reflects generation throughput, not the
        // one-off prompt-eval latency. Token count is an approximation (streamed text pieces).
        var stopwatch = new Stopwatch();
        var tokenCount = 0;
        try
        {
            await Task.Run(
                async () =>
                {
                    var lastReportMs = 0L;
                    await foreach (var token in _chat.ChatAsync(turn.MarkedText, cts.Token).ConfigureAwait(false))
                    {
                        if (!stopwatch.IsRunning)
                        {
                            stopwatch.Start();
                        }

                        var piece = token;
                        await Dispatcher.UIThread.InvokeAsync(() => reply.Append(piece));
                        tokenCount++;

                        if (stopwatch.ElapsedMilliseconds - lastReportMs >= 250)
                        {
                            lastReportMs = stopwatch.ElapsedMilliseconds;
                            var liveRate = TokensPerSecond(tokenCount, stopwatch);
                            await Dispatcher.UIThread.InvokeAsync(() => Status = $"{verb}… {tokenCount} tok · {liveRate:F1} tok/s");
                        }
                    }
                },
                cts.Token);

            Status = tokenCount > 0
                ? $"{tokenCount} tokens · {stopwatch.Elapsed.TotalSeconds:F1}s · {TokensPerSecond(tokenCount, stopwatch):F1} tok/s"
                : null;
        }
        catch (OperationCanceledException)
        {
            // Superseded by a new session or turn.
        }
        catch (Exception ex)
        {
            reply.Append($"\n[error: {ex.Message}]");
            Status = $"Inference failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void NewSession()
    {
        _turnCts?.Cancel();
        _chat.ResetSession();
        Messages.Clear();
        Status = "New holodeck session. The computer is standing by.";
    }

    private static double TokensPerSecond(int tokens, Stopwatch stopwatch) =>
        stopwatch.Elapsed.TotalSeconds > 0 ? tokens / stopwatch.Elapsed.TotalSeconds : 0;
}

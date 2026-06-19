using System.Text;
using LLama.Abstractions;
using LLama.Common;

namespace DemiurgPlaygroundPlugin.Services;

/// <summary>
/// A fixed ChatML history transform used as a fallback for GGUF models that carry no embedded chat
/// template (e.g. base models), where <c>PromptTemplateTransformer</c> throws. Imposes a known turn
/// structure so generation has a recognizable stop marker (<c>&lt;|im_end|&gt;</c>).
/// </summary>
public sealed class ChatMlHistoryTransform : IHistoryTransform
{
    public string HistoryToText(ChatHistory history)
    {
        var builder = new StringBuilder();
        foreach (var message in history.Messages)
        {
            builder
                .Append("<|im_start|>").Append(RoleName(message.AuthorRole)).Append('\n')
                .Append(message.Content).Append("<|im_end|>\n");
        }

        builder.Append("<|im_start|>assistant\n");
        return builder.ToString();
    }

    public ChatHistory TextToHistory(AuthorRole role, string text) =>
        new([new ChatHistory.Message(role, text)]);

    public IHistoryTransform Clone() => new ChatMlHistoryTransform();

    private static string RoleName(AuthorRole role) => role switch
    {
        AuthorRole.System => "system",
        AuthorRole.Assistant => "assistant",
        _ => "user",
    };
}

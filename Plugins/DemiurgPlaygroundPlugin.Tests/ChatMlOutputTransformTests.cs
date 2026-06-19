using DemiurgPlaygroundPlugin.Services;
using Xunit;

namespace DemiurgPlaygroundPlugin.Tests;

public sealed class ChatMlOutputTransformTests
{
    [Fact]
    public async Task TransformAsync_StripsTrailingStopMarker()
    {
        var result = await Collect("Hello!", "<|im_end|>");

        Assert.Equal("Hello!", result);
    }

    [Fact]
    public async Task TransformAsync_StripsMarkerSplitAcrossTokens()
    {
        var result = await Collect("Hi", "<|im", "_end|>");

        Assert.Equal("Hi", result);
    }

    [Fact]
    public async Task TransformAsync_DropsReplacementCharacter()
    {
        var result = await Collect("Hi", "�", "!");

        Assert.Equal("Hi!", result);
    }

    [Fact]
    public async Task TransformAsync_PassesPlainTextThrough()
    {
        var result = await Collect("The ", "holodeck ", "hums.");

        Assert.Equal("The holodeck hums.", result);
    }

    private static async Task<string> Collect(params string[] tokens)
    {
        var transform = new ChatMlOutputTransform();
        var output = new System.Text.StringBuilder();

        await foreach (var piece in transform.TransformAsync(ToAsync(tokens)))
        {
            output.Append(piece);
        }

        return output.ToString();
    }

    private static async IAsyncEnumerable<string> ToAsync(IEnumerable<string> tokens)
    {
        foreach (var token in tokens)
        {
            yield return token;
            await Task.Yield();
        }
    }
}

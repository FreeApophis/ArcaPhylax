namespace DemiurgPlaygroundPlugin.Chat;

/// <summary>
/// Pure classification of a user turn. A turn is addressed to the computer when it begins with one
/// of the wake words "Computer" or "Arch" as a standalone leading word (case-insensitive); anything
/// else is directed at the running simulation.
/// </summary>
public static class TurnClassifier
{
    private static readonly string[] WakeWords = ["computer", "arch"];

    public static TurnClassification Classify(string input)
    {
        var text = input.Trim();
        return new TurnClassification(StartsWithWakeWord(text), text);
    }

    private static bool StartsWithWakeWord(string text) =>
        WakeWords.Any(word => IsLeadingWord(text, word));

    private static bool IsLeadingWord(string text, string word) =>
        text.StartsWith(word, StringComparison.OrdinalIgnoreCase)
        && (text.Length == word.Length || !char.IsLetter(text[word.Length]));
}

using System.Text.RegularExpressions;
using Funcky.Monads;

namespace ArcaPhylax.CharacterCards.Cards;

/// <summary>
/// Computes the <c>character_version</c> to persist on the next save. The stored form is
/// <c>&lt;version&gt; r&lt;revision&gt;</c> (e.g. <c>1.0 r3</c>). A blank version starts at
/// <c>1.0 r1</c>. When the version is unchanged since the last save the revision is bumped —
/// a missing revision becomes <c>r1</c>, an existing one is increased by one — so the user may
/// nudge either part by hand. When the user changes the version the revision resets to <c>r1</c>,
/// starting a fresh revision line for the new version.
/// </summary>
public static partial class CardVersion
{
    private const string DefaultVersion = "1.0";

    /// <param name="current">The version currently in the editor (possibly edited by the user).</param>
    /// <param name="previous">The version stored by the last save.</param>
    public static string Next(string? current, string? previous)
    {
        var (version, revision) = Parse(current);

        return string.IsNullOrEmpty(version)
            ? Format(DefaultVersion, 1)
            : version == Parse(previous).Version
                ? Format(version, revision.Match(none: 1, some: r => r + 1))
                : Format(version, 1);
    }

    private static string Format(string version, int revision) => $"{version} r{revision}";

    // Splits a stored version into its version text and trailing " r<n>" revision, if any.
    private static (string Version, Option<int> Revision) Parse(string? raw)
    {
        var text = raw?.Trim() ?? string.Empty;
        var match = RevisionSuffix().Match(text);

        return match.Success
            ? (text[..match.Index].Trim(), Option.Some(int.Parse(match.Groups[1].Value)))
            : (text, Option<int>.None);
    }

    [GeneratedRegex(@"\s+[rR](\d+)\s*$")]
    private static partial Regex RevisionSuffix();
}

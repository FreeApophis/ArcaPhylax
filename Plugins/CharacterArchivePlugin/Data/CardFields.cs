namespace CharacterArchivePlugin.Data;

/// <summary>The free-text roleplay fields of a card shown in the detail pane alongside the description.</summary>
public sealed record CardFields(string Personality, string Scenario, string Greeting, string CharacterVersion, string CreatorNotes)
{
    public static readonly CardFields Empty = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
}

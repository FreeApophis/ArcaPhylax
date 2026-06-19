namespace DemiurgPlaygroundPlugin.Chat;

/// <summary>Whether a user turn is addressed to the computer directly, plus the trimmed text.</summary>
public sealed record TurnClassification(bool AddressedToComputer, string Text)
{
    /// <summary>The text to send to the model, prefixed with the matching Holodeck control marker.</summary>
    public string MarkedText =>
        AddressedToComputer
            ? $"{HolodeckPersona.ComputerMarker} {Text}"
            : $"{HolodeckPersona.SimulationMarker} {Text}";
}

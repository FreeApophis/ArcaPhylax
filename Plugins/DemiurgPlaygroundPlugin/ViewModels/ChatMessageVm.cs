using CommunityToolkit.Mvvm.ComponentModel;

namespace DemiurgPlaygroundPlugin.ViewModels;

/// <summary>Who authored a transcript line; drives styling and alignment in the view.</summary>
public enum ChatRole
{
    /// <summary>A crew-member message.</summary>
    User,

    /// <summary>A reply spoken by the ship's computer directly.</summary>
    Computer,

    /// <summary>A reply from inside a running simulation.</summary>
    Simulation,
}

/// <summary>
/// One line in the chat transcript. Unlike the plugin's other display models this is not a record:
/// streamed tokens append to <see cref="Text"/> live, which needs INPC.
/// </summary>
public sealed partial class ChatMessageVm : ObservableObject
{
    public ChatMessageVm(ChatRole role, string text)
    {
        Role = role;
        Text = text;
    }

    public ChatRole Role { get; }

    [ObservableProperty]
    public partial string Text { get; set; }

    public bool IsUser => Role == ChatRole.User;

    public bool IsComputer => Role == ChatRole.Computer;

    public bool IsSimulation => Role == ChatRole.Simulation;

    public string Author => Role switch
    {
        ChatRole.User => "You",
        ChatRole.Computer => "Computer",
        ChatRole.Simulation => "Simulation",
        _ => string.Empty,
    };

    public void Append(string token) => Text += token;
}

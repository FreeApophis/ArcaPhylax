using ArcaPhylax.Abstractions;

namespace ArcaPhylax.ViewModels;

public sealed class ChatViewModel : NotifyBase, ITabContentViewModel
{
    public string Title => "Chat and Messaging";
    public string Icon => "💬";
}

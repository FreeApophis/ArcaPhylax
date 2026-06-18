using ArcaPhylax.Abstractions;

namespace ArcaPhylax.ViewModels;

public sealed class ChatViewModel : ViewModelBase, ITabContentViewModel
{
    public string Title => "Chat and Messaging";

    public string Icon => NavigationIcons.Chat;

    public int NavigationWeight => 60;
}

namespace ArcaPhylax.ViewModels;

/// <summary>One line in the Plugins tab. <paramref name="RemovableId"/> is set for store-installed plugins only.</summary>
public sealed record PluginRowVm(string Name, string Version, string Source, string Publisher, string State, Option<string> RemovableId)
{
    public bool CanRemove => RemovableId.Match(none: false, some: True);
}

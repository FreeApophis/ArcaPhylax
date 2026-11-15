using ArcaPhylax.PluginPackaging;
using ArcaPhylax.PluginSystem;

namespace ArcaPhylax.ViewModels;

/// <summary>One publisher in the Trusted publishers list.</summary>
/// <remarks><c>ForgetLabel</c> is "Unblock" or "Remove" when the user's own decision can be undone, empty otherwise.</remarks>
public sealed record PublisherRowVm(string Name, string KeyId, string Standing, string Detail, bool CanBlock, string ForgetLabel)
{
    public bool CanForget => ForgetLabel.Length > 0;

    /// <summary>Key and detail in one line. (Not as bound Runs: a Run binds TwoWay and its StringFormat would write back.)</summary>
    public string Summary => $"Key {KeyId} · {Detail}";

    public static PublisherRowVm Of(PublisherStanding standing) => standing switch
    {
        EndorsedPublisher endorsed => new(
            endorsed.Publisher.Name,
            endorsed.Publisher.Key.KeyId,
            IsBuiltIn(endorsed.Publisher.Key) ? "Built in" : "Endorsed",
            $"Plugins: {string.Join(", ", endorsed.Publisher.PluginPrefixes.Select(prefix => prefix + ".*"))}",
            CanBlock: true,
            ForgetLabel: string.Empty),
        UserTrustedPublisher trusted => new(
            trusted.Key.Name,
            trusted.Key.Key.KeyId,
            "Trusted by you",
            "Any plugin id no endorsed publisher owns",
            CanBlock: false,
            ForgetLabel: "Remove"),
        BlockedPublisher blocked => new(
            blocked.Name.GetOrElse("Unknown publisher"),
            blocked.KeyId,
            "Blocked by you",
            "Its plugins are not installed or loaded",
            CanBlock: false,
            ForgetLabel: "Unblock"),
    };

    private static bool IsBuiltIn(PublisherKey key) => TrustAnchors.BuiltIn.Any(publisher => publisher.Key == key);
}

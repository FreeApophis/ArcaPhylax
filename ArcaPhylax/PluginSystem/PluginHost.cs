using System.Runtime.InteropServices;
using ArcaPhylax.Abstractions;
using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.PluginSystem;

/// <summary>Facts about this host that plugin installation depends on.</summary>
public static class PluginHost
{
    /// <summary>Per-user install location (%LOCALAPPDATA%\ArcaPhylax\plugins on Windows, ~/.local/share/... on Linux).</summary>
    public static string StoreRoot
        => Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "ArcaPhylax",
            "plugins");

    /// <summary>Bundled plugins shipped next to the application.</summary>
    public static string BundledRoot => Path.Combine(AppContext.BaseDirectory, "plugins");

    /// <summary>This host's platform and plugin SDK version.</summary>
    public static HostEnvironment Environment { get; } = new(
        RuntimeInformation.RuntimeIdentifier,
        typeof(IPlugin).Assembly.GetName().Version ?? new Version(1, 0, 0));

    public static PluginStore CreateStore() => new(StoreRoot, Environment, TrustedPublisherKeys.All);
}

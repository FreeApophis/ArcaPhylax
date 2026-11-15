using System.Runtime.InteropServices;
using ArcaPhylax.Abstractions;
using ArcaPhylax.PluginPackaging;

namespace ArcaPhylax.PluginSystem;

/// <summary>Facts about this host that plugin installation depends on.</summary>
public static class PluginHost
{
    /// <summary>Per-user install location (%LOCALAPPDATA%\ArcaPhylax\plugins on Windows, ~/.local/share/... on Linux).</summary>
    public static string StoreRoot => Path.Combine(DataRoot, "plugins");

    /// <summary>Per-user ArcaPhylax data (%LOCALAPPDATA%\ArcaPhylax): plugin store, publisher list cache, trust overrides.</summary>
    public static string DataRoot
        => Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "ArcaPhylax");

    /// <summary>Bundled plugins shipped next to the application.</summary>
    public static string BundledRoot => Path.Combine(AppContext.BaseDirectory, "plugins");

    /// <summary>This host's platform and plugin SDK version.</summary>
    public static HostEnvironment Environment { get; } = new(
        RuntimeInformation.RuntimeIdentifier,
        typeof(IPlugin).Assembly.GetName().Version ?? new Version(1, 0, 0));

    /// <summary>
    /// The online plugin catalog (GitHub Pages, published by .github/workflows/pages.yml). Set
    /// ARCAPHYLAX_PLUGIN_CATALOG to another URL or a local catalog.json path to test a catalog before publishing it.
    /// </summary>
    public static Uri CatalogUri { get; } = Option.FromNullable(System.Environment.GetEnvironmentVariable("ARCAPHYLAX_PLUGIN_CATALOG"))
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Match(none: () => new Uri("https://freeapophis.github.io/ArcaPhylax/catalog.json"), some: CatalogUriFrom);

    public static PluginStore CreateStore() => new(StoreRoot, Environment);

    public static PluginTrust CreateTrust() => new(DataRoot);

    private static Uri CatalogUriFrom(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : new Uri(Path.GetFullPath(value));
}

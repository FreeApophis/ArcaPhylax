using System.Text.Json.Serialization;

namespace ArcaPhylax.PluginPackaging;

/// <summary>A store change that can only take effect while no plugin is loaded (applied at startup). Closed.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "op")]
[JsonDerivedType(typeof(Activate), "activate")]
[JsonDerivedType(typeof(Remove), "remove")]
public closed record PendingOperation(string Id);

/// <summary>Make <paramref name="Version"/> the only installed version of plugin <paramref name="Id"/>.</summary>
public sealed record Activate(string Id, string Version) : PendingOperation(Id);

/// <summary>Uninstall plugin <paramref name="Id"/> entirely.</summary>
public sealed record Remove(string Id) : PendingOperation(Id);

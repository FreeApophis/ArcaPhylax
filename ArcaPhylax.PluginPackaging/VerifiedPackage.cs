using System.Collections.Immutable;

namespace ArcaPhylax.PluginPackaging;

/// <summary>A package whose signature, layout and manifest all checked out.</summary>
/// <param name="Manifest">The validated manifest.</param>
/// <param name="KeyId">The trusted key that signed it.</param>
/// <param name="PayloadFiles">Payload file paths relative to <c>payload/</c> (forward slashes).</param>
public sealed record VerifiedPackage(PluginManifest Manifest, string KeyId, ImmutableList<string> PayloadFiles);

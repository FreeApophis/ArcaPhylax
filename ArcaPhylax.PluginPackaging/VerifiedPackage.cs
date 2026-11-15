using System.Collections.Immutable;

namespace ArcaPhylax.PluginPackaging;

/// <summary>A package whose signature, layout and manifest all checked out. Whether its signer is trusted is a separate question.</summary>
/// <param name="Manifest">The validated manifest.</param>
/// <param name="Signer">The key that signed it.</param>
/// <param name="PayloadFiles">Payload file paths relative to <c>payload/</c> (forward slashes).</param>
public sealed record VerifiedPackage(PluginManifest Manifest, PublisherKey Signer, ImmutableList<string> PayloadFiles);

namespace ArcaPhylax.PluginPackaging;

/// <summary>
/// A signed JSON document: the exact signed bytes travel base64-encoded in <see cref="Payload"/>, so no
/// JSON canonicalisation is needed to check the signature.
/// </summary>
internal sealed record SignedEnvelope(string Payload, string KeyId, string Algorithm, string Signature);

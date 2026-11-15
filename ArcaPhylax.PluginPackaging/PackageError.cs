namespace ArcaPhylax.PluginPackaging;

/// <summary>Why a <c>.arcaplugin</c> package was rejected. Closed: callers switch over it exhaustively.</summary>
public closed record PackageError;

/// <summary>The file is not a readable zip archive.</summary>
public sealed record NotAPackage(string Reason) : PackageError;

/// <summary>A required entry (manifest, signature, or the manifest's entry assembly) is missing.</summary>
public sealed record MissingEntry(string Path) : PackageError;

/// <summary>An entry path could escape the install directory or is outside the allowed layout.</summary>
public sealed record UnsafeEntryPath(string Path) : PackageError;

/// <summary><c>plugin.json</c> is malformed or fails validation.</summary>
public sealed record InvalidManifest(string Reason) : PackageError;

/// <summary>The package is signed by a key the host does not trust.</summary>
public sealed record UntrustedKey(string KeyId) : PackageError;

/// <summary>The signature does not match the package contents (tampered or corrupt).</summary>
public sealed record InvalidSignature : PackageError;

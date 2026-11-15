namespace ArcaPhylax.Data;

/// <summary>
/// Where a dropped item lives. Closed: these three cases are the only ones, so every
/// <c>switch</c> over a <see cref="DropSource"/> is checked for exhaustiveness by the compiler.
/// </summary>
public closed record DropSource;

/// <summary>A folder dropped from the local file system, indexed in place.</summary>
public sealed record LinkedFolder(string Path) : DropSource;

/// <summary>A file dropped from the local file system, indexed in place.</summary>
public sealed record LinkedFile(string Path) : DropSource;

/// <summary>A file without a local path (e.g. from a sandboxed source), copied into the app's cache.</summary>
public sealed record CachedFile(string StoredPath) : DropSource;

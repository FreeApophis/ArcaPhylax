namespace ArcaPhylax.Data;

public record DroppedItem(long Id, string Kind, string Name, string? OriginalPath, string? StoredPath, string CreatedUtc);

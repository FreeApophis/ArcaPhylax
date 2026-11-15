namespace ArcaPhylax.Data;

public sealed record DroppedItem(long Id, string Name, DropSource Source, string CreatedUtc);

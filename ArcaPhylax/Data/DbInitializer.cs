using Dapper;

namespace ArcaPhylax.Data;

public interface IDbInitializer
{
    void EnsureCreatedAndSeed();
}

public sealed class DbInitializer(ISqliteConnectionFactory db) : IDbInitializer
{
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS Documents (
          Id INTEGER PRIMARY KEY AUTOINCREMENT,
          Title TEXT NOT NULL,
          Kind TEXT NOT NULL,
          Subtitle TEXT NULL
        );
        
        CREATE INDEX IF NOT EXISTS IX_Documents_Title ON Documents(Title);
        
        CREATE TABLE IF NOT EXISTS DroppedItems (
          Id INTEGER PRIMARY KEY AUTOINCREMENT,
          Kind TEXT NOT NULL,
          Name TEXT NOT NULL,
          OriginalPath TEXT NULL,
          StoredPath TEXT NULL,
          CreatedUtc TEXT NOT NULL
        );
        
        CREATE INDEX IF NOT EXISTS IX_DroppedItems_CreatedUtc ON DroppedItems(CreatedUtc);
        
        CREATE VIRTUAL TABLE IF NOT EXISTS DroppedItems_fts
        USING fts5(Name, OriginalPath, StoredPath, content='DroppedItems', content_rowid='Id');
        
        CREATE TRIGGER IF NOT EXISTS DroppedItems_ai AFTER INSERT ON DroppedItems BEGIN
          INSERT INTO DroppedItems_fts(rowid, Name, OriginalPath, StoredPath)
          VALUES (new.Id, new.Name, new.OriginalPath, new.StoredPath);
        END;
        
        CREATE TRIGGER IF NOT EXISTS DroppedItems_ad AFTER DELETE ON DroppedItems BEGIN
          INSERT INTO DroppedItems_fts(DroppedItems_fts, rowid, Name, OriginalPath, StoredPath)
          VALUES('delete', old.Id, old.Name, old.OriginalPath, old.StoredPath);
        END;
        
        CREATE TRIGGER IF NOT EXISTS DroppedItems_au AFTER UPDATE ON DroppedItems BEGIN
          INSERT INTO DroppedItems_fts(DroppedItems_fts, rowid, Name, OriginalPath, StoredPath)
          VALUES('delete', old.Id, old.Name, old.OriginalPath, old.StoredPath);
          INSERT INTO DroppedItems_fts(rowid, Name, OriginalPath, StoredPath)
          VALUES (new.Id, new.Name, new.OriginalPath, new.StoredPath);
        END;
        """;


    public void EnsureCreatedAndSeed()
    {
        using var connection = db.Create();

        connection.Execute(Schema);

        var count = connection.ExecuteScalar<long>("SELECT COUNT(1) FROM Documents;");

        if (count > 0)
        {
            return;
        }

        using var tx = connection.BeginTransaction();
        using var insert = connection.CreateCommand();
        insert.Transaction = tx;
        insert.CommandText = "INSERT INTO Documents (Title, Kind, Subtitle) VALUES ($t,$k,$s);";

        var pT = insert.CreateParameter(); pT.ParameterName = "$t"; insert.Parameters.Add(pT);
        var pK = insert.CreateParameter(); pK.ParameterName = "$k"; insert.Parameters.Add(pK);
        var pS = insert.CreateParameter(); pS.ParameterName = "$s"; insert.Parameters.Add(pS);

        // Seed 10k mixed tiles (fast enough for demo)
        string[] kinds = ["Image", "Document", "Video"];
        var rnd = new Random(42);

        for (int i = 1; i <= 10000; i++)
        {
            var kind = kinds[rnd.Next(kinds.Length)];
            pT.Value = $"{kind} Asset {i:00000}";
            pK.Value = kind;
            pS.Value = kind switch
            {
                "Image" => $"{rnd.Next(800, 6000)}×{rnd.Next(600, 4000)} • PNG",
                "Video" => $"{rnd.Next(1, 180)} min • H.264",
                _ => $"{rnd.Next(1, 420)} pages • PDF"
            };
            insert.ExecuteNonQuery();
        }

        tx.Commit();
    }
}

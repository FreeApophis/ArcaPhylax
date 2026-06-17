using Npgsql;

namespace CharacterArchivePlugin.Data;

/// <summary>
/// Creates the <c>character_search</c> materialized view if missing and refreshes it. The DDL lives
/// in the embedded <c>Data/character_search.sql</c> so the index is reproducible from the app.
/// </summary>
public sealed class SearchIndexBuilder(INpgsqlConnectionFactory db)
{
    private const string ResourceName = "CharacterArchivePlugin.Data.character_search.sql";

    public async Task RebuildAsync(CancellationToken cancellationToken = default)
    {
        using var connection = db.Create();

        await using (var ensure = new NpgsqlCommand(ReadDdl(), connection))
        {
            await ensure.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var refresh = new NpgsqlCommand(
            "REFRESH MATERIALIZED VIEW CONCURRENTLY character_search;", connection);
        await refresh.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string ReadDdl()
    {
        using var stream = typeof(SearchIndexBuilder).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

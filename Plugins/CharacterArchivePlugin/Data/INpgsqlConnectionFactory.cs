using CharacterArchivePlugin.Configuration;
using Npgsql;

namespace CharacterArchivePlugin.Data;

public interface INpgsqlConnectionFactory
{
    NpgsqlConnection Create();
}

/// <summary>
/// Opens connections to the user-supplied <c>char_archive</c> database. Mirrors the host's
/// <c>SqliteConnectionFactory</c>; the connection string comes from <see cref="CharacterArchiveSettings"/>.
/// </summary>
public sealed class NpgsqlConnectionFactory(CharacterArchiveSettings settings) : INpgsqlConnectionFactory
{
    public NpgsqlConnection Create()
    {
        var connection = new NpgsqlConnection(settings.ConnectionString);
        connection.Open();
        return connection;
    }
}

using Microsoft.Data.Sqlite;

namespace NutritionPlugin.Data;

/// <summary>Opens ready-to-use connections to the plugin's private SQLite database.</summary>
public interface INutritionConnectionFactory
{
    SqliteConnection Create();
}

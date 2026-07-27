using System.Collections.Immutable;
using Dapper;

namespace NutritionPlugin.Data;

/// <summary>
/// Local mirror of the host's <c>ArcaPhylax.Extensions.ToDynamicParameters</c>. Plugins reference
/// only <c>ArcaPhylax.Plugin.Sdk</c>/<c>Abstractions</c>, so the host extension is not available
/// across the boundary. Keeps the established "build Dapper parameters from an
/// <see cref="ImmutableDictionary{TKey,TValue}"/>" convention.
/// </summary>
internal static class DapperExtensions
{
    public static DynamicParameters ToDynamicParameters(this ImmutableDictionary<string, object?> parameters)
    {
        var dynamicParameters = new DynamicParameters();
        foreach (var (name, value) in parameters)
        {
            dynamicParameters.Add(name, value);
        }

        return dynamicParameters;
    }
}

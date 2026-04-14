using Dapper;
using System.Collections.Immutable;

namespace ArcaPhylax.Extensions
{
    public static class DictionaryExtensions
    {
        extension(ImmutableDictionary<string, object?> dictionary)
        {
            public DynamicParameters ToDynamicParameters()
                => new(dictionary);
        }
    }
}

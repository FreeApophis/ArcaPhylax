using System.Collections.Immutable;
using Dapper;

namespace ArcaPhylax.Extensions;

public static class DictionaryExtensions
{
    extension(ImmutableDictionary<string, object?> dictionary)
    {
        public DynamicParameters ToDynamicParameters()
            => new(dictionary);
    }
}

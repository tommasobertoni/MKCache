using System.Collections.Generic;

namespace MKCache
{
    internal static class Helpers
    {
        public static IReadOnlyList<T> AsReadOnly<T>(this IReadOnlyList<T> list) => list;
    }
}

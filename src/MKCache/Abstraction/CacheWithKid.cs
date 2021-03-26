using Microsoft.Extensions.Caching.Memory;

namespace MKCache.Abstraction
{
    internal class CacheWithKid<T>
    {
        public CacheWithKid(
            MemoryCache cache,
            MKCache<T>.KeyIdentifier keyIdentifier)
        {
            Cache = cache;
            KeyIdentifier = keyIdentifier;
        }

        public MemoryCache Cache { get; }

        public MKCache<T>.KeyIdentifier KeyIdentifier { get; }

        public void Deconstruct(
            out MemoryCache cache,
            out MKCache<T>.KeyIdentifier keyIdentifier)
        {
            cache = Cache;
            keyIdentifier = KeyIdentifier;
        }
    }
}

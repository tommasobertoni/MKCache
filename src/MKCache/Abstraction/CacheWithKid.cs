using Microsoft.Extensions.Caching.Memory;

namespace MKCache.Abstraction
{
    internal class CacheWithKid<T>
    {
        public CacheWithKid(
            IMemoryCache cache,
            MKCache<T>.KeyIdentifier keyIdentifier)
        {
            Cache = cache;
            KeyIdentifier = keyIdentifier;
        }

        public IMemoryCache Cache { get; }

        public MKCache<T>.KeyIdentifier KeyIdentifier { get; }

        public void Deconstruct(
            out IMemoryCache cache,
            out MKCache<T>.KeyIdentifier keyIdentifier)
        {
            cache = Cache;
            keyIdentifier = KeyIdentifier;
        }
    }
}

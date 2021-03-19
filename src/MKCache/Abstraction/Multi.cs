using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace MKCache.Abstraction
{
    internal class Multi<T> : ICache<T>
    {
        private readonly MemoryCacheOptions _options;
        private readonly IReadOnlyList<MKCache<T>.KeyIdentifier> _keyIdentifiers;
        private IReadOnlyList<CacheWithKid<T>> _caches;

        public Multi(
            MemoryCacheOptions options,
            IReadOnlyList<MKCache<T>.KeyIdentifier> keyIdentifiers)
        {
            _options = options;
            _keyIdentifiers = keyIdentifiers;
            _caches = Create(options, keyIdentifiers);
        }

        public int CachesCount => _caches.Count;

        public bool TryGetValue(object key, out T? value)
        {
            value = default;

            foreach (var (cache, _) in _caches)
            {
                if (cache.TryGetValue(key, out value))
                    return true;
            }

            return false;
        }

        public void Set(object ignoredKey, T value, TimeSpan absoluteExpirationRelativeToNow)
        {
            foreach (var (cache, kid) in _caches)
            {
                object key = kid(value);

                if (key != null)
                    cache.Set(key, value, absoluteExpirationRelativeToNow);
            }
        }

        public void Dispose()
        {
            foreach (var (cache, _) in _caches)
                cache.Dispose();
        }

        public void Reset()
        {
            var oldCaches = _caches;
            _caches = Create(_options, _keyIdentifiers);

            foreach (var (cache, _) in oldCaches)
                cache.Dispose();
        }

        private static IReadOnlyList<CacheWithKid<T>> Create(
            MemoryCacheOptions options,
            IReadOnlyList<MKCache<T>.KeyIdentifier> keyIdentifiers)
        {
            return keyIdentifiers.Select(kid => new CacheWithKid<T>(new MemoryCache(options), kid)).ToArray();
        }
    }
}

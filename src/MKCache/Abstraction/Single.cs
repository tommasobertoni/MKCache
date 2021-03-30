using System;
using Microsoft.Extensions.Caching.Memory;

namespace MKCache.Abstraction
{
    internal class Single<T> : ICache<T>
    {
        private readonly MemoryCacheOptions _options;
        private MemoryCache _cache;

        public Single(MemoryCacheOptions options)
        {
            _options = options;
            _cache = Create(_options);
        }

        public int Count => _cache.Count;

        public bool TryGetValue(object key, out T? value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public void Set(object key, T value, TimeSpan expirationRelativeToNow)
        {
            _cache.Set(key, value, expirationRelativeToNow);
        }

        public void Dispose() => _cache.Dispose();

        public void Clear()
        {
            var oldCache = _cache;
            _cache = Create(_options);
            oldCache.Dispose();
        }

        private static MemoryCache Create(MemoryCacheOptions options) => new(options);
    }
}

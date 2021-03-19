using System;
using Microsoft.Extensions.Caching.Memory;

namespace MKCache.Abstraction
{
    public class Single<T> : ICache<T>
    {
        private readonly MemoryCacheOptions _options;
        private IMemoryCache _cache;

        public Single(MemoryCacheOptions options)
        {
            _options = options;
            _cache = Create(_options);
        }

        public bool TryGetValue(object key, out T? value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public void Set(object key, T value, TimeSpan absoluteExpirationRelativeToNow)
        {
            _cache.Set(key, value, absoluteExpirationRelativeToNow);
        }

        public void Dispose() => _cache.Dispose();

        public void Reset()
        {
            var oldCache = _cache;
            _cache = Create(_options);
            oldCache.Dispose();
        }

        private static IMemoryCache Create(MemoryCacheOptions options) => new MemoryCache(options);
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using MKCache.Abstraction;

namespace MKCache
{
    public class MKCache<T>
    {
        public delegate object KeyIdentifier(T item);

        protected ICache<T> _cache;
        protected readonly MemoryCacheOptions _options;
        protected readonly IReadOnlyList<KeyIdentifier> _keyIdentifiers;
        protected readonly ConcurrentDictionary<object, Task<T>> _runningAsyncFetchers = new();

        public MKCache(params KeyIdentifier[] keyIdentifiers)
            : this(new MemoryCacheOptions(), keyIdentifiers)
        {
        }

        public MKCache(MemoryCacheOptions options, params KeyIdentifier[] keyIdentifiers)
            : this(options, keyIdentifiers.AsReadOnly())
        {
        }

        public MKCache(
            MemoryCacheOptions options,
            IReadOnlyList<KeyIdentifier> keyIdentifiers)
        {
            _options = options;
            _keyIdentifiers = keyIdentifiers;
            _cache = Create(_options, _keyIdentifiers);
        }

        public bool ReuseRunningAsyncFetchers { get; set; } = true;

        public int CachesCount => _cache is Multi<T> mc ? mc.CachesCount : 1;

        protected static ICache<T> Create(
            MemoryCacheOptions options,
            IReadOnlyList<KeyIdentifier> keyIdentifiers)
        {
            return keyIdentifiers.Any()
                ? new Multi<T>(options, keyIdentifiers)
                : new Single<T>(options);
        }

        public virtual T? Get(object key)
        {
            var _ = _cache.TryGetValue(key, out var value);
            return value;
        }

        public virtual void Clear()
        {
            _cache.Dispose();
            _cache = Create(_options, _keyIdentifiers);
        }

        public virtual T GetOrCreate(
            string key,
            Func<T> factory,
            TimeSpan expirationRelativeToUtcNow)
        {
            var found = _cache.TryGetValue(key, out var value);
            if (found) return value!;

            // Item not found.

            value = factory();
            _cache.Set(key, value, expirationRelativeToUtcNow);

            return value;
        }

        public virtual async Task<T> GetOrCreateAsync(
            object key,
            Func<Task<T>> asyncFactory,
            TimeSpan expirationRelativeToUtcNow)
        {
            var found = _cache.TryGetValue(key, out var value);
            if (found) return value!;

            // Item not found.

            value = await FetchAsync(key, asyncFactory);
            _cache.Set(key, value, expirationRelativeToUtcNow);

            return value;
        }

        private async Task<T> FetchAsync(
            object key,
            Func<Task<T>> asyncFactory)
        {
            var runningAsyncFinderKey = $"fetch_async_{key}";

            if (ReuseRunningAsyncFetchers)
            {
                // Check if another async request for the same object is already running

                if (_runningAsyncFetchers.TryGetValue(runningAsyncFinderKey, out var runningTask))
                {
                    // Another async finder for the same key is running!
                    return await runningTask.ConfigureAwait(false);
                }
            }

            // This key has no async finder running.
            var asyncFetcherTask = asyncFactory();

            // Add the newly created task to the dictionary, so that other consumers can reuse it (if configured).
            _runningAsyncFetchers.TryAdd(runningAsyncFinderKey, asyncFetcherTask);

            var result = await asyncFetcherTask.ConfigureAwait(false);

            // Remove the completed task
            _runningAsyncFetchers.TryRemove(runningAsyncFinderKey, out var _);

            return result;
        }
    }
}

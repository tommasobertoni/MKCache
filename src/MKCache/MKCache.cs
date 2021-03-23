using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using MKCache.Abstraction;

namespace MKCache
{
    /// <summary>
    /// Cache items using more than one key.
    /// </summary>
    /// <typeparam name="T">The type of the items in the cache.</typeparam>
    public class MKCache<T>
    {
        /// <summary>
        /// Returns the key to be used for caching the item.
        /// </summary>
        /// <param name="item">The item to be cached</param>
        /// <returns>The cache key.</returns>
        public delegate object KeyIdentifier(T item);

        private ICache<T> _cache;
        private readonly MemoryCacheOptions _options;
        private readonly IReadOnlyList<KeyIdentifier> _keyIdentifiers;
        private readonly ConcurrentDictionary<object, Task<T>> _runningAsyncFetchers = new();

        /// <summary>
        /// Defines the keys that an item can be cached with.
        /// If no <see cref="KeyIdentifier"/>s are provided, the cache will behave as a single-key cache
        /// using the one provided by the caller.
        /// </summary>
        /// <param name="keyIdentifiers">The <see cref="KeyIdentifier"/>s to be used, one for each key.</param>
        public MKCache(params KeyIdentifier[] keyIdentifiers)
            : this(new MemoryCacheOptions(), keyIdentifiers)
        {
        }

        /// <summary>
        /// Defines the keys that an item can be cached with.
        /// If no <see cref="KeyIdentifier"/>s are provided, the cache will behave as a single-key cache
        /// using the one provided by the caller.
        /// </summary>
        /// <param name="options">Options for the memory cache.</param>
        /// <param name="keyIdentifiers">The <see cref="KeyIdentifier"/>s to be used, one for each key.</param>
        public MKCache(MemoryCacheOptions options, params KeyIdentifier[] keyIdentifiers)
            : this(options, keyIdentifiers.AsReadOnly())
        {
        }

        /// <summary>
        /// Defines the keys that an item can be cached with.
        /// If no <see cref="KeyIdentifier"/>s are provided, the cache will behave as a single-key cache
        /// using the one provided by the caller.
        /// </summary>
        /// <param name="options">Options for the memory cache.</param>
        /// <param name="keyIdentifiers">The <see cref="KeyIdentifier"/>s to be used, one for each key.</param>
        public MKCache(
            MemoryCacheOptions options,
            IReadOnlyList<KeyIdentifier> keyIdentifiers)
        {
            _options = options;
            _keyIdentifiers = keyIdentifiers;
            _cache = Create(_options, _keyIdentifiers);
        }

        /// <summary>
        /// Specifies whether to reuse not-yet-completed async fetchers invoked when an item is not in the cache.
        /// Reusing these tasks greatly increases the benefits of the cache when many concurrent requests may happen
        /// on a missing item. Default to: true.
        /// </summary>
        public bool ReuseRunningAsyncFetchers { get; set; } = true;

        /// <summary>
        /// Returns the number of internal caches used: one for each configured key.
        /// </summary>
        public int CachesCount => _cache is Multi<T> mc ? mc.CachesCount : 1;

        private static ICache<T> Create(
            MemoryCacheOptions options,
            IReadOnlyList<KeyIdentifier> keyIdentifiers)
        {
            return keyIdentifiers.Any()
                ? new Multi<T>(options, keyIdentifiers)
                : new Single<T>(options);
        }

        /// <summary>
        /// Try to get an item out of the cache using the specified key.
        /// </summary>
        /// <param name="key">The key of the item.</param>
        /// <returns>The item, if found.</returns>
        public virtual T? Get(object key)
        {
            var _ = _cache.TryGetValue(key, out var value);
            return value;
        }

        /// <summary>
        /// Clears the content of the cache.
        /// </summary>
        public virtual void Clear()
        {
            _cache.Dispose();
            _cache = Create(_options, _keyIdentifiers);
        }

        /// <summary>
        /// Try to get an item out of the cache using the specified key,
        /// if not found the delegate will be invoked and its value will be cached (if not null).
        /// </summary>
        /// <param name="key">The key used to search for the item.</param>
        /// <param name="factory">A delegate returning the item, if not found in the cache.</param>
        /// <param name="expirationRelativeToUtcNow">Defines how long the cached item should last for, relative to UTC now.</param>
        /// <returns>The the newly created or cached item.</returns>
        public virtual T GetOrCreate(
            string key,
            Func<T> factory,
            TimeSpan expirationRelativeToUtcNow)
        {
            var found = _cache.TryGetValue(key, out var value);
            if (found) return value!;

            // Item not found.

            value = factory();

            if (value is not null)
                _cache.Set(key, value, expirationRelativeToUtcNow);

            return value;
        }

        /// <summary>
        /// Try to get an item out of the cache using the specified key,
        /// if not found the delegate will be invoked and its value will be cached (if not null).
        /// </summary>
        /// <param name="key">The key used to search for the item.</param>
        /// <param name="asyncFactory">A delegate returning asynchronously the item, if not found in the cache.</param>
        /// <param name="expirationRelativeToUtcNow">Defines how long the cached item should last for, relative to UTC now.</param>
        /// <returns>The the newly created or cached item.</returns>
        public virtual async Task<T> GetOrCreateAsync(
            object key,
            Func<Task<T>> asyncFactory,
            TimeSpan expirationRelativeToUtcNow)
        {
            var found = _cache.TryGetValue(key, out var value);
            if (found) return value!;

            // Item not found.

            value = await FetchAsync(key, asyncFactory);

            if (value is not null)
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

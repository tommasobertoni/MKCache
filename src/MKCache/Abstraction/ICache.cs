using System;

namespace MKCache.Abstraction
{
    internal interface ICache<T> : IDisposable
    {
        int Count { get; }

        bool TryGetValue(object key, out T? value);

        void Set(object key, T value, TimeSpan expirationRelativeToNow);

        T? Remove(object key);

        void Clear();
    }
}

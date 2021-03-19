using System;

namespace MKCache.Abstraction
{
    public interface ICache<T> : IDisposable
    {
        bool TryGetValue(object key, out T? value);

        void Set(object key, T value, TimeSpan absoluteExpirationRelativeToNow);

        void Reset();
    }
}

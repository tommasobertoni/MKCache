using System;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace MKCache.Tests
{
    public class MultiKeysTests
    {
        private static TimeSpan Expiration => TimeSpan.FromMinutes(5);

        [Fact]
        public void Can_inspect_items_count()
        {
            var cache = new MKCache<Item>(
                x => x.Id,
                x => x.Name);

            Assert.Equal(0, cache.Count);

            cache.GetOrCreate("1", () => new Item(), Expiration);
            Assert.Equal(1, cache.Count);

            cache.GetOrCreate("name", () => new Item(), Expiration);
            Assert.Equal(2, cache.Count);

            cache.Clear();
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void An_item_is_cached_with_multiple_keys()
        {
            var item = new Item();
            var key = item.Id;

            var factoryMock = new Mock<Func<Item>>();
            factoryMock.Setup(factory => factory()).Returns(item);

            var cache = new MKCache<Item>(
                x => x.Id,
                x => x.Name);

            var foundItem = cache.GetOrCreate(key, factoryMock.Object, Expiration);
            factoryMock.Verify(factory => factory(), Times.Once);
            Assert.Same(item, foundItem);

            var cachedItem = cache.GetOrCreate(key, factoryMock.Object, Expiration);
            factoryMock.Verify(factory => factory(), Times.Once);
            Assert.Same(item, cachedItem);

            var cachedItemWithDifferentKey = cache.GetOrCreate(item.Name, factoryMock.Object, Expiration);
            factoryMock.Verify(factory => factory(), Times.Once);
            Assert.Same(item, cachedItemWithDifferentKey);
        }

        [Fact]
        public async Task An_item_is_cached_with_multiple_keys_async()
        {
            var item = new Item();
            var key = item.Id;

            var asyncFactoryMock = new Mock<Func<Task<Item>>>();
            asyncFactoryMock.Setup(asyncFactory => asyncFactory()).Returns(Task.FromResult(item));

            var cache = new MKCache<Item>(
                x => x.Id,
                x => x.Name);

            var foundItem = await cache.GetOrCreateAsync(key, asyncFactoryMock.Object, Expiration);
            asyncFactoryMock.Verify(asyncFactory => asyncFactory(), Times.Once);
            Assert.Same(item, foundItem);

            var cachedItem = await cache.GetOrCreateAsync(key, asyncFactoryMock.Object, Expiration);
            asyncFactoryMock.Verify(asyncFactory => asyncFactory(), Times.Once);
            Assert.Same(item, cachedItem);

            var cachedItemWithDifferentKey = await cache.GetOrCreateAsync(item.Name, asyncFactoryMock.Object, Expiration);
            asyncFactoryMock.Verify(asyncFactory => asyncFactory(), Times.Once);
            Assert.Same(item, cachedItemWithDifferentKey);
        }

        [Fact]
        public void Cache_can_be_cleared()
        {
            var item = new Item();
            var key = item.Id;

            var factoryMock = new Mock<Func<Item>>();
            factoryMock.Setup(factory => factory()).Returns(item);

            var cache = new MKCache<Item>(
                x => x.Id,
                x => x.Name);

            var foundItem = cache.GetOrCreate(key, factoryMock.Object, Expiration);
            factoryMock.Verify(factory => factory(), Times.Once);
            Assert.Same(item, foundItem);

            var cachedItem = cache.GetOrCreate(key, factoryMock.Object, Expiration);
            factoryMock.Verify(factory => factory(), Times.Once);
            Assert.Same(item, cachedItem);

            var cachedItemWithDifferentKey = cache.GetOrCreate(item.Name, factoryMock.Object, Expiration);
            factoryMock.Verify(factory => factory(), Times.Once);
            Assert.Same(item, cachedItemWithDifferentKey);

            cache.Clear();

            var newlyCachedItem = cache.GetOrCreate(key, factoryMock.Object, Expiration);
            factoryMock.Verify(factory => factory(), Times.Exactly(2));
            Assert.Same(item, newlyCachedItem);
        }

        [Fact]
        public void Can_be_disposed()
        {
            var cache = new MKCache<Item>(
               x => x.Id,
               x => x.Name);

            var item = cache.GetOrCreate("1", () => new Item(), Expiration);

            cache.Dispose();

            // Dispose is not Clear.
            Assert.Equal(1, cache.Count);

            // Disposed cache cannot be used anymore.
            Assert.Throws<ObjectDisposedException>(() =>
                cache.GetOrCreate("1", () => new Item(), Expiration));
        }

        [Fact]
        public void Can_set_item()
        {
            var cache = new MKCache<Item>(
               x => x.Id,
               x => x.Name);

            var item = new Item();

            var existingItem = cache.Get(item.Id);
            Assert.Null(existingItem);

            cache.Set("", item, Expiration);

            existingItem = cache.Get(item.Id);
            Assert.NotNull(existingItem);
        }

        [Fact]
        public void Item_can_be_removed()
        {
            var cache = new MKCache<Item>(
               x => x.Id,
               x => x.Name);

            var item = new Item();

            cache.Set(item.Id, item, Expiration);

            var existingItem = cache.Get(item.Id);
            Assert.NotNull(existingItem);

            existingItem = cache.Remove(item.Id);
            Assert.NotNull(existingItem);

            existingItem = cache.Get(item.Id);
            Assert.Null(existingItem);
        }

        [Fact]
        public void Removing_missing_item_does_not_throw()
        {
            var cache = new MKCache<Item>(
               x => x.Id,
               x => x.Name);

            Assert.Equal(0, cache.Count);

            var missing = cache.Remove("foo");
            Assert.Null(missing);
        }
    }
}

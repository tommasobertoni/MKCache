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
                // Multi-key cache rules.
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
                // Multi-key cache rules.
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
                // Multi-key cache rules.
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
    }
}

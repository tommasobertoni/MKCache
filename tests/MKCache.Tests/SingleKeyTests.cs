using System;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace MKCache.Tests
{
    public class SingleKeyTests
    {
        private static TimeSpan Expiration => TimeSpan.FromMinutes(5);

        [Fact]
        public void Can_inspect_items_count()
        {
            var cache = new MKCache<int>();
            Assert.Equal(0, cache.Count);

            cache.GetOrCreate("1", () => 1, Expiration);
            Assert.Equal(1, cache.Count);

            cache.GetOrCreate("2", () => 2, Expiration);
            Assert.Equal(2, cache.Count);

            cache.Clear();
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void An_item_is_cached_with_the_default_key()
        {
            var item = new Item();
            var key = item.Id;

            var factoryMock = new Mock<Func<Item>>();
            factoryMock.Setup(factory => factory()).Returns(item);

            var cache = new MKCache<Item>();

            var foundItem = cache.GetOrCreate(key, factoryMock.Object, Expiration);
            factoryMock.Verify(factory => factory(), Times.Once);
            Assert.Same(item, foundItem);

            var cachedItem = cache.GetOrCreate(key, factoryMock.Object, Expiration);
            factoryMock.Verify(factory => factory(), Times.Once);
            Assert.Same(item, cachedItem);

            var _ = cache.GetOrCreate(item.Name, factoryMock.Object, Expiration);
            // Delegate invoked a second time, because the item was not found
            factoryMock.Verify(factory => factory(), Times.Exactly(2));
        }

        [Fact]
        public async Task An_item_is_cached_with_the_default_key_async()
        {
            var item = new Item();
            var key = item.Id;

            var asyncFactoryMock = new Mock<Func<Task<Item>>>();
            asyncFactoryMock.Setup(asyncFactory => asyncFactory()).Returns(Task.FromResult(item));

            var cache = new MKCache<Item>();

            var foundItem = await cache.GetOrCreateAsync(key, asyncFactoryMock.Object, Expiration);
            asyncFactoryMock.Verify(asyncFactory => asyncFactory(), Times.Once);
            Assert.Same(item, foundItem);

            var cachedItem = await cache.GetOrCreateAsync(key, asyncFactoryMock.Object, Expiration);
            asyncFactoryMock.Verify(asyncFactory => asyncFactory(), Times.Once);
            Assert.Same(item, cachedItem);

            var _ = await cache.GetOrCreateAsync(item.Name, asyncFactoryMock.Object, Expiration);
            // Delegate invoked a second time, because the item was not found
            asyncFactoryMock.Verify(asyncFactory => asyncFactory(), Times.Exactly(2));
        }

        [Fact]
        public async Task Default_key_cache_can_be_cleared()
        {
            var item = new Item();
            var key = item.Id;

            var asyncFactoryMock = new Mock<Func<Task<Item>>>();
            asyncFactoryMock.Setup(asyncFactory => asyncFactory()).Returns(Task.FromResult(item));

            var cache = new MKCache<Item>();

            var foundItem = await cache.GetOrCreateAsync(key, asyncFactoryMock.Object, Expiration);
            asyncFactoryMock.Verify(asyncFactory => asyncFactory(), Times.Once);
            Assert.Same(item, foundItem);

            var cachedItem = await cache.GetOrCreateAsync(key, asyncFactoryMock.Object, Expiration);
            asyncFactoryMock.Verify(asyncFactory => asyncFactory(), Times.Once);
            Assert.Same(item, cachedItem);

            cache.Clear();

            var _ = await cache.GetOrCreateAsync(key, asyncFactoryMock.Object, Expiration);
            asyncFactoryMock.Verify(asyncFactory => asyncFactory(), Times.Exactly(2));
        }

        [Fact]
        public void An_item_is_cached_with_a_single_key()
        {
            var item = new Item();

            var factoryMock = new Mock<Func<Item>>();
            factoryMock.Setup(factory => factory()).Returns(item);

            var cache = new MKCache<Item>(
                x => x.Name /* Name property is the key */);

            _ = cache.GetOrCreate(item.Id, factoryMock.Object, Expiration);
            factoryMock.Verify(factory => factory(), Times.Once);

            var cachedItem = cache.GetOrCreate(item.Name, factoryMock.Object, Expiration);
            factoryMock.Verify(factory => factory(), Times.Once);
            Assert.Same(item, cachedItem);

            _ = cache.GetOrCreate(item.Id, factoryMock.Object, Expiration);
            // Delegate invoked a second time, because the item was not found using its Id
            factoryMock.Verify(factory => factory(), Times.Exactly(2));
        }

        [Fact]
        public async Task An_item_is_cached_with_a_single_key_async()
        {
            var item = new Item();

            var asyncFactoryMock = new Mock<Func<Task<Item>>>();
            asyncFactoryMock.Setup(asyncFactory => asyncFactory()).Returns(Task.FromResult(item));

            var cache = new MKCache<Item>(
                x => x.Name /* Name property is the key */);

            _ = await cache.GetOrCreateAsync(item.Id, asyncFactoryMock.Object, Expiration);
            asyncFactoryMock.Verify(asyncFactory => asyncFactory(), Times.Once);

            var cachedItem = await cache.GetOrCreateAsync(item.Name, asyncFactoryMock.Object, Expiration);
            asyncFactoryMock.Verify(asyncFactory => asyncFactory(), Times.Once);
            Assert.Same(item, cachedItem);

            _ = await cache.GetOrCreateAsync(item.Id, asyncFactoryMock.Object, Expiration);
            // Delegate invoked a second time, because the item was not found using its Id
            asyncFactoryMock.Verify(asyncFactory => asyncFactory(), Times.Exactly(2));
        }

        [Fact]
        public void Cache_can_be_cleared()
        {
            var item = new Item();
            var key = item.Id;

            var factoryMock = new Mock<Func<Item>>();
            factoryMock.Setup(factory => factory()).Returns(item);

            var cache = new MKCache<Item>();

            var foundItem = cache.GetOrCreate(key, factoryMock.Object, Expiration);
            factoryMock.Verify(factory => factory(), Times.Once);
            Assert.Same(item, foundItem);

            var cachedItem = cache.GetOrCreate(key, factoryMock.Object, Expiration);
            factoryMock.Verify(factory => factory(), Times.Once);
            Assert.Same(item, cachedItem);

            cache.Clear();

            var newlyCachedItem = cache.GetOrCreate(key, factoryMock.Object, Expiration);
            factoryMock.Verify(factory => factory(), Times.Exactly(2));
            Assert.Same(item, newlyCachedItem);
        }
    }
}

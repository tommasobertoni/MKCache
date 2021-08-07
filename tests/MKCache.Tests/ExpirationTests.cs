using System;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace MKCache.Tests
{
    public class ExpirationTests
    {
        [Fact]
        public async Task Cache_item_expires()
        {
            var cache = new MKCache<Item>();

            var item = new Item();

            var factoryMock = new Mock<Func<Item>>();
            factoryMock.Setup(factory => factory()).Returns(item);

            var expirySpan = TimeSpan.FromSeconds(1);

            var foundItem = cache.GetOrCreate(item.Id, factoryMock.Object, expirySpan);
            factoryMock.Verify(factory => factory(), Times.Once);
            Assert.Same(item, foundItem);

            var cachedItem = cache.GetOrCreate(item.Id, () => new Item(), expirySpan);
            Assert.Same(item, cachedItem);

            await Task.Delay(expirySpan * 3);

            var newItem = cache.GetOrCreate(item.Id, factoryMock.Object, expirySpan);
            factoryMock.Verify(factory => factory(), Times.Exactly(2));
        }

        [Fact]
        public async Task Cache_item_expires_async()
        {
            var cache = new MKCache<Item>();

            var item = new Item();

            var asyncFactoryMock = new Mock<Func<Task<Item>>>();
            asyncFactoryMock.Setup(asyncFactory => asyncFactory()).Returns(Task.FromResult(item));

            var expirySpan = TimeSpan.FromSeconds(1);

            var foundItem = await cache.GetOrCreateAsync(item.Id, asyncFactoryMock.Object, expirySpan);
            asyncFactoryMock.Verify(factory => factory(), Times.Once);
            Assert.Same(item, foundItem);

            var cachedItem = await cache.GetOrCreateAsync(item.Id, () => Task.FromResult(new Item()), expirySpan);
            Assert.Same(item, cachedItem);

            await Task.Delay(expirySpan * 3);

            var newItem = await cache.GetOrCreateAsync(item.Id, asyncFactoryMock.Object, expirySpan);
            asyncFactoryMock.Verify(factory => factory(), Times.Exactly(2));
        }

        [Fact]
        public async Task Single_key_item_expires()
        {
            var cache = new MKCache<Item>();
            var key = "foo";

            var existingItem = cache.Get(key);
            Assert.Null(existingItem);

            cache.Set(key, new Item(), TimeSpan.FromMilliseconds(100));

            existingItem = cache.Get(key);
            Assert.NotNull(existingItem);

            await Task.Delay(3000);

            existingItem = cache.Get(key);
            Assert.Null(existingItem);
        }

        [Fact]
        public async Task Multi_key_item_expires()
        {
            var cache = new MKCache<Item>(
               x => x.Id,
               x => x.Name);

            var item = new Item();

            var existingItem = cache.Get(item.Id);
            Assert.Null(existingItem);

            cache.Set("", item, TimeSpan.FromMilliseconds(100));

            existingItem = cache.Get(item.Id);
            Assert.NotNull(existingItem);

            await Task.Delay(3000);

            existingItem = cache.Get(item.Id);
            Assert.Null(existingItem);
        }
    }
}

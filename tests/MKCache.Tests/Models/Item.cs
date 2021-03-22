using System;

namespace MKCache.Tests
{
    public class Item
    {
        public Item()
            : this(name: Guid.NewGuid().ToString("n"))
        {
        }

        public Item(string name)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Count = DateTime.UtcNow.Second;
        }

        public string Id { get; }

        public string Name { get; }

        public int Count { get; }
    }
}

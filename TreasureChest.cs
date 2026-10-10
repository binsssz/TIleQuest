using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    public sealed class TreasureChest
    {
        private readonly List<Item> _items;

        public IReadOnlyList<Point> Tiles { get; }
        public IReadOnlyList<Item> Items => _items;

        public TreasureChest(Point topTile, IEnumerable<Item> items)
        {
            Tiles = new[]
            {
                topTile,
                new Point(topTile.X, topTile.Y + 1),
            };
            _items = new List<Item>(items);
        }

        public Item? TakeItem(int index)
        {
            if (index < 0 || index >= _items.Count)
            {
                return null;
            }

            Item item = _items[index];
            _items.RemoveAt(index);
            return item;
        }
    }
}

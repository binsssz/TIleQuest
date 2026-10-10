using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    public sealed class TreasureChest
    {
        private readonly List<Item> _items;

        public IReadOnlyList<Point> Tiles { get; }
        public IReadOnlyList<Item> Items => _items;

        // Chests are 2 tiles wide and 1 tall, matching the sprites in dungeon.png.
        public TreasureChest(Point leftTile, IEnumerable<Item> items)
        {
            Tiles = new[]
            {
                leftTile,
                new Point(leftTile.X + 1, leftTile.Y),
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
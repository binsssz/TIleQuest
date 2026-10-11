using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    // The village hearth: a statue on a round stone platform that the player
    // wakes to start a night and the goblins try to tear down. Occupies
    // Size x Size tiles (the statue's base, blocked for movement) and has hit
    // points that persist from night to night.
    public class Hearth
    {
        public const int DefaultMaxHealth = 100;

        private readonly Point[] _tiles;

        public Point Position { get; }   // top-left tile of the footprint
        public int Size { get; }
        public int Health { get; private set; }
        public int MaxHealth { get; }
        public bool IsDestroyed => Health <= 0;
        public IReadOnlyList<Point> Tiles => _tiles;

        public Hearth(Point topLeft, int size, int maxHealth = DefaultMaxHealth)
        {
            Position = topLeft;
            Size = size;
            MaxHealth = maxHealth;
            Health = maxHealth;

            _tiles = new Point[size * size];
            for (int dy = 0; dy < size; dy++)
            {
                for (int dx = 0; dx < size; dx++)
                {
                    _tiles[dy * size + dx] = new Point(topLeft.X + dx, topLeft.Y + dy);
                }
            }
        }

        public void TakeDamage(int amount)
        {
            Health = Math.Max(0, Health - amount);
        }

        public void RestoreFully()
        {
            Health = MaxHealth;
        }

        public bool Contains(Point tile)
        {
            return Array.IndexOf(_tiles, tile) >= 0;
        }

        // True when tile touches the footprint from outside: orthogonally by
        // default, or including the four corners when includeDiagonals is set.
        public bool IsAdjacentTo(Point tile, bool includeDiagonals = false)
        {
            if (Contains(tile))
            {
                return false;
            }

            foreach (Point hearthTile in _tiles)
            {
                int dx = Math.Abs(hearthTile.X - tile.X);
                int dy = Math.Abs(hearthTile.Y - tile.Y);
                bool touching = includeDiagonals ? Math.Max(dx, dy) == 1 : dx + dy == 1;
                if (touching)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TileQuest
{
    public sealed class DrawRock
    {
        private const int SourceTileSize = TileSprites.GridSize;
        private static readonly float[] SizeVariants = { 0.75f, 1.15f, 1.6f };
        private static readonly Rectangle[] Variants = TileSprites.Rocks;

        private readonly Texture2D _texture;
        private readonly Rectangle _source;
        private readonly Point _tile;
        private readonly int _tileSize;

        private DrawRock(Texture2D texture, Rectangle source, Point tile, int tileSize)
        {
            _texture = texture;
            _source = source;
            _tile = tile;
            _tileSize = tileSize;
        }

        private static int Hash(Point tile)
        {
            return unchecked(tile.X * 19349663 ^ tile.Y * 73856093);
        }

        private static Rectangle SourceFor(Point tile)
        {
            return Variants[(Hash(tile) & 0x7fffffff) % Variants.Length];
        }

        private static float ScaleFor(Point tile)
        {
            return SizeVariants[(Hash(tile) >> 8 & 0x7fffffff) % SizeVariants.Length];
        }

        public static Rectangle GetBounds(Point tile, int tileSize)
        {
            float scale = tileSize / (float)SourceTileSize * ScaleFor(tile);
            int size = (int)Math.Round(SourceTileSize * scale);
            int left = tile.X * tileSize + tileSize / 2 - size / 2;
            int bottom = (tile.Y + 1) * tileSize;
            return new Rectangle(left, bottom - size, size, size);
        }

        public static DrawRock[] CreateFor(TileMap map, Texture2D texture, int tileSize)
        {
            var rocks = new List<DrawRock>();
            foreach (var (position, tile) in map.AllTiles())
            {
                if (tile == TileType.Rock)
                {
                    rocks.Add(new DrawRock(texture, SourceFor(position), position, tileSize));
                }
            }
            return rocks.ToArray();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, GetBounds(_tile, _tileSize), _source, Color.White);
        }
    }
}
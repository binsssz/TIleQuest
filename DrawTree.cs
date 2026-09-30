using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TileQuest
{
    // One DrawTree per Tree tile, and ForestGenerator only places a tree where
    // its sprite won't overlap another tree's sprite. So every tree you see is
    // a single, separate sprite standing on exactly one blocked tile, and every
    // blocked tree tile has a sprite on it.
    //
    // Only the narrow tree sprites are used (22px wide on the sheet = 1.4
    // tiles), so the sprite is essentially the size of the one tile it blocks.
    public sealed class DrawTree
    {
        // The sheet is drawn on a 16px grid; TileSize scales it up.
        public const int SourceTileSize = TileSprites.GridSize;

        // Sprite coordinates live in TileSprites.cs.
        private static readonly Rectangle[] Variants = TileSprites.Trees;

        private static readonly float[] SizeVariants = { 0.8f, 1.2f, 1.6f };

        private readonly Texture2D _texture;
        private readonly Rectangle _source;
        private readonly Point _anchorTile;
        private readonly int _tileSize;

        private DrawTree(Texture2D texture, Rectangle source, Point anchorTile, int tileSize)
        {
            _texture = texture;
            _source = source;
            _anchorTile = anchorTile;
            _tileSize = tileSize;
        }

        // Y pixel of the bottom of the tree's tile. Game1 compares this with
        // the player's feet to decide who is drawn in front.
        public int BaseY => (_anchorTile.Y + 1) * _tileSize;

        // Which sprite a tree tile uses. Depends only on the position, so a
        // tree always looks the same and the generator can ask how big it
        // will be before placing it.
        private static Rectangle VariantFor(Point tile)
        {
            int hash = unchecked(tile.X * 73856093 ^ tile.Y * 19349663);
            return Variants[(hash & 0x7fffffff) % Variants.Length];
        }

        private static float ScaleFor(Point tile)
        {
            int hash = unchecked(tile.X * 83492791 ^ tile.Y * 29765797);
            return SizeVariants[(hash & 0x7fffffff) % SizeVariants.Length];
        }

        // Where the tree's sprite lands, in sheet pixels (16px per tile),
        // with the sprite centred on the tile and its bottom on the tile's
        // bottom edge. Used by ForestGenerator to keep trees from overlapping.
        public static Rectangle GetBounds(Point tile)
        {
            Rectangle source = VariantFor(tile);
            float sizeScale = ScaleFor(tile);
            int width = (int)Math.Round(source.Width * sizeScale);
            int height = (int)Math.Round(source.Height * sizeScale);
            int x = tile.X * SourceTileSize + SourceTileSize / 2 - width / 2;
            int y = (tile.Y + 1) * SourceTileSize - height;
            return new Rectangle(x, y, width, height);
        }

        public static Rectangle GetCollisionBounds(Point tile, int tileSize)
        {
            Rectangle source = VariantFor(tile);
            float scale = ScaleFor(tile) * tileSize / (float)SourceTileSize;
            int width = (int)Math.Round(source.Width * 0.4f * scale);
            int height = (int)Math.Round(7f * scale);
            int left = tile.X * tileSize + tileSize / 2 - width / 2;
            int bottom = (tile.Y + 1) * tileSize;
            return new Rectangle(left, bottom - height, width, height);
        }

        public static DrawTree[] CreateForest(TileMap map, Texture2D texture, int tileSize)
        {
            var trees = new List<DrawTree>();
            foreach (var (position, tile) in map.AllTiles())
            {
                if (tile == TileType.Tree)
                {
                    trees.Add(new DrawTree(texture, VariantFor(position), position, tileSize));
                }
            }

            // Back-to-front (top row first). Game1.Draw relies on this order
            // to draw the player between the trees behind and in front of them.
            trees.Sort((a, b) =>
            {
                int byRow = a._anchorTile.Y.CompareTo(b._anchorTile.Y);
                return byRow != 0 ? byRow : a._anchorTile.X.CompareTo(b._anchorTile.X);
            });

            return trees.ToArray();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            float scale = _tileSize / (float)SourceTileSize * ScaleFor(_anchorTile);
            int width = (int)Math.Round(_source.Width * scale);
            int height = (int)Math.Round(_source.Height * scale);
            int anchorX = _anchorTile.X * _tileSize + _tileSize / 2;
            int anchorY = (_anchorTile.Y + 1) * _tileSize;
            var destination = new Rectangle(anchorX - width / 2, anchorY - height, width, height);

            spriteBatch.Draw(_texture, destination, _source, Color.White);
        }
    }
}
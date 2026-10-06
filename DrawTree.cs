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
    // Each tree type uses a separate sprite sheet and is normalized to the same
    // approximate height before its size variation is applied.
    public sealed class DrawTree : IDepthSorted
    {
        // Source sheets use sprites sized to a 16px tile grid.
        public const int SourceTileSize = TileSprites.GridSize;

        private static readonly Rectangle Tree1Source = new(0, 0, 48, 96);
        private static readonly Rectangle Tree2Source = new(0, 0, 80, 128);
        private static readonly Rectangle Tree3Source = new(0, 0, 112, 160);

        private static readonly float[] SizeVariants = { 0.8f, 1.2f, 1.6f };
        private const float TreeSizeScale = 1.35f;
        private static readonly Rectangle[] TreeSources =
        {
            Tree1Source,
            Tree2Source,
            Tree3Source,
            new(0, 80, 48, 80),
            new(0, 112, 64, 112),
            new(0, 160, 96, 160),
            new(0, 0, 64, 144),
            new(0, 0, 128, 256),
        };

        private readonly Texture2D _texture;
        private readonly Rectangle _source;
        private readonly TileType _type;
        private readonly Point _anchorTile;
        private readonly int _tileSize;

        private DrawTree(Texture2D texture, Rectangle source, TileType type, Point anchorTile, int tileSize)
        {
            _texture = texture;
            _source = source;
            _type = type;
            _anchorTile = anchorTile;
            _tileSize = tileSize;
        }

        // Y pixel of the bottom of the tree's tile. Game1 compares this with
        // the player's feet to decide who is drawn in front.
        public int BaseY => (_anchorTile.Y + 1) * _tileSize;

        // Which sprite a tree tile uses. Depends only on the position, so a
        // tree always looks the same and the generator can ask how big it
        // will be before placing it.
        public static bool IsTreeType(TileType type)
        {
            return type >= TileType.Tree && type <= TileType.Tree8;
        }

        private static Rectangle SourceFor(TileType type)
        {
            return TreeSources[(int)type - (int)TileType.Tree];
        }

        private static Texture2D TextureFor(TileType type, Texture2D[] textures)
        {
            return textures[(int)type - (int)TileType.Tree];
        }

        private static float ScaleFor(Point tile)
        {
            int hash = unchecked(tile.X * 83492791 ^ tile.Y * 29765797);
            return SizeVariants[(hash & 0x7fffffff) % SizeVariants.Length];
        }

        private static float NormalizeScale(TileType type) => 54f * TreeSizeScale / SourceFor(type).Height;

        // Used by ForestGenerator to avoid overlapping tree sprites.
        public static Rectangle GetBounds(Point tile, TileType type)
        {
            Rectangle source = SourceFor(type);
            float scale = NormalizeScale(type) * ScaleFor(tile);
            int width = (int)Math.Round(source.Width * scale);
            int height = (int)Math.Round(source.Height * scale);
            int x = tile.X * SourceTileSize + SourceTileSize / 2 - width / 2;
            int y = (tile.Y + 1) * SourceTileSize - height;
            return new Rectangle(x, y, width, height);
        }

        public static Rectangle GetCollisionBounds(Point tile, TileType type, int tileSize)
        {
            Rectangle source = SourceFor(type);
            float scale = NormalizeScale(type) * ScaleFor(tile) * tileSize / (float)SourceTileSize;
            int width = (int)Math.Round(source.Width * 0.4f * scale);
            int height = (int)Math.Round(7f * scale);
            int left = tile.X * tileSize + tileSize / 2 - width / 2;
            int bottom = (tile.Y + 1) * tileSize;
            return new Rectangle(left, bottom - height, width, height);
        }

        public static DrawTree[] CreateForest(TileMap map, Texture2D[] treeTextures, int tileSize)
        {
            var trees = new List<DrawTree>();
            foreach (var (position, tile) in map.AllTiles())
            {
                if (IsTreeType(tile))
                {
                    trees.Add(new DrawTree(TextureFor(tile, treeTextures), SourceFor(tile), tile, position, tileSize));
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

        public static void DrawBottomCanopy(
            SpriteBatch spriteBatch, Texture2D[] treeTextures, int mapWidth, int mapHeight, int tileSize)
        {
            for (int row = 0; row < 2; row++)
            {
                int anchorY = mapHeight + 1 + row * 2;
                int x = row == 0 ? -1 : 1;
                int lastX = int.MinValue;
                while (x <= mapWidth)
                {
                    DrawCanopyTree(spriteBatch, treeTextures, x, anchorY, mapWidth, tileSize);
                    lastX = x;
                    int spacingHash = unchecked(x * 19349663 ^ anchorY * 73856093) & 0x7fffffff;
                    x += 3 + spacingHash % 3;
                }

                if (lastX < mapWidth - 2)
                {
                    DrawCanopyTree(spriteBatch, treeTextures, mapWidth, anchorY, mapWidth, tileSize);
                }
            }
        }

        public static void DrawLeftCornerCanopyOverlay(
            SpriteBatch spriteBatch, Texture2D[] treeTextures, int mapWidth, int mapHeight, int tileSize)
        {
            DrawCanopyFoliage(spriteBatch, treeTextures[3], new Rectangle(0, 80, 48, 48),
                -tileSize, mapHeight * tileSize - tileSize * 2, tileSize * 3, tileSize * 2);
            DrawCanopyFoliage(spriteBatch, treeTextures[4], new Rectangle(0, 112, 64, 64),
                tileSize, mapHeight * tileSize - tileSize * 2, tileSize * 3, tileSize * 2);
            DrawCanopyFoliage(spriteBatch, treeTextures[5], new Rectangle(0, 160, 96, 80),
                tileSize * 4, mapHeight * tileSize - tileSize * 2, tileSize * 4, tileSize * 2);
        }

        private static void DrawCanopyFoliage(
            SpriteBatch spriteBatch, Texture2D texture, Rectangle source, int x, int y, int width, int height)
        {
            spriteBatch.Draw(texture, new Rectangle(x, y, width, height), source, Color.White);
        }

        private static void DrawCanopyTree(
            SpriteBatch spriteBatch, Texture2D[] treeTextures, int x, int anchorY, int mapWidth, int tileSize)
        {
            var anchor = new Point(x, anchorY);
            int hash = unchecked(x * 83492791 ^ anchorY * 29765797) & 0x7fffffff;
            int treeIndex = hash % treeTextures.Length;
            Rectangle source = TreeSources[treeIndex];
            float sizeScale = ScaleFor(anchor);
            if (x < mapWidth / 3)
            {
                sizeScale = Math.Max(sizeScale, 1.2f);
            }
            float scale = tileSize / (float)SourceTileSize *
                          (54f * TreeSizeScale / source.Height) * sizeScale;
            int width = (int)Math.Round(source.Width * scale);
            int height = (int)Math.Round(source.Height * scale);
            int anchorX = x * tileSize + tileSize / 2;
            int anchorYInPixels = (anchorY + 1) * tileSize;
            var destination = new Rectangle(anchorX - width / 2, anchorYInPixels - height, width, height);
            spriteBatch.Draw(treeTextures[treeIndex], destination, source, Color.White);
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            Draw(spriteBatch);
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            float scale = _tileSize / (float)SourceTileSize * NormalizeScale(_type) * ScaleFor(_anchorTile);
            int width = (int)Math.Round(_source.Width * scale);
            int height = (int)Math.Round(_source.Height * scale);
            int anchorX = _anchorTile.X * _tileSize + _tileSize / 2;
            int anchorY = (_anchorTile.Y + 1) * _tileSize;
            var destination = new Rectangle(anchorX - width / 2, anchorY - height, width, height);

            spriteBatch.Draw(_texture, destination, _source, Color.White);
        }

        public void DrawShadow(SpriteBatch spriteBatch, Texture2D shadowTexture)
        {
            int width = (int)Math.Round(_tileSize * 1.5f);
            int height = (int)Math.Round(_tileSize * 0.6f);
            int anchorX = _anchorTile.X * _tileSize + _tileSize / 2;
            int y = BaseY - height / 2;
            var destination = new Rectangle(anchorX - width / 2, y, width, height);
            spriteBatch.Draw(shadowTexture, destination, TileSprites.TreeShadow, Color.White);
        }
    }
}
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TileQuest
{
    // Anything that has to be drawn in front of or behind the player depending
    // on how far down the screen its base is. Game1 keeps one list of these
    // (trees and props together), sorted by BaseY, and draws the player in the
    // right gap - see Game1.DrawTreesAndPlayer.
    public interface IDepthSorted
    {
        // Y pixel of the bottom of the tile the object stands on.
        int BaseY { get; }

        void Draw(SpriteBatch spriteBatch);
    }

    // One sprite for one VillageProp. The tile is fully blocked (TileType.Prop
    // is not walkable), so the player can never stand "inside" it; the sprite
    // just reaches up and out past the tile, like a tree canopy.
    public sealed class DrawProp : IDepthSorted
    {
        private readonly Texture2D _texture;
        private readonly Rectangle _source;
        private readonly Point _tile;
        private readonly int _tileSize;

        private DrawProp(Texture2D texture, Rectangle source, Point tile, int tileSize)
        {
            _texture = texture;
            _source = source;
            _tile = tile;
            _tileSize = tileSize;
        }

        public int BaseY => (_tile.Y + 1) * _tileSize;

        public static DrawProp[] CreateFor(
            TileMap map, IReadOnlyDictionary<PropSheet, Texture2D> sheets, int tileSize)
        {
            var props = new List<DrawProp>();
            foreach (VillageProp prop in map.VillageProps)
            {
                Texture2D texture = sheets[PropCatalog.SheetOf(prop.Kind)];
                props.Add(new DrawProp(texture, PropCatalog.SourceFor(prop.Kind, prop.Tile), prop.Tile, tileSize));
            }
            return props.ToArray();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            float scale = _tileSize / (float)TileSprites.GridSize;
            int width = (int)Math.Round(_source.Width * scale);
            int height = (int)Math.Round(_source.Height * scale);
            int x = _tile.X * _tileSize + _tileSize / 2 - width / 2;
            var destination = new Rectangle(x, BaseY - height, width, height);
            spriteBatch.Draw(_texture, destination, _source, Color.White);
        }
    }
}

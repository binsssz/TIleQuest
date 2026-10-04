using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TileQuest
{
    // Adds a rough, irregular edge to the village's dirt roads. This is purely
    // visual: it never changes a tile type, so walkability, spawn and the
    // connectivity check behave exactly as before.
    //
    // Stone pixels shave a deterministic, stepped profile into each exposed
    // road edge. The profile is based on world-pixel coordinates, so it stays
    // continuous across tile boundaries. Dirt caps still fill inside bends.
    public sealed class PathCorners
    {
        // Set to false to go back to square roads.
        public const bool Enabled = true;

        // Depth variation along the rough edge, in source pixels.
        private const int MinEdgeDepth = 1;
        private const int EdgeDepthVariation = 3;

        public const int Radius = 6;

        // Order matches the arrays below: NW, NE, SW, SE.
        private static readonly int[] CornerDx = { -1, 1, -1, 1 };
        private static readonly int[] CornerDy = { -1, -1, 1, 1 };
        // Order: north, east, south, west.
        private static readonly int[] SideDx = { 0, 1, 0, -1 };
        private static readonly int[] SideDy = { -1, 0, 1, 0 };

        private readonly Texture2D[] _dirtCaps = new Texture2D[4];
        private readonly Texture2D[] _grassCaps = new Texture2D[4];
        private readonly Texture2D[] _stoneCaps = new Texture2D[4];
        private readonly Dictionary<(int X, int Y, int Side), Texture2D> _roughEdges = new();
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Color[] _sheet;
        private readonly int _sheetWidth;

        public PathCorners(GraphicsDevice graphicsDevice, Texture2D floorTiles)
        {
            _graphicsDevice = graphicsDevice;
            _sheet = new Color[floorTiles.Width * floorTiles.Height];
            _sheetWidth = floorTiles.Width;
            floorTiles.GetData(_sheet);

            for (int corner = 0; corner < 4; corner++)
            {
                _dirtCaps[corner] = BuildCap(
                    graphicsDevice, _sheet, _sheetWidth, TileSprites.BrightCrackedRoad, corner);
                _grassCaps[corner] = BuildCap(
                    graphicsDevice, _sheet, _sheetWidth, TileSprites.Grass, corner);
                _stoneCaps[corner] = BuildCap(
                    graphicsDevice, _sheet, _sheetWidth, TileSprites.Stone, corner);
            }
        }

        // A 16x16 texture that is transparent except in one corner, where it
        // copies `source` for every pixel lying outside a quarter circle.
        private static Texture2D BuildCap(
            GraphicsDevice graphicsDevice, Color[] sheet, int sheetWidth, Rectangle source, int corner)
        {
            int size = TileSprites.GridSize;
            int radius = Math.Clamp(Radius, 1, size / 2);
            bool east = CornerDx[corner] > 0;
            bool south = CornerDy[corner] > 0;

            int squareX = east ? size - radius : 0;
            int squareY = south ? size - radius : 0;
            float centreX = east ? size - radius : radius;
            float centreY = south ? size - radius : radius;

            var pixels = new Color[size * size]; // default Color is fully transparent
            for (int py = squareY; py < squareY + radius; py++)
            {
                for (int px = squareX; px < squareX + radius; px++)
                {
                    float dx = px + 0.5f - centreX;
                    float dy = py + 0.5f - centreY;
                    if (dx * dx + dy * dy > radius * radius)
                    {
                        pixels[py * size + px] = sheet[(source.Y + py) * sheetWidth + source.X + px];
                    }
                }
            }

            var cap = new Texture2D(graphicsDevice, size, size);
            cap.SetData(pixels);
            return cap;
        }

        // Call after every floor tile has been drawn. Village map only.
        public void Draw(SpriteBatch spriteBatch, TileMap map, int tileSize)
        {
            if (!Enabled || map.MapType != WorldMapType.Village)
            {
                return;
            }

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    bool isDirt = IsDirt(map, x, y);
                    if (isDirt)
                    {
                        for (int side = 0; side < SideDx.Length; side++)
                        {
                            if (IsDirt(map, x + SideDx[side], y + SideDy[side]))
                            {
                                continue;
                            }

                            Texture2D edge = GetRoughEdge(x, y, side);
                            spriteBatch.Draw(
                                edge,
                                new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize),
                                Color.White);
                        }
                    }

                    for (int corner = 0; corner < 4; corner++)
                    {
                        int dx = CornerDx[corner];
                        int dy = CornerDy[corner];
                        bool sideXIsDirt = IsDirt(map, x + dx, y);
                        bool sideYIsDirt = IsDirt(map, x, y + dy);

                        if (isDirt)
                        {
                            if (sideXIsDirt && sideYIsDirt && IsDirt(map, x + dx, y + dy))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (sideXIsDirt && sideYIsDirt && IsDirt(map, x + dx, y + dy))
                            {
                                spriteBatch.Draw(
                                    _dirtCaps[corner],
                                    new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize),
                                    Color.White);
                            }

                            bool isStone = IsStone(map, x, y);
                            bool sideXIsStone = IsStone(map, x + dx, y);
                            bool sideYIsStone = IsStone(map, x, y + dy);

                            if (isStone &&
                                !sideXIsStone && !sideYIsStone &&
                                !sideXIsDirt && !sideYIsDirt)
                            {
                                spriteBatch.Draw(
                                    _grassCaps[corner],
                                    new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize),
                                    Color.White);
                            }
                            else if (!isStone &&
                                sideXIsStone && sideYIsStone &&
                                IsStone(map, x + dx, y + dy))
                            {
                                spriteBatch.Draw(
                                    _stoneCaps[corner],
                                    new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize),
                                    Color.White);
                            }

                            continue;
                        }
                    }
                }
            }
        }

        private Texture2D GetRoughEdge(int x, int y, int side)
        {
            var key = (x, y, side);
            if (!_roughEdges.TryGetValue(key, out Texture2D? edge))
            {
                edge = BuildRoughEdge(x, y, side);
                _roughEdges.Add(key, edge);
            }

            return edge;
        }

        private Texture2D BuildRoughEdge(int tileX, int tileY, int side)
        {
            int size = TileSprites.GridSize;
            var pixels = new Color[size * size];
            bool horizontal = side == 0 || side == 2;
            int edgeCoordinate = side switch
            {
                0 => tileY * size,
                1 => (tileX + 1) * size,
                2 => (tileY + 1) * size,
                _ => tileX * size,
            };

            for (int along = 0; along < size; along++)
            {
                int worldAlong = (horizontal ? tileX : tileY) * size + along;
                int depth = GetEdgeDepth(worldAlong, edgeCoordinate, horizontal);
                for (int inset = 0; inset < depth; inset++)
                {
                    int px;
                    int py;
                    switch (side)
                    {
                        case 0:
                            px = along;
                            py = inset;
                            break;
                        case 1:
                            px = size - 1 - inset;
                            py = along;
                            break;
                        case 2:
                            px = along;
                            py = size - 1 - inset;
                            break;
                        default:
                            px = inset;
                            py = along;
                            break;
                    }

                    pixels[py * size + px] = _sheet[
                        (TileSprites.Stone.Y + py) * _sheetWidth + TileSprites.Stone.X + px];
                }
            }

            var edge = new Texture2D(_graphicsDevice, size, size);
            edge.SetData(pixels);
            return edge;
        }

        private static int GetEdgeDepth(int worldAlong, int edgeCoordinate, bool horizontal)
        {
            int segment = worldAlong / 4;
            uint hash = unchecked(
                (uint)segment * 374761393u ^
                (uint)edgeCoordinate * 668265263u ^
                (horizontal ? 2246822519u : 3266489917u));
            hash ^= hash >> 13;
            hash *= 1274126177u;
            return MinEdgeDepth + (int)(hash % EdgeDepthVariation);
        }

        // Only DirtPath is drawn as dirt in the village (lanterns, the gate and
        // buildings are drawn on stone), so only DirtPath counts as road here.
        private static bool IsDirt(TileMap map, int x, int y)
        {
            x = Math.Clamp(x, 0, map.Width - 1);
            y = Math.Clamp(y, 0, map.Height - 1);
            return map.GetTile(new Point(x, y)) == TileType.DirtPath;
        }

        private static bool IsStone(TileMap map, int x, int y)
        {
            x = Math.Clamp(x, 0, map.Width - 1);
            y = Math.Clamp(y, 0, map.Height - 1);
            TileType tile = map.GetTile(new Point(x, y));
            if (tile == TileType.VillageLantern)
            {
                return true;
            }

            if (tile == TileType.VillagePaving)
            {
                return !IsDirt(map, x, y - 1) &&
                       !IsDirt(map, x + 1, y) &&
                       !IsDirt(map, x, y + 1) &&
                       !IsDirt(map, x - 1, y);
            }

            foreach (var structure in map.VillageStructures)
            {
                if (structure.Type != TileType.House2 &&
                    structure.Type == tile &&
                    x >= structure.Position.X &&
                    x < structure.Position.X + structure.Width &&
                    y == structure.Position.Y + structure.Height - 1)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
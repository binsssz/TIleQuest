using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    public enum TileType
    {
        Grass,
        TallGrass,
        DirtPath,
        Tree,
        Tree2,
        Tree3,
        Rock,
        VillageHearth,
        Church,
        House1,
        House2,
        ShopNPC,
        ForestExit,
        VillageLantern,
        Wall       // sentinel only — the default for out-of-bounds coordinates;
                   // never actually placed on the generated map
    }

    public readonly record struct VillageStructure(TileType Type, Point Position, int Width, int Height)
    {
        public const float RenderScale = 2f;
    }

    // DSA note: tiles are stored in a Dictionary<Point, TileType> (hash table) rather
    // than a bounds-checked 2D array, so lookups by grid coordinate are O(1). This is
    // the "Inventory & tile lookups -> Hash Table" piece from the project plan.
    public class TileMap
    {
        public int Width { get; }
        public int Height { get; }
        public int TileSize { get; }
        public Point SpawnPoint { get; }
        public bool IsFullyConnected { get; }
        public IReadOnlyList<VillageStructure> VillageStructures { get; }

        private readonly Dictionary<Point, TileType> _tiles;

        public TileMap(int width, int height, int tileSize)
        {
            Width = width;
            Height = height;
            TileSize = tileSize;

            _tiles = VillageGenerator.Generate(width, height, out var spawnPoint, out var villageStructures);
            SpawnPoint = spawnPoint;
            VillageStructures = villageStructures;

            // Verify every walkable tile is reachable from spawn via a graph +
            // BFS over the generated tiles. ForestGenerator already repairs
            // any isolated pockets internally, so this should always report
            // fully connected — it's a second, independent verification layer
            // at map-initialization time.
            var graph = new TileGraph(_tiles);
            IsFullyConnected = graph.IsFullyConnected(SpawnPoint, out var unreachableCount);
            Console.WriteLine(IsFullyConnected
                ? "[TileGraph] BFS connectivity check: OK, all ground reachable from spawn."
                : $"[TileGraph] BFS connectivity check: WARNING — {unreachableCount} tile(s) unreachable from spawn.");
        }

        public TileType GetTile(Point gridPos)
        {
            return _tiles.TryGetValue(gridPos, out var tile) ? tile : TileType.Wall;
        }

        // Tree and rock anchors are checked against their collision bounds
        // below, so their visible size controls the space they occupy.
        public bool IsWalkable(Point gridPos)
        {
            var tile = GetTile(gridPos);
            if (tile != TileType.Grass && tile != TileType.TallGrass && tile != TileType.DirtPath &&
                tile != TileType.VillageLantern &&
                !DrawTree.IsTreeType(tile) && tile != TileType.Rock)
            {
                return false;
            }

            var playerBounds = new Rectangle(
                gridPos.X * TileSize + TileSize / 4,
                gridPos.Y * TileSize + TileSize * 2 / 3,
                TileSize / 2,
                TileSize / 4);

            for (int x = gridPos.X - 1; x <= gridPos.X + 1; x++)
            {
                for (int y = gridPos.Y - 1; y <= gridPos.Y + 1; y++)
                {
                    var obstaclePosition = new Point(x, y);
                    var obstacleTile = GetTile(obstaclePosition);
                    if (DrawTree.IsTreeType(obstacleTile) &&
                        playerBounds.Intersects(DrawTree.GetCollisionBounds(obstaclePosition, obstacleTile, TileSize)))
                    {
                        return false;
                    }
                    if (obstacleTile == TileType.Rock &&
                        playerBounds.Intersects(DrawRock.GetBounds(obstaclePosition, TileSize)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public IEnumerable<KeyValuePair<Point, TileType>> AllTiles() => _tiles;
    }
}

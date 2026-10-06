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
        Tree4,
        Tree5,
        Tree6,
        Tree7,
        Tree8,
        Rock,
        VillageHearth,
        Church,
        House1,
        House2,
        ShopNPC,
        ForestExit,
        VillageExit,
        VillageLantern,
        VillagePaving,
        VillageFlower,
        VillageGarden,
        ForestFlower,
        ForestFoxglove,
        ForestMushroom,
        ForestHill,
        ForestCave,
        Prop,      // any PropCatalog prop (boulder, dead tree, bush...): blocked
        Well,      // village well structure: blocked
        Wall       // sentinel only — the default for out-of-bounds coordinates;
                   // never actually placed on the generated map
    }

    public readonly record struct VillageStructure(TileType Type, Point Position, int Width, int Height)
    {
        public const float RenderScale = 2f;

        public Rectangle[] GetCollisionBounds(int tileSize)
        {
            Point spriteSize = Type switch
            {
                TileType.Church => new Point(128, 112),
                TileType.House1 => new Point(80, 112),
                TileType.House2 => new Point(128, 112),
                _ => Point.Zero,
            };
            if (spriteSize == Point.Zero)
            {
                return Array.Empty<Rectangle>();
            }

            int spriteWidth = (int)Math.Round(spriteSize.X * RenderScale);
            int spriteHeight = (int)Math.Round(spriteSize.Y * RenderScale);
            int spriteX = Position.X * tileSize +
                          (Width * tileSize - spriteWidth) / 2;
            int spriteY = (Position.Y + Height) * tileSize - spriteHeight;

            // Solid areas in sprite pixels, measured from the opaque pixels of
            // each PNG: the wall band only, from the row where the walls come
            // out from under the roof overhang down to where they meet the
            // ground. Roof, chimney and the steps below the door stay walkable.
            // Re-measure these if a building PNG is redrawn; press F3 in game
            // to draw them over the sprites and check.
            Rectangle[] solidAreas = Type switch
            {
                // CHURCH.png: both side wings and the centre block share one
                // wall band (y 64..97); only the door porch reaches down to y 104.
                TileType.Church => new[]
                {
                    new Rectangle(2, 64, 124, 33),
                    new Rectangle(31, 97, 66, 7),
                },
                // HOUSE 1.png: walls x 5..75, y 73..104.
                TileType.House1 => new[] { new Rectangle(5, 73, 70, 31) },
                // HOUSE 2.png: the main walls (x 5..77, y 73..104) and the lower
                // side wing (x 77..126), whose wall ends at y 89 - below that
                // is open grass.
                TileType.House2 => new[]
                {
                    new Rectangle(5, 73, 72, 31),
                    new Rectangle(77, 64, 49, 25),
                },
                _ => Array.Empty<Rectangle>(),
            };

            var bounds = new Rectangle[solidAreas.Length];
            for (int i = 0; i < solidAreas.Length; i++)
            {
                Rectangle area = solidAreas[i];
                bounds[i] = new Rectangle(
                    spriteX + (int)Math.Round(area.X * RenderScale),
                    spriteY + (int)Math.Round(area.Y * RenderScale),
                    (int)Math.Round(area.Width * RenderScale),
                    (int)Math.Round(area.Height * RenderScale));
            }

            return bounds;
        }
    }

    public enum WorldMapType
    {
        Village,
        Forest
    }

    public readonly record struct MapTravel(WorldMapType Destination, Point ArrivalPoint);

    // DSA note: tiles are stored in a Dictionary<Point, TileType> (hash table) rather
    // than a bounds-checked 2D array, so lookups by grid coordinate are O(1). This is
    // the "Inventory & tile lookups -> Hash Table" piece from the project plan.
    public class TileMap
    {
        public int Width { get; }
        public int Height { get; }
        public int TileSize { get; }
        public WorldMapType MapType { get; }
        public Point SpawnPoint { get; }
        public bool IsFullyConnected { get; }
        internal ForestTmxMap? ForestMap { get; }
        public IReadOnlyList<VillageStructure> VillageStructures { get; }
        public IReadOnlyList<VillageProp> VillageProps { get; }
        public IReadOnlyList<VillageProp> Props => VillageProps;

        private readonly Dictionary<Point, TileType> _tiles;
        private readonly Dictionary<Point, MapTravel> _travelPoints = new();
        private readonly HashSet<Point> _forestWalkableTiles;
        private readonly HashSet<Point> _forestTreeAnchorTiles;

        public TileMap(int width, int height, int tileSize)
            : this(width, height, tileSize, WorldMapType.Village)
        {
        }

        public TileMap(int width, int height, int tileSize, WorldMapType mapType)
        {
            TileSize = tileSize;
            MapType = mapType;

            if (mapType == WorldMapType.Village)
            {
                Width = width;
                Height = height;
                _forestWalkableTiles = new HashSet<Point>();
                _forestTreeAnchorTiles = new HashSet<Point>();
                _tiles = VillageGenerator.Generate(
                    width, height, out var spawnPoint, out var villageStructures, out var villageProps);
                SpawnPoint = spawnPoint;
                VillageStructures = villageStructures;
                VillageProps = villageProps;
                AddTravelPoint(
                    new Point(width / 2, height - 1),
                    TileType.ForestExit,
                    WorldMapType.Forest,
                    new Point(width / 2, height / 2));
            }
            else
            {
                ForestMap = ForestTmxMap.Load();
                Width = ForestMap.Width;
                Height = ForestMap.Height;
                _forestWalkableTiles = ForestMap.WalkableTiles;
                _forestTreeAnchorTiles = ForestMap.TreeAnchorTiles;
                SpawnPoint = new Point(Width / 2, Height / 2);
                if (!_forestWalkableTiles.Contains(SpawnPoint) ||
                    _forestTreeAnchorTiles.Contains(SpawnPoint))
                {
                    throw new InvalidOperationException(
                        $"The forest spawn point x={SpawnPoint.X}, y={SpawnPoint.Y} is outside the walkable foothill or on a tree base.");
                }
                _tiles = new Dictionary<Point, TileType>(Width * Height);
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        _tiles[new Point(x, y)] = TileType.Grass;
                    }
                }
                VillageStructures = Array.Empty<VillageStructure>();
                VillageProps = Array.Empty<VillageProp>();
                AddTravelPoint(
                    SpawnPoint,
                    TileType.VillageExit,
                    WorldMapType.Village,
                    new Point(width / 2, height - 2));
            }

            Dictionary<Point, TileType> graphTiles = _tiles;
            if (MapType == WorldMapType.Forest)
            {
                graphTiles = new Dictionary<Point, TileType>();
                foreach (Point position in _forestWalkableTiles)
                {
                    if (position.X >= 0 && position.X < Width &&
                        position.Y >= 0 && position.Y < Height &&
                        !_forestTreeAnchorTiles.Contains(position))
                    {
                        graphTiles[position] = TileType.Grass;
                    }
                }
            }

            var graph = new TileGraph(graphTiles);
            IsFullyConnected = graph.IsFullyConnected(SpawnPoint, out var unreachableCount);
            Console.WriteLine(IsFullyConnected
                ? "[TileGraph] BFS connectivity check: OK, all ground reachable from spawn."
                : $"[TileGraph] BFS connectivity check: WARNING — {unreachableCount} tile(s) unreachable from spawn.");
        }

        public bool TryGetTravel(Point position, out MapTravel travel)
        {
            return _travelPoints.TryGetValue(position, out travel);
        }

        public TileType GetTile(Point gridPos)
        {
            return _tiles.TryGetValue(gridPos, out var tile) ? tile : TileType.Wall;
        }

        // Village buildings, trees and rocks use sprite bounds so their visible
        // size controls occupied space.
        public bool IsWalkable(Point gridPos)
        {
            if (MapType == WorldMapType.Forest)
            {
                return gridPos.X >= 0 && gridPos.X < Width &&
                       gridPos.Y >= 0 && gridPos.Y < Height &&
                       _forestWalkableTiles.Contains(gridPos) &&
                       !_forestTreeAnchorTiles.Contains(gridPos);
            }

            var tile = GetTile(gridPos);
            bool isWalkableGround =
                tile == TileType.Grass || tile == TileType.TallGrass || tile == TileType.DirtPath ||
                tile == TileType.ForestExit || tile == TileType.VillageExit ||
                tile == TileType.VillagePaving || tile == TileType.VillageFlower ||
                tile == TileType.VillageGarden || tile == TileType.ForestFlower ||
                tile == TileType.ForestFoxglove || tile == TileType.ForestMushroom ||
                DrawTree.IsTreeType(tile) || tile == TileType.Rock;
            bool isBuildingSprite = tile == TileType.Church ||
                                    tile == TileType.House1 ||
                                    tile == TileType.House2;
            if (!isWalkableGround && !isBuildingSprite)
            {
                return false;
            }

            var playerBounds = new Rectangle(
                gridPos.X * TileSize + TileSize / 4,
                gridPos.Y * TileSize + TileSize * 2 / 3,
                TileSize / 2,
                TileSize / 4);

            foreach (var structure in VillageStructures)
            {
                foreach (Rectangle collisionBounds in structure.GetCollisionBounds(TileSize))
                {
                    if (playerBounds.Intersects(collisionBounds))
                    {
                        return false;
                    }
                }
            }

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

        private void AddTravelPoint(Point position, TileType tile, WorldMapType destination, Point arrivalPoint)
        {
            _tiles[position] = tile;
            _travelPoints.Add(position, new MapTravel(destination, arrivalPoint));
        }
    }
}

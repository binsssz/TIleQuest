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
        ForestDungeonEntrance,
        DungeonExit,
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
        Forest,
        Dungeon
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
        public IReadOnlyList<VillageStructure> VillageStructures { get; }
        public IReadOnlyList<VillageProp> VillageProps => _props;
        public IReadOnlyList<VillageProp> Props => _props;
        public IReadOnlyDictionary<Point, ResourceType> Pickups => _pickups;

        private readonly Dictionary<Point, TileType> _tiles;
        private readonly Dictionary<Point, MapTravel> _travelPoints = new();
        private readonly List<VillageProp> _props = new();
        private readonly Dictionary<Point, ResourceNode> _resourceNodes = new();
        private readonly Dictionary<Point, ResourceType> _pickups = new();

        public TileMap(int width, int height, int tileSize)
            : this(width, height, tileSize, WorldMapType.Village)
        {
        }

        public TileMap(int width, int height, int tileSize, WorldMapType mapType)
        {
            Width = width;
            Height = height;
            TileSize = tileSize;
            MapType = mapType;

            if (mapType == WorldMapType.Village)
            {
                _tiles = CreateCollisionTiles(
                    VillageCollisionLayout.Rows, width, height, nameof(VillageCollisionLayout));
                SpawnPoint = VillageCollisionLayout.SpawnPoint;
                VillageStructures = Array.Empty<VillageStructure>();
            }
            else if (mapType == WorldMapType.Forest)
            {
                _tiles = ForestGenerator.Generate(
                    width, height, new Random(), out var forestSpawn, out var forestProps);
                SpawnPoint = forestSpawn;
                VillageStructures = Array.Empty<VillageStructure>();
                _props.AddRange(forestProps);
                AddTravelPoint(
                    ForestLayout.Gate,
                    TileType.ForestExit,
                    WorldMapType.Village,
                    VillageCollisionLayout.SpawnPoint);
                int firstEntranceTile = ForestLayout.DungeonEntrance.X -
                                        ForestLayout.DungeonEntranceWidth / 2;
                for (int offset = 0; offset < ForestLayout.DungeonEntranceWidth; offset++)
                {
                    AddTravelPoint(
                        new Point(firstEntranceTile + offset, ForestLayout.DungeonEntrance.Y),
                        TileType.ForestDungeonEntrance,
                        WorldMapType.Dungeon,
                        DungeonCollisionLayout.SpawnPoint);
                }
            }
            else
            {
                _tiles = CreateCollisionTiles(
                    DungeonCollisionLayout.Rows, width, height, nameof(DungeonCollisionLayout));
                SpawnPoint = DungeonCollisionLayout.SpawnPoint;
                VillageStructures = Array.Empty<VillageStructure>();
                AddTravelPoint(
                    DungeonCollisionLayout.Exit,
                    TileType.DungeonExit,
                    WorldMapType.Forest,
                    ForestLayout.DungeonEntrance);
            }

            PlaceResources();

            var graph = new TileGraph(_tiles);
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

        public bool TryHarvestResource(Point tile, out Item item)
        {
            return TryHarvestResource(tile, out item, out _);
        }

        // One swing landing on a resource node: yields one item and takes a hit
        // off the node. nodeDepleted is true when that was the last hit - the
        // node is then gone from the map (tile walkable again, prop removed),
        // so the caller should rebuild its map visuals.
        public bool TryHarvestResource(Point tile, out Item item, out bool nodeDepleted)
        {
            item = null!;
            nodeDepleted = false;
            if (!_resourceNodes.TryGetValue(tile, out var node) || node.IsDepleted)
            {
                return false;
            }

            item = node.Hit();
            if (node.IsDepleted)
            {
                nodeDepleted = true;
                _tiles[tile] = TileType.Grass;
                _props.Remove(new VillageProp(node.Prop, node.Tile));
            }

            return true;
        }

        // Brings every depleted node back (e.g. when a new day starts). A node
        // whose tile is currently blocked by something else - the player or an
        // enemy, via isOccupied - stays depleted until next time. Returns true
        // if anything came back, so the caller can rebuild its map visuals.
        public bool RestoreResourceNodes(Func<Point, bool>? isOccupied = null)
        {
            bool restoredAny = false;
            foreach (ResourceNode node in _resourceNodes.Values)
            {
                if (!node.IsDepleted || (isOccupied?.Invoke(node.Tile) ?? false))
                {
                    continue;
                }

                node.Restore();
                _tiles[node.Tile] = TileType.Prop;
                _props.Add(new VillageProp(node.Prop, node.Tile));
                restoredAny = true;
            }

            return restoredAny;
        }

        // Iron and gold on the floor: walking onto the tile collects it.
        public bool TryCollectPickup(Point tile, out Item item)
        {
            item = null!;
            if (!_pickups.TryGetValue(tile, out var type))
            {
                return false;
            }

            _pickups.Remove(tile);
            item = ResourceItems.Create(type);
            return true;
        }

        private void PlaceResources()
        {
            foreach (var spot in ResourceLayout.NodesFor(MapType))
            {
                ResourceNode? node = ResourceNode.ForProp(spot.Prop, spot.Tile);
                if (node == null)
                {
                    Console.WriteLine($"[Resources] WARNING — {spot.Prop} at {spot.Tile} is not a harvestable prop; skipped.");
                    continue;
                }

                if (!CanPlaceResource(spot.Tile, "node"))
                {
                    continue;
                }

                _tiles[spot.Tile] = TileType.Prop;
                _props.Add(new VillageProp(spot.Prop, spot.Tile));
                _resourceNodes.Add(spot.Tile, node);
            }

            foreach (var spot in ResourceLayout.PickupsFor(MapType))
            {
                if (CanPlaceResource(spot.Tile, "pickup"))
                {
                    _pickups.Add(spot.Tile, spot.Type);
                }
            }
        }

        private bool CanPlaceResource(Point tile, string what)
        {
            bool free = IsWalkable(tile) &&
                        !_travelPoints.ContainsKey(tile) &&
                        tile != SpawnPoint &&
                        !_resourceNodes.ContainsKey(tile) &&
                        !_pickups.ContainsKey(tile);
            if (!free)
            {
                Console.WriteLine(
                    $"[Resources] WARNING — {what} at {tile} on the {MapType} map skipped " +
                    "(blocked, travel tile, arrival point or duplicate).");
            }

            return free;
        }

        // Building, tree and rock anchors are checked against their sprite
        // collision bounds below, so their visible size controls occupied space.
        public bool IsWalkable(Point gridPos)
        {
            if (MapType == WorldMapType.Forest && ForestLayout.UseGeneratedVisuals && IsUnderCliffFace(gridPos))
            {
                return false;
            }

            var tile = GetTile(gridPos);
            bool isWalkableGround =
                tile == TileType.Grass || tile == TileType.TallGrass || tile == TileType.DirtPath ||
                tile == TileType.ForestExit || tile == TileType.VillageExit ||
                tile == TileType.ForestDungeonEntrance || tile == TileType.DungeonExit ||
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

        // The cliff face art hangs two tiles below the last hill row, so those
        // two grass tiles look like wall and have to block the player too.
        private bool IsUnderCliffFace(Point gridPos)
        {
            return IsForestWallTile(GetTile(new Point(gridPos.X, gridPos.Y - 1))) ||
                   IsForestWallTile(GetTile(new Point(gridPos.X, gridPos.Y - 2)));
        }

        private static bool IsForestWallTile(TileType tile) =>
            tile == TileType.ForestHill || tile == TileType.ForestCave;

        public IEnumerable<KeyValuePair<Point, TileType>> AllTiles() => _tiles;

        private static Dictionary<Point, TileType> CreateCollisionTiles(
            string[] rows, int width, int height, string layoutName)
        {
            if (rows.Length != height)
            {
                throw new InvalidOperationException(
                    $"{layoutName} has {rows.Length} rows but the map is {height} tiles tall.");
            }

            var tiles = new Dictionary<Point, TileType>(width * height);
            for (int y = 0; y < height; y++)
            {
                if (rows[y].Length != width)
                {
                    throw new InvalidOperationException(
                        $"{layoutName} row y={y} has {rows[y].Length} characters but the map is {width} tiles wide.");
                }

                for (int x = 0; x < width; x++)
                {
                    tiles[new Point(x, y)] = rows[y][x] switch
                    {
                        '.' => TileType.Grass,
                        '#' => TileType.Wall,
                        char symbol => throw new InvalidOperationException(
                            $"{layoutName} contains unsupported symbol '{symbol}' at x={x}, y={y}."),
                    };
                }
            }

            return tiles;
        }

        private void AddTravelPoint(Point position, TileType tile, WorldMapType destination, Point arrivalPoint)
        {
            _tiles[position] = tile;
            _travelPoints.Add(position, new MapTravel(destination, arrivalPoint));
        }
    }
}
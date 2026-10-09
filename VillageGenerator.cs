using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    // Builds the village map from the text grid in VillageLayout.cs (edit that
    // file to redesign the village; this file only reads and checks it).
    //
    // The result is the same data the old procedural generator produced: a
    // Dictionary<Point, TileType> covering every tile, the list of building
    // footprints that Game1 draws sprites for, and the player's spawn tile.
    // Map dimensions and sprites are unchanged.
    public static class VillageGenerator
    {
        private readonly record struct Building(TileType Type, int Width, int Height);

        // Footprint sizes are fixed per building because the sprites in
        // Game1.DrawVillageStructure are positioned from them.
        private static readonly Dictionary<char, Building> Buildings = new()
        {
            ['C'] = new Building(TileType.Church, 6, 6),
            ['1'] = new Building(TileType.House1, 4, 6),
            ['2'] = new Building(TileType.House2, 4, 6),
            ['H'] = new Building(TileType.VillageHearth, 2, 2),
            // The sprite is 2x3 tiles; its top row overhangs the 2x2 footprint.
            ['W'] = new Building(TileType.Well, 2, 2),
        };

        private const int MaxReportedProblems = 25;

        public static Dictionary<Point, TileType> Generate(
            int width, int height, out Point spawnPoint, out List<VillageStructure> structures,
            out List<VillageProp> props)
        {
            string[] rows = VillageLayout.Rows;
            var errors = new List<string>();
            var tiles = new Dictionary<Point, TileType>();
            spawnPoint = Point.Zero;
            structures = new List<VillageStructure>();
            props = new List<VillageProp>();

            // 1. The grid must match the map size exactly (36 x 28 today).
            if (rows.Length != height)
            {
                errors.Add($"The layout has {rows.Length} rows but the map is {height} tiles tall (y = 0..{height - 1}).");
            }
            for (int y = 0; y < rows.Length; y++)
            {
                if (rows[y].Length != width)
                {
                    errors.Add($"Row y={y} has {rows[y].Length} characters but the map is {width} tiles wide (x = 0..{width - 1}).");
                }
            }
            ThrowIfAny(errors);

            // 2. Read every tile, top row first, left to right. A building
            // letter claims its whole footprint starting at its top-left tile.
            var claimed = new HashSet<Point>();
            var spawns = new List<Point>();
            var gates = new List<Point>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var position = new Point(x, y);
                    if (claimed.Contains(position))
                    {
                        continue;
                    }

                    char symbol = rows[y][x];
                    if (Buildings.TryGetValue(symbol, out var building))
                    {
                        PlaceBuilding(rows, width, height, position, symbol, building, tiles, claimed, structures, errors);
                        continue;
                    }

                    switch (symbol)
                    {
                        case '.':
                            tiles[position] = TileType.Grass;
                            break;
                        case ',':
                            tiles[position] = TileType.TallGrass;
                            break;
                        case '=':
                            tiles[position] = TileType.DirtPath;
                            break;
                        case 'L':
                            tiles[position] = TileType.VillageLantern;
                            break;
                        case 'P':
                            tiles[position] = TileType.VillagePaving;
                            break;
                        case 'S':
                            tiles[position] = TileType.DirtPath;
                            spawns.Add(position);
                            break;
                        case 'X':
                            tiles[position] = TileType.ForestExit;
                            gates.Add(position);
                            break;
                        case 'F':
                            tiles[position] = TileType.VillageFlower;
                            break;
                        case 'G':
                            tiles[position] = TileType.VillageGarden;
                            break;
                        case 'R':
                            tiles[position] = TileType.Rock;
                            break;
                        case 'T':
                            tiles[position] = VillageTreeType(position);
                            break;
                        default:
                            if (PropCatalog.Letters.TryGetValue(symbol, out var propKind))
                            {
                                tiles[position] = TileType.Prop;
                                props.Add(new VillageProp(propKind, position));
                            }
                            else
                            {
                                errors.Add($"Unknown character '{symbol}' at x={x}, y={y}. See the legend at the top of VillageLayout.cs.");
                                tiles[position] = TileType.Grass;
                            }
                            break;
                    }
                }
            }

            // 3. Spawn, forest gate and the tile you arrive on when you come
            // back from the forest. TileMap and the forest map are wired to the
            // bottom-centre tiles, so those stay fixed.
            var gate = new Point(width / 2, height - 1);
            var arrival = new Point(width / 2, height - 2);

            if (spawns.Count != 1)
            {
                errors.Add($"The layout needs exactly one player spawn 'S' but has {spawns.Count}.");
            }
            else
            {
                spawnPoint = spawns[0];
            }

            if (gates.Count != 1)
            {
                errors.Add($"The layout needs exactly one forest gate 'X' but has {gates.Count}; it belongs at x={gate.X}, y={gate.Y}.");
            }
            else if (gates[0] != gate)
            {
                errors.Add($"The forest gate 'X' is at x={gates[0].X}, y={gates[0].Y} but must stay at x={gate.X}, y={gate.Y} (bottom edge, centre column), because TileMap and the forest map link to that tile.");
            }

            if (!TileGraph.IsWalkableType(tiles[arrival]))
            {
                errors.Add($"The tile north of the gate (x={arrival.X}, y={arrival.Y}) is {tiles[arrival]} but must be walkable: it is where you arrive when you return from the forest.");
            }
            ThrowIfAny(errors);

            // 4. Walk the map with the same BFS graph TileMap uses. The gate and
            // every walkable road tile must be reachable from the
            // spawn, otherwise a road is cut in two. Unreachable grass or
            // tall grass is only reported as a warning, like TileMap's check.
            var graph = new TileGraph(tiles);
            HashSet<Point> reachable = graph.ReachableFrom(spawnPoint);
            if (!reachable.Contains(gate))
            {
                errors.Add($"The forest gate (x={gate.X}, y={gate.Y}) cannot be reached by walking from the spawn (x={spawnPoint.X}, y={spawnPoint.Y}); buildings are blocking every route.");
            }

            var strandedRoads = new List<Point>();
            var strandedGround = new List<Point>();
            foreach (var pair in tiles)
            {
                if (!TileGraph.IsWalkableType(pair.Value) || reachable.Contains(pair.Key))
                {
                    continue;
                }

                if (pair.Value == TileType.DirtPath || pair.Value == TileType.VillagePaving)
                {
                    strandedRoads.Add(pair.Key);
                }
                else if (pair.Value != TileType.ForestExit)
                {
                    strandedGround.Add(pair.Key);
                }
            }
            if (strandedRoads.Count > 0)
            {
                errors.Add($"{strandedRoads.Count} road or paving tile(s) can't be reached by walking from the spawn, starting with {FirstFew(strandedRoads)}. A building is probably cutting off the route.");
            }
            ThrowIfAny(errors);

            Console.WriteLine(
                $"[VillageLayout] OK: {width}x{height} grid, {structures.Count} building(s), {props.Count} prop(s), " +
                $"spawn ({spawnPoint.X}, {spawnPoint.Y}), forest gate and all roads reachable.");
            if (strandedGround.Count > 0)
            {
                Console.WriteLine(
                    $"[VillageLayout] WARNING: {strandedGround.Count} grass tile(s) can't be reached from the spawn, " +
                    $"starting with {FirstFew(strandedGround)}.");
            }

            // Game1 draws structures in list order, so keep them back-to-front
            // (lowest bottom edge first) so a building in front covers one behind.
            structures.Sort((a, b) =>
            {
                int byBottom = (a.Position.Y + a.Height).CompareTo(b.Position.Y + b.Height);
                return byBottom != 0 ? byBottom : a.Position.X.CompareTo(b.Position.X);
            });

            return tiles;
        }

        private static TileType VillageTreeType(Point position)
        {
            int variant = (position.X * 31 + position.Y * 17) % 8;
            return variant switch
            {
                0 => TileType.Tree,
                1 => TileType.Tree2,
                2 => TileType.Tree3,
                3 => TileType.Tree4,
                4 => TileType.Tree5,
                5 => TileType.Tree6,
                6 => TileType.Tree7,
                _ => TileType.Tree8,
            };
        }

        // Claims a building's footprint whose top-left tile is `origin`.
        // Every tile in the footprint must be the same letter and unclaimed.
        private static void PlaceBuilding(
            string[] rows, int width, int height, Point origin, char symbol, Building building,
            Dictionary<Point, TileType> tiles, HashSet<Point> claimed,
            List<VillageStructure> structures, List<string> errors)
        {
            int right = origin.X + building.Width;
            int bottom = origin.Y + building.Height;

            bool fits = right <= width && bottom <= height;
            for (int x = origin.X; fits && x < right; x++)
            {
                for (int y = origin.Y; fits && y < bottom; y++)
                {
                    if (rows[y][x] != symbol || claimed.Contains(new Point(x, y)))
                    {
                        fits = false;
                    }
                }
            }

            if (fits)
            {
                structures.Add(new VillageStructure(building.Type, origin, building.Width, building.Height));
            }
            else
            {
                errors.Add(
                    $"{building.Type} '{symbol}' starting at x={origin.X}, y={origin.Y} must be a solid block of " +
                    $"{building.Width} columns x {building.Height} rows of '{symbol}' inside the map, " +
                    "not overlapping another building.");
            }

            // Stamp the tiles that really contain the letter, so one bad
            // block doesn't produce a pile of follow-up errors.
            for (int x = origin.X; x < Math.Min(width, right); x++)
            {
                for (int y = origin.Y; y < Math.Min(height, bottom); y++)
                {
                    var position = new Point(x, y);
                    if (rows[y][x] == symbol && !claimed.Contains(position))
                    {
                        tiles[position] = building.Type;
                        claimed.Add(position);
                    }
                }
            }
        }

        // "(x, y), (x, y), ..." for the first few tiles in reading order.
        private static string FirstFew(List<Point> points)
        {
            points.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
            var shown = new List<string>();
            for (int i = 0; i < points.Count && i < 8; i++)
            {
                shown.Add($"({points[i].X}, {points[i].Y})");
            }
            return string.Join(", ", shown);
        }

        private static void ThrowIfAny(List<string> errors)
        {
            if (errors.Count == 0)
            {
                return;
            }

            var message = new StringBuilder();
            message.AppendLine($"VillageLayout.cs has {errors.Count} problem(s):");
            for (int i = 0; i < errors.Count && i < MaxReportedProblems; i++)
            {
                message.AppendLine("  - " + errors[i]);
            }
            if (errors.Count > MaxReportedProblems)
            {
                message.AppendLine($"  ...and {errors.Count - MaxReportedProblems} more.");
            }
            throw new InvalidOperationException(message.ToString());
        }
    }
}
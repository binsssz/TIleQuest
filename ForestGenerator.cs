using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    // DSA note: this is the "Procedural forest scattering -> Recursion" piece
    // from the project plan, replacing the earlier BSP dungeon generator. The
    // whole map starts as open grass — there are no walls at all. SpreadCluster
    // recursively grows a clump of trees/rocks/tall grass outward from a
    // random seed tile, with the spread probability decaying a little on each
    // recursive step so clusters taper off naturally instead of growing
    // forever. Three base cases stop the recursion: a depth limit, going out
    // of bounds, and landing on a tile that's no longer plain grass (already
    // claimed by this or another cluster).
    public static class ForestGenerator
    {
        private static readonly Point[] FourDirections =
        {
            new Point(0, -1),
            new Point(0, 1),
            new Point(-1, 0),
            new Point(1, 0),
        };

        public static Dictionary<Point, TileType> Generate(
            int width, int height, Random random, out Point spawnPoint,
            int treeClusters = 0, int rockClusters = 0, int tallGrassPatches = 0)
        {
            var tiles = new Dictionary<Point, TileType>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tiles[new Point(x, y)] = TileType.Grass;
                }
            }

            int area = width * height;
            if (treeClusters <= 0) treeClusters = Math.Max(6, area / 180);
            if (rockClusters <= 0) rockClusters = Math.Max(5, area / 200);
            if (tallGrassPatches <= 0) tallGrassPatches = Math.Max(4, area / 120);

            ScatterGroves(tiles, width, height, random, treeClusters);

            var canopyZone = BuildCanopyZone(tiles);
            ScatterClusters(tiles, width, height, random, rockClusters, TileType.Rock, maxDepth: 2, spreadChance: 0.45, blocked: canopyZone);
            ScatterClusters(tiles, width, height, random, tallGrassPatches, TileType.TallGrass, maxDepth: 3, spreadChance: 0.60, blocked: canopyZone);

            spawnPoint = FindSpawnPoint(tiles, width, height);
            EnsureFullyConnected(tiles, spawnPoint, random);

            return tiles;
        }

        // --- Recursive cluster growth --------------------------------------------

        private static void ScatterClusters(
            Dictionary<Point, TileType> tiles, int width, int height, Random random,
            int clusterCount, TileType entityType, int maxDepth, double spreadChance,
            HashSet<Point>? blocked)
        {
            for (int i = 0; i < clusterCount; i++)
            {
                var seed = RandomGrassTile(tiles, width, height, random, blocked);
                if (seed.HasValue)
                {
                    SpreadCluster(tiles, seed.Value, entityType, random, width, height, depth: 0, maxDepth, spreadChance, blocked);
                }
            }
        }

        // --- Tree groves (recursive, with a no-overlap rule) ---------------------

        // A grove grows left, right, up and down from each tree, by a random
        // distance so it looks scattered rather than planted in rows. The
        // minimum steps keep sprites from overlapping: neighbours side by side
        // need 2+ tiles (the sprite is 1.4 tiles wide); straight up/down needs
        // 4+ because the sprites are tall. SpreadGrove still rejects any spot
        // that would overlap.
        private static Point GroveOffset(int branch, Random random)
        {
            return branch switch
            {
                0 => new Point(-random.Next(2, 4), random.Next(-2, 3)),
                1 => new Point(random.Next(2, 4), random.Next(-2, 3)),
                2 => new Point(random.Next(-1, 2), -random.Next(4, 6)),
                _ => new Point(random.Next(-1, 2), random.Next(4, 6)),
            };
        }

        private static void ScatterGroves(
            Dictionary<Point, TileType> tiles, int width, int height, Random random, int groveCount)
        {
            var placedBounds = new List<Rectangle>();
            for (int i = 0; i < groveCount; i++)
            {
                var seed = RandomGrassTile(tiles, width, height, random, blocked: null);
                if (seed.HasValue)
                {
                    SpreadGrove(tiles, seed.Value, random, width, height, depth: 0, maxDepth: 3, spreadChance: 0.75, placedBounds);
                }
            }
        }

        private static void SpreadGrove(
            Dictionary<Point, TileType> tiles, Point origin, Random random, int width, int height,
            int depth, int maxDepth, double spreadChance, List<Rectangle> placedBounds)
        {
            if (depth > maxDepth)
            {
                return; // base case: grove has grown as far as it's allowed to
            }
            if (origin.X < 0 || origin.Y < 0 || origin.X >= width || origin.Y >= height)
            {
                return; // base case: out of bounds
            }
            if (!tiles.TryGetValue(origin, out var current) || current != TileType.Grass)
            {
                return; // base case: tile already used
            }
            // base case: the sprite would overlap another tree, or hang off
            // the edge of the map.
            var treeType = (TileType)random.Next((int)TileType.Tree, (int)TileType.Tree3 + 1);
            var bounds = DrawTree.GetBounds(origin, treeType);
            var spacedBounds = bounds;
            spacedBounds.Inflate(3, 3);
            if (spacedBounds.X < 0 || spacedBounds.Y < 0 ||
                spacedBounds.Right > width * DrawTree.SourceTileSize ||
                spacedBounds.Bottom > height * DrawTree.SourceTileSize)
            {
                return;
            }
            foreach (var other in placedBounds)
            {
                if (spacedBounds.Intersects(other))
                {
                    return;
                }
            }

            tiles[origin] = treeType;
            placedBounds.Add(spacedBounds);

            for (int branch = 0; branch < 4; branch++)
            {
                if (random.NextDouble() < spreadChance)
                {
                    SpreadGrove(tiles, origin + GroveOffset(branch, random), random, width, height, depth + 1, maxDepth, spreadChance * 0.8, placedBounds);
                }
            }
        }

        // Every tile a tree sprite covers, even partly.
        private static HashSet<Point> BuildCanopyZone(Dictionary<Point, TileType> tiles)
        {
            const int size = DrawTree.SourceTileSize;
            var zone = new HashSet<Point>();
            foreach (var pair in tiles)
            {
                if (!DrawTree.IsTreeType(pair.Value))
                {
                    continue;
                }

                var bounds = DrawTree.GetBounds(pair.Key, pair.Value);
                bounds.Inflate(3, 3);
                for (int tx = bounds.X / size; tx <= (bounds.Right - 1) / size; tx++)
                {
                    for (int ty = bounds.Y / size; ty <= (bounds.Bottom - 1) / size; ty++)
                    {
                        zone.Add(new Point(tx, ty));
                    }
                }
            }
            return zone;
        }

        private static void SpreadCluster(
            Dictionary<Point, TileType> tiles, Point origin, TileType entityType,
            Random random, int width, int height, int depth, int maxDepth, double spreadChance,
            HashSet<Point>? blocked)
        {
            if (depth > maxDepth)
            {
                return; // base case: this cluster has grown as far as it's allowed to
            }
            if (origin.X < 0 || origin.Y < 0 || origin.X >= width || origin.Y >= height)
            {
                return; // base case: out of bounds
            }
            if (!tiles.TryGetValue(origin, out var current) || current != TileType.Grass)
            {
                return; // base case: already claimed by this or another cluster
            }
            if (blocked != null && blocked.Contains(origin))
            {
                return; // base case: tile is hidden behind a tree canopy
            }
            if (entityType == TileType.Rock && !CanPlaceRock(tiles, origin, width, height, blocked))
            {
                return;
            }

            tiles[origin] = entityType;

            // Spread chance decays each step outward so clusters taper off
            // into an organic-looking clump rather than a hard-edged blob.
            foreach (var direction in FourDirections)
            {
                if (random.NextDouble() < spreadChance)
                {
                    SpreadCluster(tiles, origin + direction, entityType, random, width, height, depth + 1, maxDepth, spreadChance * 0.6, blocked);
                }
            }
        }

        private static bool CanPlaceRock(
            Dictionary<Point, TileType> tiles, Point origin, int width, int height, HashSet<Point>? blocked)
        {
            var candidate = DrawRock.GetBounds(origin, DrawTree.SourceTileSize);
            candidate.Inflate(3, 3);
            if (candidate.X < 0 || candidate.Y < 0 ||
                candidate.Right > width * DrawTree.SourceTileSize ||
                candidate.Bottom > height * DrawTree.SourceTileSize)
            {
                return false;
            }

            if (blocked != null)
            {
                for (int x = candidate.X / DrawTree.SourceTileSize; x <= (candidate.Right - 1) / DrawTree.SourceTileSize; x++)
                {
                    for (int y = candidate.Y / DrawTree.SourceTileSize; y <= (candidate.Bottom - 1) / DrawTree.SourceTileSize; y++)
                    {
                        if (blocked.Contains(new Point(x, y)))
                        {
                            return false;
                        }
                    }
                }
            }

            for (int x = origin.X - 2; x <= origin.X + 2; x++)
            {
                for (int y = origin.Y - 2; y <= origin.Y + 2; y++)
                {
                    var other = new Point(x, y);
                    var otherBounds = DrawRock.GetBounds(other, DrawTree.SourceTileSize);
                    otherBounds.Inflate(3, 3);
                    if (tiles.TryGetValue(other, out var tile) && tile == TileType.Rock &&
                        candidate.Intersects(otherBounds))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static Point? RandomGrassTile(
            Dictionary<Point, TileType> tiles, int width, int height, Random random, HashSet<Point>? blocked)
        {
            // A bounded number of random tries rather than scanning the whole
            // map — plenty for the sparse cluster counts used here.
            for (int attempt = 0; attempt < 50; attempt++)
            {
                var candidate = new Point(random.Next(width), random.Next(height));
                if (tiles[candidate] == TileType.Grass && (blocked == null || !blocked.Contains(candidate)))
                {
                    return candidate;
                }
            }
            return null;
        }

        // --- Spawn point -----------------------------------------------------------

        private static Point FindSpawnPoint(Dictionary<Point, TileType> tiles, int width, int height)
        {
            var center = new Point(width / 2, height / 2);
            if (tiles[center] == TileType.Grass)
            {
                return center;
            }

            // Center happened to land inside a cluster — spiral outward ring
            // by ring until an open grass tile turns up.
            for (int radius = 1; radius < Math.Max(width, height); radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        var candidate = new Point(center.X + dx, center.Y + dy);
                        if (tiles.TryGetValue(candidate, out var tile) && tile == TileType.Grass)
                        {
                            return candidate;
                        }
                    }
                }
            }

            return center; // extremely unlikely fallback
        }

        // --- Connectivity repair (graph + BFS), carried over from the dungeon version ---

        private static void EnsureFullyConnected(Dictionary<Point, TileType> tiles, Point spawnPoint, Random random)
        {
            const int maxRepairPasses = 8;

            for (int pass = 0; pass < maxRepairPasses; pass++)
            {
                var graph = new TileGraph(tiles);
                var components = graph.FindConnectedComponents();
                if (components.Count <= 1)
                {
                    return; // single connected component — nothing to fix
                }

                var mainComponent = components.First(c => c.Contains(spawnPoint));
                foreach (var component in components)
                {
                    if (component == mainComponent)
                    {
                        continue;
                    }

                    // Any two points from each component work as endpoints —
                    // both are guaranteed walkable tiles.
                    ClearPath(tiles, component.First(), mainComponent.First(), random);
                }
                // Loop again with a freshly rebuilt graph in case one pass
                // didn't fully merge every pocket.
            }
        }

        private static void ClearPath(Dictionary<Point, TileType> tiles, Point a, Point b, Random random)
        {
            // L-shaped path: horizontal-then-vertical or vertical-then-horizontal,
            // chosen randomly so repaired paths don't all bend the same way.
            if (random.NextDouble() < 0.5)
            {
                ClearHorizontal(tiles, a.X, b.X, a.Y);
                ClearVertical(tiles, a.Y, b.Y, b.X);
            }
            else
            {
                ClearVertical(tiles, a.Y, b.Y, a.X);
                ClearHorizontal(tiles, a.X, b.X, b.Y);
            }
        }

        private static void ClearHorizontal(Dictionary<Point, TileType> tiles, int x1, int x2, int y)
        {
            for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
            {
                tiles[new Point(x, y)] = TileType.Grass;
            }
        }

        private static void ClearVertical(Dictionary<Point, TileType> tiles, int y1, int y2, int x)
        {
            for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
            {
                tiles[new Point(x, y)] = TileType.Grass;
            }
        }
    }
}
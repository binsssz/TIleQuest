using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    // DSA note: this is the "Connectivity check -> Graph + BFS" piece from the
    // project plan. Walkable tiles (Grass and TallGrass) are nodes; two nodes
    // are connected if they're orthogonally adjacent and both walkable. Built
    // as an adjacency list: Dictionary<Point, List<Point>>.
    //
    // Takes a raw tile dictionary rather than a TileMap so ForestGenerator can
    // use this graph internally, during generation, before a TileMap even
    // exists yet — that's what makes the auto-repair step in
    // ForestGenerator.EnsureFullyConnected possible.
    public class TileGraph
    {
        private static readonly List<Point> EmptyNeighbors = new();

        private static readonly Point[] FourDirections =
        {
            new Point(0, -1), // up
            new Point(0, 1),  // down
            new Point(-1, 0), // left
            new Point(1, 0),  // right
        };

        private readonly Dictionary<Point, List<Point>> _adjacency = new();

        public TileGraph(Dictionary<Point, TileType> tiles)
        {
            foreach (var pair in tiles)
            {
                if (!IsWalkableType(pair.Value))
                {
                    continue;
                }

                var position = pair.Key;
                var neighbors = new List<Point>();
                foreach (var direction in FourDirections)
                {
                    var neighbor = position + direction;
                    if (tiles.TryGetValue(neighbor, out var neighborTile) && IsWalkableType(neighborTile))
                    {
                        neighbors.Add(neighbor);
                    }
                }
                _adjacency[position] = neighbors;
            }
        }

        // Kept in one place so TileGraph and TileMap.IsWalkable can't quietly
        // drift out of sync about which tile types are passable.
        private static bool IsWalkableType(TileType tile)
        {
            return tile == TileType.Grass || tile == TileType.TallGrass;
        }

        public IReadOnlyList<Point> Neighbors(Point position)
        {
            return _adjacency.TryGetValue(position, out var neighbors) ? neighbors : EmptyNeighbors;
        }

        // Breadth-first search: explores the graph one "ring" of distance at a
        // time using a FIFO queue, marking each tile visited the moment it's
        // enqueued so nothing is processed twice. Returns every tile reachable
        // from start by walking through walkable tiles only.
        public HashSet<Point> ReachableFrom(Point start)
        {
            var visited = new HashSet<Point> { start };
            var queue = new Queue<Point>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in Neighbors(current))
                {
                    if (visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return visited;
        }

        // True if every walkable tile in the graph is reachable from start.
        // unreachableCount reports how many are not, for logging/diagnostics.
        public bool IsFullyConnected(Point start, out int unreachableCount)
        {
            var reachable = ReachableFrom(start);
            unreachableCount = _adjacency.Count - reachable.Count;
            return unreachableCount == 0;
        }

        // Finds every connected component among ALL walkable tiles (not just
        // reachability from one point) via repeated BFS: run BFS from any
        // unvisited node, record what it reaches as one component, then move
        // on to the next unvisited node and repeat. Used by ForestGenerator to
        // detect isolated pockets so they can be stitched together.
        public List<HashSet<Point>> FindConnectedComponents()
        {
            var components = new List<HashSet<Point>>();
            var visited = new HashSet<Point>();

            foreach (var node in _adjacency.Keys)
            {
                if (visited.Contains(node))
                {
                    continue;
                }

                var component = ReachableFrom(node);
                components.Add(component);
                visited.UnionWith(component);
            }

            return components;
        }
    }
}

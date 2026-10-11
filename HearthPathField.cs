using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    // DSA note: Graph + BFS, used for gameplay (enemy navigation) rather than
    // only a startup check. One breadth-first search runs outward from the
    // tiles beside the hearth and stores each reachable tile's walking
    // distance. Every goblin then just steps to the neighbouring tile with the
    // smallest distance, so one O(tiles) search serves the whole wave instead
    // of one search per goblin per step.
    public class HearthPathField
    {
        private const double SidestepChance = 0.5;

        private static readonly Point[] FourDirections =
        {
            new Point(0, -1),
            new Point(0, 1),
            new Point(-1, 0),
            new Point(1, 0),
        };

        private readonly Dictionary<Point, int> _distance = new();

        // targetTiles are the (blocked) tiles being attacked. Attackers stand on
        // a walkable tile orthogonally beside them, which is distance 0.
        public HearthPathField(
            IEnumerable<Point> targetTiles,
            Func<Point, bool> isWalkable,
            int width,
            int height)
        {
            var targets = new HashSet<Point>(targetTiles);
            var queue = new Queue<Point>();

            bool CanStandOn(Point tile) =>
                tile.X >= 0 && tile.X < width && tile.Y >= 0 && tile.Y < height &&
                !targets.Contains(tile) && isWalkable(tile);

            foreach (Point target in targets)
            {
                foreach (Point direction in FourDirections)
                {
                    Point beside = target + direction;
                    if (CanStandOn(beside) && !_distance.ContainsKey(beside))
                    {
                        _distance[beside] = 0;
                        queue.Enqueue(beside);
                    }
                }
            }

            while (queue.Count > 0)
            {
                Point current = queue.Dequeue();
                int next = _distance[current] + 1;
                foreach (Point direction in FourDirections)
                {
                    Point neighbor = current + direction;
                    if (CanStandOn(neighbor) && !_distance.ContainsKey(neighbor))
                    {
                        _distance[neighbor] = next;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        // Walking distance to the nearest attack tile, or -1 if unreachable.
        public int DistanceTo(Point tile)
        {
            return _distance.TryGetValue(tile, out int distance) ? distance : -1;
        }

        public bool IsReachable(Point tile)
        {
            return _distance.ContainsKey(tile);
        }

        // The free neighbouring tile that is closest to the hearth, or null if
        // the enemy is already at an attack tile or has to wait. isBlocked
        // marks tiles held by other actors. When every closer tile is taken an
        // enemy sometimes sidesteps to a free tile of equal distance, so a crowd
        // spreads around the hearth instead of queueing in a single line.
        public Point? NextStep(Point from, Func<Point, bool> isBlocked)
        {
            if (!_distance.TryGetValue(from, out int current) || current == 0)
            {
                return null;
            }

            Point? best = null;
            int bestDistance = int.MaxValue;
            int first = Random.Shared.Next(FourDirections.Length);
            for (int i = 0; i < FourDirections.Length; i++)
            {
                Point candidate = from + FourDirections[(first + i) % FourDirections.Length];
                if (!_distance.TryGetValue(candidate, out int distance) || isBlocked(candidate))
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            if (best == null)
            {
                return null;
            }

            if (bestDistance < current)
            {
                return best;
            }

            return bestDistance == current && Random.Shared.NextDouble() < SidestepChance ? best : null;
        }
    }
}

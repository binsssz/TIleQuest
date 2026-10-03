using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    public static class VillageGenerator
    {
        public static Dictionary<Point, TileType> Generate(
            int width, int height, out Point spawnPoint, out List<VillageStructure> structures)
        {
            var tiles = new Dictionary<Point, TileType>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tiles[new Point(x, y)] = TileType.Grass;
                }
            }

            int centerX = width / 2;
            int centerY = height / 2;
            int houseMargin = width / 6;
            int leftHouseX = houseMargin;
            int rightHouseX = width - houseMargin - 4;
            int northHouseY = height / 4;
            int southHouseY = height * 2 / 3;

            structures = new List<VillageStructure>
            {
                new(TileType.Church, new Point(centerX - 3, 3), 6, 6),
                new(TileType.House1, new Point(leftHouseX +2, northHouseY), 4, 6),
                new(TileType.House2, new Point(rightHouseX, northHouseY), 4, 6),
                new(TileType.House2, new Point(leftHouseX, southHouseY), 4, 6),
                new(TileType.House1, new Point(rightHouseX, southHouseY), 4, 6),
                new(TileType.VillageHearth, new Point(centerX - 1, centerY - 1), 2, 2)
            };

            foreach (var structure in structures)
            {
                for (int x = structure.Position.X; x < structure.Position.X + structure.Width; x++)
                {
                    for (int y = structure.Position.Y; y < structure.Position.Y + structure.Height; y++)
                    {
                        tiles[new Point(x, y)] = structure.Type;
                    }
                }
            }

            // A broad crossroad connects the forest gate, the church and all four homes.
            AddRoad(tiles, width, height, centerX - 2, centerX + 1, 9, height - 1);
            AddRoad(tiles, width, height, 1, width - 2, centerY - 1, centerY + 1);
            AddRoad(tiles, width, height, centerX - 5, centerX + 4, centerY - 4, centerY + 4);

            // Short lanes lead from each home's front to the village square.
            foreach (int houseX in new[] { leftHouseX, rightHouseX })
            {
                int doorwayX = houseX + 2;
                AddRoad(tiles, width, height, doorwayX, doorwayX, northHouseY + 6, centerY - 2);
                AddRoad(tiles, width, height, doorwayX, doorwayX, centerY + 2, southHouseY - 1);
            }

            AddGarden(tiles, width, height, 2, northHouseY + 1);
            AddGarden(tiles, width, height, width - 5, northHouseY + 1);
            AddGarden(tiles, width, height, 2, southHouseY + 1);
            AddGarden(tiles, width, height, width - 5, southHouseY + 1);

            foreach (var lantern in new[]
            {
                new Point(centerX - 4, centerY - 3),
                new Point(centerX + 3, centerY - 3),
                new Point(centerX - 4, centerY + 3),
                new Point(centerX + 3, centerY + 3)
            })
            {
                if (tiles[lantern] == TileType.DirtPath)
                {
                    tiles[lantern] = TileType.VillageLantern;
                }
            }

            spawnPoint = new Point(centerX, height - 2);
            tiles[spawnPoint] = TileType.DirtPath;
            return tiles;
        }

        private static void AddRoad(
            Dictionary<Point, TileType> tiles, int width, int height,
            int left, int right, int top, int bottom)
        {
            for (int x = left; x <= right; x++)
            {
                for (int y = top; y <= bottom; y++)
                {
                    var position = new Point(x, y);
                    if (tiles.TryGetValue(position, out var tile) && tile == TileType.Grass)
                    {
                        tiles[position] = TileType.DirtPath;
                    }
                }
            }
        }

        private static void AddGarden(Dictionary<Point, TileType> tiles, int width, int height, int left, int top)
        {
            for (int x = left; x < left + 3; x++)
            {
                for (int y = top; y < top + 2; y++)
                {
                    var position = new Point(x, y);
                    if (position.X >= 0 && position.Y >= 0 && position.X < width && position.Y < height &&
                        tiles[position] == TileType.Grass)
                    {
                        tiles[position] = TileType.TallGrass;
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using System.Buffers.Binary;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    internal sealed class ForestTmxMap
    {
        internal const string WalkableLayerName = "foothill";
        internal const string WalkableGrassLayerName = "foothill grass";
        internal const string TreesLayerName = "foothill trees";
        private const uint TileIdMask = 0x1FFFFFFF;

        public int Width { get; }
        public int Height { get; }
        public int TileWidth { get; }
        public int TileHeight { get; }
        public IReadOnlyList<ForestTmxTileset> Tilesets { get; }
        public IReadOnlyList<ForestTmxLayer> Layers { get; }
        public HashSet<Point> WalkableTiles { get; }
        public HashSet<Point> TreeAnchorTiles { get; }

        private ForestTmxMap(
            int width,
            int height,
            int tileWidth,
            int tileHeight,
            IReadOnlyList<ForestTmxTileset> tilesets,
            IReadOnlyList<ForestTmxLayer> layers,
            HashSet<Point> walkableTiles,
            HashSet<Point> treeAnchorTiles)
        {
            Width = width;
            Height = height;
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            Tilesets = tilesets;
            Layers = layers;
            WalkableTiles = walkableTiles;
            TreeAnchorTiles = treeAnchorTiles;
        }

        public static ForestTmxMap Load()
        {
            using Stream stream = TitleContainer.OpenStream("Content/forestmap.tmx");
            XDocument document = XDocument.Load(stream);
            XElement map = document.Root
                ?? throw new InvalidDataException("The forest TMX file has no map root element.");

            int width = ReadPositiveDimension(map, "width");
            int height = ReadPositiveDimension(map, "height");
            int tileWidth = ReadPositiveDimension(map, "tilewidth");
            int tileHeight = ReadPositiveDimension(map, "tileheight");
            var tilesets = map.Elements("tileset")
                .Select(ReadTileset)
                .OrderBy(tileset => tileset.FirstGlobalId)
                .ToArray();
            ForestTmxLayer[] layers = map.Elements("layer")
                .Select(layer => ReadLayer(layer, width, height))
                .ToArray();

            ForestTmxLayer[] walkableLayers = layers
                .Where(layer => layer.Name == WalkableLayerName ||
                                layer.Name == WalkableGrassLayerName)
                .ToArray();
            if (walkableLayers.Length != 2)
            {
                throw new InvalidDataException(
                    $"The forest TMX must contain exactly one '{WalkableLayerName}' and one '{WalkableGrassLayerName}' layer.");
            }

            ForestTmxLayer[] treesLayers = layers
                .Where(layer => layer.Name == TreesLayerName)
                .ToArray();
            if (treesLayers.Length != 1)
            {
                throw new InvalidDataException(
                    $"The forest TMX must contain exactly one '{TreesLayerName}' layer; found {treesLayers.Length}.");
            }

            foreach (ForestTmxLayer layer in layers)
            {
                foreach (uint globalTileId in layer.GlobalTileIds)
                {
                    uint tileId = GetTileId(globalTileId);
                    if (tileId == 0)
                    {
                        continue;
                    }

                    ForestTmxTileset? tileset = tilesets.LastOrDefault(
                        candidate => tileId >= candidate.FirstGlobalId &&
                                     tileId - candidate.FirstGlobalId < candidate.TileCount);
                    if (tileset is null)
                    {
                        throw new InvalidDataException(
                            $"Forest TMX layer '{layer.Name}' references unknown global tile ID {tileId}.");
                    }

                    tileset.GetTileImage((int)(tileId - tileset.FirstGlobalId));
                }
            }

            var walkableTiles = new HashSet<Point>();
            foreach (ForestTmxLayer layer in walkableLayers)
            {
                for (int i = 0; i < layer.GlobalTileIds.Length; i++)
                {
                    if (GetTileId(layer.GlobalTileIds[i]) != 0)
                    {
                        walkableTiles.Add(new Point(i % width, i / width));
                    }
                }
            }

            HashSet<Point> treeAnchorTiles = FindTreeAnchorTiles(treesLayers[0], width, height);
            return new ForestTmxMap(
                width, height, tileWidth, tileHeight, tilesets, layers, walkableTiles, treeAnchorTiles);
        }

        private static HashSet<Point> FindTreeAnchorTiles(ForestTmxLayer layer, int width, int height)
        {
            var occupiedTiles = new HashSet<Point>();
            for (int i = 0; i < layer.GlobalTileIds.Length; i++)
            {
                if (GetTileId(layer.GlobalTileIds[i]) != 0)
                {
                    occupiedTiles.Add(new Point(i % width, i / width));
                }
            }

            var anchors = new HashSet<Point>();
            var visited = new HashSet<Point>();
            var queue = new Queue<Point>();
            foreach (Point start in occupiedTiles)
            {
                if (!visited.Add(start))
                {
                    continue;
                }

                queue.Enqueue(start);
                var component = new List<Point>();
                int bottomY = start.Y;
                while (queue.Count > 0)
                {
                    Point current = queue.Dequeue();
                    component.Add(current);
                    bottomY = Math.Max(bottomY, current.Y);

                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        for (int offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            if (offsetX == 0 && offsetY == 0)
                            {
                                continue;
                            }

                            Point neighbor = new(current.X + offsetX, current.Y + offsetY);
                            if (occupiedTiles.Contains(neighbor) && visited.Add(neighbor))
                            {
                                queue.Enqueue(neighbor);
                            }
                        }
                    }
                }

                Point[] bottomRow = component
                    .Where(position => position.Y == bottomY)
                    .OrderBy(position => position.X)
                    .ToArray();
                anchors.Add(bottomRow[bottomRow.Length / 2]);
            }

            return anchors;
        }

        public static uint GetTileId(uint globalTileId)
        {
            return globalTileId & TileIdMask;
        }

        private static ForestTmxTileset ReadTileset(XElement element)
        {
            int firstGlobalId = ReadPositiveDimension(element, "firstgid");
            int tileWidth = ReadPositiveDimension(element, "tilewidth");
            int tileHeight = ReadPositiveDimension(element, "tileheight");
            int columns = ReadPositiveDimension(element, "columns");
            int tileCount = ReadPositiveDimension(element, "tilecount");
            XElement? image = element.Element("image");
            if (image is not null)
            {
                return new ForestTmxTileset(
                    firstGlobalId,
                    tileCount,
                    tileWidth,
                    tileHeight,
                    columns,
                    ReadImageName(image, element),
                    new Dictionary<int, ForestTmxTileImage>());
            }

            var tileImages = new Dictionary<int, ForestTmxTileImage>();
            foreach (XElement tile in element.Elements("tile"))
            {
                int localTileId = ReadNonnegativeDimension(tile, "id");
                XElement tileImage = tile.Element("image")
                    ?? throw new InvalidDataException(
                        $"Forest TMX image-collection tile {localTileId} has no image.");
                int imageWidth = ReadPositiveDimension(tileImage, "width");
                int imageHeight = ReadPositiveDimension(tileImage, "height");
                XElement properties = tile.Element("properties")
                    ?? throw new InvalidDataException(
                        $"Forest TMX image-collection tile {localTileId} has no crop properties.");
                int sourceX = ReadTileImageProperty(properties, "nottiled_image_x");
                int sourceY = ReadTileImageProperty(properties, "nottiled_image_y");
                if (sourceX + tileWidth > imageWidth || sourceY + tileHeight > imageHeight)
                {
                    throw new InvalidDataException(
                        $"Forest TMX image-collection tile {localTileId} has a crop outside its source image.");
                }

                tileImages.Add(
                    localTileId,
                    new ForestTmxTileImage(
                        ReadImageName(tileImage, tile),
                        sourceX,
                        sourceY,
                        tileWidth,
                        tileHeight));
            }

            if (tileImages.Count == 0)
            {
                throw new InvalidDataException(
                    $"The forest TMX tileset '{(string?)element.Attribute("name")}' has neither an atlas nor tile images.");
            }

            return new ForestTmxTileset(
                firstGlobalId,
                tileCount,
                tileWidth,
                tileHeight,
                columns,
                null,
                tileImages);
        }

        private static string ReadImageName(XElement image, XElement tileset)
        {
            string imageName = Path.GetFileName((string?)image.Attribute("source") ?? string.Empty);
            if (string.IsNullOrWhiteSpace(imageName))
            {
                throw new InvalidDataException(
                    $"The forest TMX tileset '{(string?)tileset.Attribute("name")}' has an invalid image path.");
            }

            return imageName;
        }

        private static int ReadTileImageProperty(XElement properties, string propertyName)
        {
            XElement? property = properties.Elements("property")
                .FirstOrDefault(candidate => (string?)candidate.Attribute("name") == propertyName);
            if (!int.TryParse((string?)property?.Attribute("value"), out int value) || value < 0)
            {
                throw new InvalidDataException(
                    $"Forest TMX image-collection tiles are missing a valid '{propertyName}' property.");
            }

            return value;
        }

        private static ForestTmxLayer ReadLayer(XElement element, int mapWidth, int mapHeight)
        {
            string name = (string?)element.Attribute("name")
                ?? throw new InvalidDataException("A forest TMX layer has no name.");
            int width = ReadPositiveDimension(element, "width");
            int height = ReadPositiveDimension(element, "height");
            if (width != mapWidth || height != mapHeight)
            {
                throw new InvalidDataException(
                    $"Forest TMX layer '{name}' is {width}x{height}; expected {mapWidth}x{mapHeight}.");
            }

            XElement data = element.Element("data")
                ?? throw new InvalidDataException($"Forest TMX layer '{name}' has no tile data.");
            if ((string?)data.Attribute("encoding") != "base64" ||
                (string?)data.Attribute("compression") != "gzip")
            {
                throw new InvalidDataException(
                    $"Forest TMX layer '{name}' must use base64-encoded gzip tile data.");
            }

            byte[] compressedData = Convert.FromBase64String(data.Value);
            using var compressedStream = new MemoryStream(compressedData);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var tileStream = new MemoryStream();
            gzipStream.CopyTo(tileStream);

            int expectedByteCount = checked(width * height * sizeof(uint));
            if (tileStream.Length != expectedByteCount)
            {
                throw new InvalidDataException(
                    $"Forest TMX layer '{name}' contains {tileStream.Length} bytes; expected {expectedByteCount}.");
            }

            byte[] tileData = tileStream.ToArray();
            var globalTileIds = new uint[width * height];
            for (int i = 0; i < globalTileIds.Length; i++)
            {
                globalTileIds[i] = BinaryPrimitives.ReadUInt32LittleEndian(
                    tileData.AsSpan(i * sizeof(uint), sizeof(uint)));
            }

            bool visible = (string?)element.Attribute("visible") != "0";
            return new ForestTmxLayer(name, globalTileIds, visible);
        }

        private static int ReadPositiveDimension(XElement element, string name)
        {
            if (!int.TryParse((string?)element.Attribute(name), out int value) || value <= 0)
            {
                throw new InvalidDataException($"The forest TMX has an invalid {name}.");
            }

            return value;
        }

        private static int ReadNonnegativeDimension(XElement element, string name)
        {
            if (!int.TryParse((string?)element.Attribute(name), out int value) || value < 0)
            {
                throw new InvalidDataException($"The forest TMX has an invalid {name}.");
            }

            return value;
        }
    }

    internal sealed class ForestTmxTileset
    {
        public int FirstGlobalId { get; }
        public int TileCount { get; }
        public int TileWidth { get; }
        public int TileHeight { get; }
        public int Columns { get; }
        public string? ImageName { get; }
        public IReadOnlyDictionary<int, ForestTmxTileImage> TileImages { get; }

        public ForestTmxTileset(
            int firstGlobalId,
            int tileCount,
            int tileWidth,
            int tileHeight,
            int columns,
            string? imageName,
            IReadOnlyDictionary<int, ForestTmxTileImage> tileImages)
        {
            FirstGlobalId = firstGlobalId;
            TileCount = tileCount;
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            Columns = columns;
            ImageName = imageName;
            TileImages = tileImages;
        }

        public ForestTmxTileImage GetTileImage(int localTileId)
        {
            if (ImageName is not null)
            {
                return new ForestTmxTileImage(
                    ImageName,
                    localTileId % Columns * TileWidth,
                    localTileId / Columns * TileHeight,
                    TileWidth,
                    TileHeight);
            }

            if (TileImages.TryGetValue(localTileId, out ForestTmxTileImage? image))
            {
                return image;
            }

            throw new InvalidDataException(
                $"Forest TMX image-collection tileset has no image for tile ID {localTileId}.");
        }
    }

    internal sealed class ForestTmxTileImage
    {
        public string ImageName { get; }
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }

        public ForestTmxTileImage(string imageName, int x, int y, int width, int height)
        {
            ImageName = imageName;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    internal sealed class ForestTmxLayer
    {
        public string Name { get; }
        public uint[] GlobalTileIds { get; }
        public bool Visible { get; }

        public ForestTmxLayer(string name, uint[] globalTileIds, bool visible)
        {
            Name = name;
            GlobalTileIds = globalTileIds;
            Visible = visible;
        }
    }
}

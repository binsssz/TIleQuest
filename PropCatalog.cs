using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    // Everything the village layout can place that is a single sprite on a
    // single blocked tile: boulders, dead trees, bushes, crystals and the
    // little forge-corner props. To add another prop, add a PropKind, a letter
    // in Letters and a row in Definitions - VillageGenerator, TileMap and
    // Game1 pick it up without any other change.
    public enum PropKind
    {
        TallBoulder,
        RoundBoulder,
        BrownBoulder,
        DeadTree,
        Bush,
        BigBush,
        Crystal,
        Anvil,
        Stump,
        Barrel,
        Scarecrow,
    }

    // Which PNG a prop's source rectangle points into (see FileFor).
    public enum PropSheet
    {
        Rocks,
        ObjectRocks,
        Vegetation,
        Tools,
        Furniture,
        Farm,
    }

    // One prop on the map. The tile is the one it stands on; the sprite is
    // drawn bottom-centre on that tile and may reach up and sideways past it.
    public readonly record struct VillageProp(PropKind Kind, Point Tile);

    public static class PropCatalog
    {
        // Layout letter -> prop. These must not collide with the letters
        // VillageGenerator already uses (. , = L P F G R T S X C 1 2 H W).
        public static readonly IReadOnlyDictionary<char, PropKind> Letters = new Dictionary<char, PropKind>
        {
            ['B'] = PropKind.TallBoulder,
            ['O'] = PropKind.RoundBoulder,
            ['M'] = PropKind.BrownBoulder,
            ['D'] = PropKind.DeadTree,
            ['U'] = PropKind.Bush,
            ['N'] = PropKind.BigBush,
            ['K'] = PropKind.Crystal,
            ['A'] = PropKind.Anvil,
            ['J'] = PropKind.Stump,
            ['Q'] = PropKind.Barrel,
            ['E'] = PropKind.Scarecrow,
        };

        private readonly record struct Definition(PropSheet Sheet, Rectangle[] Variants);

        // Source rectangles are in pixels on the sheet. When a prop has more
        // than one, the tile position picks one, so the same layout always
        // looks the same but neighbouring props differ.
        private static readonly Dictionary<PropKind, Definition> Definitions = new()
        {
            [PropKind.TallBoulder] = new(PropSheet.ObjectRocks, new Rectangle[]
            {
                TileSprites.HarvestableRock,
            }),
            [PropKind.RoundBoulder] = new(PropSheet.ObjectRocks, new Rectangle[]
            {
                TileSprites.HarvestableRock,
            }),
            [PropKind.BrownBoulder] = new(PropSheet.ObjectRocks, new Rectangle[]
            {
                TileSprites.HarvestableRock,
            }),
            [PropKind.DeadTree] = new(PropSheet.Vegetation, new Rectangle[]
            {
                new(196, 98, 39, 46),
                new(197, 65, 34, 31),
            }),
            // Small bush: green or autumn orange, chosen by position.
            [PropKind.Bush] = new(PropSheet.Vegetation, new Rectangle[]
            {
                new(2, 5, 29, 27),
                new(146, 5, 29, 27),
            }),
            [PropKind.BigBush] = new(PropSheet.Vegetation, new Rectangle[]
            {
                new(2, 99, 43, 43),
                new(146, 99, 43, 43),
            }),
            [PropKind.Crystal] = new(PropSheet.Rocks, new Rectangle[]
            {
                new(145, 278, 14, 26),
                new(163, 283, 10, 21),
            }),
            [PropKind.Anvil] = new(PropSheet.Tools, new Rectangle[] { new(144, 120, 32, 24) }),
            [PropKind.Stump] = new(PropSheet.Tools, new Rectangle[] { new(0, 125, 16, 19) }),
            [PropKind.Barrel] = new(PropSheet.Furniture, new Rectangle[] { new(752, 72, 16, 24) }),
            [PropKind.Scarecrow] = new(PropSheet.Farm, new Rectangle[] { new(244, 33, 24, 47) }),
        };

        public static PropSheet SheetOf(PropKind kind) => Definitions[kind].Sheet;

        public static Rectangle SourceFor(PropKind kind, Point tile)
        {
            Rectangle[] variants = Definitions[kind].Variants;
            int hash = unchecked(tile.X * 19349663 ^ tile.Y * 73856093);
            return variants[(hash & 0x7fffffff) % variants.Length];
        }

        // Path under Content/ (the .csproj copies every PNG there).
        public static string FileFor(PropSheet sheet)
        {
            return sheet switch
            {
                PropSheet.Rocks => "Rocks/Rocks.png",
                PropSheet.ObjectRocks => "Environment/Props/Static/objects.png",
                PropSheet.Vegetation => "Vegetation/Vegetation.png",
                PropSheet.Tools => "Environment/Props/Static/Tools.png",
                PropSheet.Furniture => "Environment/Props/Static/Furniture.png",
                _ => "Environment/Props/Static/Farm.png",
            };
        }
    }
}

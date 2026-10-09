using Microsoft.Xna.Framework;

namespace TileQuest
{
    // ====================================================================
    //  FOREST LAYOUT  -  edit this file to design the forest map.
    // ====================================================================
    //
    //  Same rules as VillageLayout.cs: every string is ONE ROW, every
    //  character is ONE TILE. The map is 36 wide (x = 0..35) and 29 tall
    //  (y = 0..28); (0, 0) is the top-left tile.
    //
    //  The forest is now a hand-made Tiled map (Map01.tmx), drawn as two
    //  sprites (see Sprites below). This grid is COLLISION ONLY:
    //    '#'  blocked  (cliffs, tree trunks, stumps, void, unreachable grass)
    //    '.'  walkable (the open meadow - the one combat platform). Canopy
    //         crowns do NOT block: the player walks under them; only trunks do.
    //  It is 36 wide x 29 tall to match the Tiled map. Press F3 in game to see
    //  every blocked tile drawn over the art.
    //
    //  .   walkable grass
    //  #   blocked
    //  X   the gate - exactly one, at ForestLayout.Gate (x=18, y=17); this is
    //      also where the player arrives.
    //
    //  Keep all 29 rows exactly 36 characters wide.
    public static class ForestLayout
    {
        public static readonly string[] Rows =
        {
        //          0         1         2         3
        //          012345678901234567890123456789012345
        /* y= 0 */ "####################################",
        /* y= 1 */ "####################################",
        /* y= 2 */ "####################################",
        /* y= 3 */ "####################################",
        /* y= 4 */ "####################################",
        /* y= 5 */ "####################################",
        /* y= 6 */ "####################################",
        /* y= 7 */ "####################################",
        /* y= 8 */ "####################################",
        /* y= 9 */ "####################################",
        /* y=10 */ "####################################",
        /* y=11 */ "####################################",
        /* y=12 */ "####################################",
        /* y=13 */ "...#...#############################",
        /* y=14 */ ".#.....#############################",
        /* y=15 */ ".............#.........#######...###",
        /* y=16 */ "##.....#...#......................#.",
        /* y=17 */ "##................X.#...............",
        /* y=18 */ "###########.........................",
        /* y=19 */ "############........................",
        /* y=20 */ "######################..............",
        /* y=21 */ "#######################.............",  
        /* y=22 */ "#########################..##.######",
        /* y=23 */ "##########################....######",
        /* y=24 */ "####################################",
        /* y=25 */ "####################################",
        /* y=26 */ "####################################",
        /* y=27 */ "####################################",
        /* y=28 */ "####################################",
        //          012345678901234567890123456789012345
        //          0         1         2         3
        };

        // Center tile of the three-tile cave approach on row y=15.
        public static readonly Point Gate = new Point(18, 17);
        public static readonly Point DungeonEntrance = new Point(31, 15);
        public const int DungeonEntranceWidth = 3;

        // ------------------------------------------------------------------
        //  HAND-PAINTED SPRITES
        //
        //  Drop a PNG under Content/ (e.g. Content/Environment/Forest/foothill.png),
        //  list it in Sprites, and it is drawn at that tile. Paint at native 16 px
        //  scale (the game draws it 2x), canvas a multiple of 16, transparent
        //  background. A file that does not exist yet is skipped, so you can list
        //  sprites before you have painted them.
        //
        //  Collision does NOT come from the sprites: it stays in Rows above
        //  ('#' blocked, '.' walkable). Press F3 in game to see every blocked tile.
        //
        //  When the painted art replaces the generated walls and canopy, set
        //  UseGeneratedVisuals to false. Collision is unaffected.
        // ------------------------------------------------------------------
        public static readonly bool UseGeneratedVisuals = false;

        public enum SpriteLayer
        {
            Ground,        // on the floor, under everything
            BehindPlayer,  // under the player and trees (detail sprites)
            OverPlayer,    // on top of the player (canopy)
        }

        public readonly record struct ForestSprite(
            string File, int TileX, int TileY, SpriteLayer Layer, bool Flip = false);

        public static readonly ForestSprite[] Sprites =
        {
            // Everything from Map01.tmx except the canopy layers (tree1-3).
            new("forest_ground.png", 0, 0, SpriteLayer.Ground),
            // The canopy layers, drawn over the player.
            new("forest_canopy.png", 0, 0, SpriteLayer.OverPlayer),
        };

        // A piece of Wall_Variations.png drawn over the wall at tile (TileX, TileY).
        // Source is in art pixels (16 px = 1 tile). Draw order: after the tile
        // walls, before the trees.
        public readonly record struct WallPanel(Rectangle Source, int TileX, int TileY, bool Flip = false);

        // Mine entrance + big cave opening: the 16x5-tile base panel, flipped so
        // the mine frame sits on the left. Its grass edge lands on row 9, the
        // last row the left block's cliff face covers (the block's base is row 7).
        public static readonly WallPanel[] Panels =
        {
            new(new Rectangle(0, 80, 256, 80), 0, 5, Flip: true),
        };
    }
}
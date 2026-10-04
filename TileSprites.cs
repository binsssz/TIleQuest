using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    // Single home for sprite-sheet coordinates. Everything here is a source
    // rectangle in pixels, meant to be passed as the sourceRectangle argument
    // of SpriteBatch.Draw. Nothing in this class loads or draws anything.
    //
    // Sheets in use:
    //   "Tilesets/All free tiles.png" -> free-form tree crops (Trees below).
    //   "Ground/Tiles/Floors_Tiles.png" -> grass ground + dirt patch textures.
    //   "Rocks/Rocks.png" -> small pebble clusters (Rocks below).
    //   "Vegetation/Vegetation.png" and "Shadows/Shadows.png" supply decorations.
    //   "Characters/player.png" is loaded as a stand-alone image.
    //   The player's 48x48 animation cells are cut up in Game1.DrawPlayer.
    public static class TileSprites
    {
        // Both sheets are drawn on a 16px grid; TileSize scales it up.
        public const int GridSize = 16;
        // Plain grass ground. This must be the SAME green the patch frame below
        // is painted with (row 10 of the sheet), otherwise a lighter square
        // halo shows up around every patch.
        public static readonly Rectangle Grass = new(16, 160, GridSize, GridSize);
        public static readonly Rectangle Stone = new(256, 0, GridSize, GridSize);
        public static readonly Rectangle BrightCrackedRoad = new(96, 160, GridSize, GridSize);

        // Dirt patch = two layers drawn on the same spot:
        //   1. DirtPatchFill: the flat brown tile, repeated under the whole patch.
        //   2. DirtPatchFrame: a 5x5-tile GREEN frame whose middle is cut out in a
        //      round, jagged shape. The cut-out is what lets the brown show through.
        // The brown frames on the sheet (x 160-240) are the opposite thing: brown
        // with a grass-shaped hole. They are for grass islands inside dirt, not
        // for dirt on grass, so they are not used here.
        // The frame's four corner tiles are transparent, so the fill is only laid
        // under the plus-shaped footprint (see Game1.DrawDirtPatches).
        public const int DirtPatchSizeInTiles = 5;
        public static readonly Rectangle DirtPatchFill = new(176, 160, GridSize, GridSize);
        public static readonly Rectangle DirtPatchFrame =
            new(0, 0, DirtPatchSizeInTiles * GridSize, DirtPatchSizeInTiles * GridSize);
        public static readonly Rectangle TreeShadow = new(0, 0, 112, 48);
        public static readonly Rectangle[] Vegetation =
        {
            new(5, 165, 7, 9),
            new(21, 164, 7, 10),
            new(36, 166, 9, 6),
            new(67, 164, 11, 8),
            new(97, 177, 14, 15),
            new(229, 176, 8, 16),
        };

        // Tiny grey pebbles on "Rocks/Rocks.png", sprinkled over the village grass
        // as walkable decoration (Game1.DrawVillageGroundDetails).
        public static readonly Rectangle[] FloorPebbles =
        {
            new(100, 71, 7, 4),
            new(116, 70, 7, 5),
            new(132, 70, 8, 6),
            new(147, 69, 9, 7),
        };

        // Reed / fern clumps on "Vegetation/Vegetation.png", drawn on village
        // tall-grass tiles.
        public static readonly Rectangle[] GrassTufts =
        {
            new(112, 166, 32, 26),
            new(145, 166, 15, 26),
            new(97, 177, 14, 15),
            new(164, 176, 9, 16),
        };

        // Tree sprites on "Tilesets/All free tiles.png", including trunk + shadow.
        // Only the narrow variants are used (22px wide = 1.4 tiles).
        public static readonly Rectangle[] Trees =
        {
            new(13, 69, 22, 54),   // tall, dark
            new(61, 81, 22, 42),   // short, dark
            new(157, 69, 22, 54),  // tall, light
            new(205, 81, 22, 42),  // short, light
        };

        // Small pebble clusters on "Rocks/Rocks.png".
        public static readonly Rectangle[] Rocks =
        {
            new(64, 32, GridSize, GridSize),
            new(80, 32, GridSize, GridSize),
            new(64, 48, GridSize, GridSize),
            new(80, 48, GridSize, GridSize),
        };
    }
}
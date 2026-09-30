using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    // Single home for sprite-sheet coordinates. Everything here is a source
    // rectangle in pixels, meant to be passed as the sourceRectangle argument
    // of SpriteBatch.Draw. Nothing in this class loads or draws anything.
    //
    // Sheets in use:
    //   "All free tiles.png" -> trees (Trees below). 16x16 grid, free-form crops.
    //   "Rocks.png"          -> rocks (Rocks below). Plain 16x16 grid cells.
    //   "grass.png", "tallgrass_overlay.png", "player.png" are stand-alone
    //   images; the player's 48x48 animation cells are cut up in Game1.DrawPlayer.
    public static class TileSprites
    {
        // Both sheets are drawn on a 16px grid; TileSize scales it up.
        public const int GridSize = 16;

        // Tree sprites on "All free tiles.png", including trunk + shadow.
        // Only the narrow variants are used (22px wide = 1.4 tiles).
        public static readonly Rectangle[] Trees =
        {
            new(13, 69, 22, 54),   // tall, dark
            new(61, 81, 22, 42),   // short, dark
            new(157, 69, 22, 54),  // tall, light
            new(205, 81, 22, 42),  // short, light
        };

        // Rock sprites on "Rocks.png": columns 4-11 of rows 0-1, one cell each.
        public static readonly Rectangle[] Rocks = BuildRocks();

        private static Rectangle[] BuildRocks()
        {
            var rocks = new List<Rectangle>();
            for (int row = 0; row < 2; row++)
            {
                for (int column = 4; column < 12; column++)
                {
                    rocks.Add(new Rectangle(column * GridSize, row * GridSize, GridSize, GridSize));
                }
            }
            return rocks.ToArray();
        }
    }
}

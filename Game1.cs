using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TileQuest
{
    public class Game1 : Game
    {
        private const int TileSize = 32;

        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch = null!;

        // Real sprite textures, direct-loaded (no Content Pipeline/mgcb tool
        // needed — see LoadTexture below and the .csproj's
        // CopyToOutputDirectory entry for Content/*.png).
        private Texture2D _floorTilesTexture = null!;
        private Texture2D _vegetationTexture = null!;
        private Texture2D _shadowsTexture = null!;
        private Texture2D _tree1Texture = null!;
        private Texture2D _tree2Texture = null!;
        private Texture2D _tree3Texture = null!;
        private Texture2D _rocksTexture = null!;
        private Texture2D _churchTexture = null!;
        private Texture2D _house1Texture = null!;
        private Texture2D _house2Texture = null!;
        private Texture2D _playerTexture = null!;
        private DrawTree[] _trees = Array.Empty<DrawTree>();
        private DrawRock[] _rocks = Array.Empty<DrawRock>();
        private (Rectangle Source, Rectangle Destination)[] _vegetation = Array.Empty<(Rectangle, Rectangle)>();
        private Rectangle[] _dirtPatches = Array.Empty<Rectangle>(); // tile-unit footprints

        private TileMap _map = null!;
        private Player _player = null!;
        private Camera2D _camera = null!;

        // player.png is a grid of 48x48 cells, 6 frames per row:
        //   rows 0-2 = standing (down, side, up), rows 3-5 = walking (down, side, up).
        // The side row faces right; facing left is that row flipped.
        private const int PlayerCellSize = 48;
        private const int PlayerFrameCount = 6;
        private const float WalkFramesPerSecond = 12f;
        private const int PlayerSpriteHeight = 21; // head-to-feet in source pixels, scaled to one tile

        // Same 32x32 crop of every cell, so the character doesn't jitter
        // between frames. The bottom edge is where the feet/shadow sit.
        private static readonly Rectangle PlayerCrop = new(8, 12, 32, 32);

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 800,
                PreferredBackBufferHeight = 600
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _map = new TileMap(width: 60, height: 45, tileSize: TileSize);
            _player = new Player(_map.SpawnPoint, TileSize, tilesPerSecond: 8f);

            _camera = new Camera2D(
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight,
                _map.Width,
                _map.Height,
                TileSize);
            _camera.SnapTo(_player.PixelPosition);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _tree1Texture = LoadTexture("Tree1.png");
            _tree2Texture = LoadTexture("Tree 2.png");
            _tree3Texture = LoadTexture("Tree 3.png");
            _trees = DrawTree.CreateForest(_map, _tree1Texture, _tree2Texture, _tree3Texture, TileSize);
            _floorTilesTexture = LoadTexture("Floors_Tiles.png");
            _dirtPatches = CreateDirtPatches();
            _vegetationTexture = LoadTexture("Vegetation.png");
            _shadowsTexture = LoadTexture("Shadows.png");
            _vegetation = CreateVegetation();
            _rocksTexture = LoadTexture("Rocks.png");
            _rocks = DrawRock.CreateFor(_map, _rocksTexture, TileSize);
            _churchTexture = LoadTexture("CHURCH.png");
            _house1Texture = LoadTexture("HOUSE 1.png");
            _house2Texture = LoadTexture("HOUSE 2.png");
            _playerTexture = LoadTexture("player.png");
        }

        // Direct-load, no Content Pipeline: reads PNG bytes straight from the
        // output directory (copied there at build time per the .csproj) via
        // TitleContainer, which works the same on Windows/macOS/Linux.
        private Texture2D LoadTexture(string fileName)
        {
            using Stream stream = TitleContainer.OpenStream($"Content/{fileName}");
            return Texture2D.FromStream(GraphicsDevice, stream);
        }

        protected override void Update(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();
            if (keyboard.IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            _player.Update(gameTime, _map, keyboard);
            _camera.Update(gameTime, _player.PixelPosition, followSpeed: 8f);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(transformMatrix: _camera.GetTransformationMatrix(), samplerState: SamplerState.PointClamp);

            DrawTiles();
            DrawDirtPatches();
            DrawTreeShadows();
            DrawRocks();
            DrawVegetation();
            DrawVillageStructures();
            DrawTreesAndPlayer();

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawTiles()
        {
            foreach (var (gridPos, tile) in _map.AllTiles())
            {
                var rect = new Rectangle(gridPos.X * TileSize, gridPos.Y * TileSize, TileSize, TileSize);

                _spriteBatch.Draw(_floorTilesTexture, rect, TileSprites.Grass, Color.White);
            }
        }

        // Dirt patches are fixed-size (5x5 tiles) so the sprite is always drawn at
        // an exact 2x scale; stretching it to other sizes would distort the pixels.
        private Rectangle[] CreateDirtPatches()
        {
            const int patchCount = 18;
            var patches = new List<Rectangle>();
            var random = new Random();

            // A row of patches in the village; spaced one patch-width apart so
            // they sit side by side without overlapping.
            int centerX = _map.Width / 2;
            int villagePathY = _map.Height / 2 + 3;
            foreach (int x in new[] { centerX - 5, centerX, centerX + 5 })
            {
                TryAddDirtPatch(new Point(x, villagePathY), patches);
            }

            for (int attempt = 0; attempt < patchCount * 80 && patches.Count < patchCount; attempt++)
            {
                var center = new Point(random.Next(_map.Width), random.Next(_map.Height));
                TryAddDirtPatch(center, patches);
            }

            return patches.ToArray();
        }

        private bool TryAddDirtPatch(Point center, List<Rectangle> patches)
        {
            int size = TileSprites.DirtPatchSizeInTiles;
            int half = size / 2;
            var tileArea = new Rectangle(center.X - half, center.Y - half, size, size);

            if (tileArea.Left < 0 || tileArea.Top < 0 ||
                tileArea.Right > _map.Width || tileArea.Bottom > _map.Height)
            {
                return false;
            }

            foreach (var patch in patches)
            {
                if (patch.Intersects(tileArea))
                {
                    return false;
                }
            }

            for (int x = tileArea.Left; x < tileArea.Right; x++)
            {
                for (int y = tileArea.Top; y < tileArea.Bottom; y++)
                {
                    TileType tile = _map.GetTile(new Point(x, y));
                    if (tile != TileType.Grass && tile != TileType.TallGrass)
                    {
                        return false;
                    }
                }
            }

            patches.Add(tileArea); // stored in TILE units; converted to pixels when drawn
            return true;
        }

        private void DrawDirtPatches()
        {
            int size = TileSprites.DirtPatchSizeInTiles;

            foreach (var area in _dirtPatches)
            {
                // Layer 1: brown fill, one tile at a time, skipping the four
                // corner tiles (the frame is transparent there, so fill would
                // show as little brown squares sticking out).
                for (int row = 0; row < size; row++)
                {
                    for (int column = 0; column < size; column++)
                    {
                        bool isCorner = (row == 0 || row == size - 1) && (column == 0 || column == size - 1);
                        if (isCorner)
                        {
                            continue;
                        }

                        var tile = new Rectangle((area.X + column) * TileSize, (area.Y + row) * TileSize, TileSize, TileSize);
                        _spriteBatch.Draw(_floorTilesTexture, tile, TileSprites.DirtPatchFill, Color.White);
                    }
                }

                // Layer 2: green frame on top. Its cut-out reveals the fill.
                var destination = new Rectangle(area.X * TileSize, area.Y * TileSize, size * TileSize, size * TileSize);
                _spriteBatch.Draw(_floorTilesTexture, destination, TileSprites.DirtPatchFrame, Color.White);
            }
        }

        private (Rectangle Source, Rectangle Destination)[] CreateVegetation()
        {
            var decorations = new List<(Rectangle Source, Rectangle Destination)>();
            var random = new Random();
            float scale = TileSize / (float)TileSprites.GridSize;

            foreach (var (position, tile) in _map.AllTiles())
            {
                if (tile != TileType.TallGrass || random.NextDouble() >= 0.75 || IsInsideDirtPatch(position))
                {
                    continue;
                }

                Rectangle source = TileSprites.Vegetation[random.Next(TileSprites.Vegetation.Length)];
                int width = (int)Math.Round(source.Width * scale);
                int height = (int)Math.Round(source.Height * scale);
                int offsetX = random.Next(Math.Max(1, TileSize - width + 1));
                int x = position.X * TileSize + offsetX;
                int y = (position.Y + 1) * TileSize - height;
                decorations.Add((source, new Rectangle(x, y, width, height)));
            }

            return decorations.ToArray();
        }

        private bool IsInsideDirtPatch(Point position)
        {
            foreach (var patch in _dirtPatches)
            {
                if (patch.Contains(position))
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawTreeShadows()
        {
            foreach (var tree in _trees)
            {
                tree.DrawShadow(_spriteBatch, _shadowsTexture);
            }
        }

        private void DrawVegetation()
        {
            foreach (var (source, destination) in _vegetation)
            {
                _spriteBatch.Draw(_vegetationTexture, destination, source, Color.White);
            }
        }

        private void DrawRocks()
        {
            foreach (var rock in _rocks)
            {
                rock.Draw(_spriteBatch);
            }
        }

        private void DrawVillageStructures()
        {
            foreach (var structure in _map.VillageStructures)
            {
                Texture2D texture = structure.Type switch
                {
                    TileType.Church => _churchTexture,
                    TileType.House1 => _house1Texture,
                    TileType.House2 => _house2Texture,
                    _ => null!,
                };
                if (texture == null)
                {
                    continue;
                }

                int footprintWidth = structure.Width * TileSize;
                int width = (int)Math.Round(texture.Width * VillageStructure.RenderScale);
                int height = (int)Math.Round(texture.Height * VillageStructure.RenderScale);
                int x = structure.Position.X * TileSize + (footprintWidth - width) / 2;
                int y = (structure.Position.Y + structure.Height) * TileSize - height;
                var destination = new Rectangle(x, y, width, height);
                _spriteBatch.Draw(texture, destination, Color.White);
            }
        }

        // Depth sorting: trees are sorted top-to-bottom, so draw the ones whose
        // base is at or above the player's feet first, then the player, then
        // the trees lower on the screen. That way the player walks in front of
        // a tree when south of it and behind it (hidden by the canopy) when
        // north of it.
        private void DrawTreesAndPlayer()
        {
            int playerFootY = (int)_player.PixelPosition.Y + TileSize;

            int i = 0;
            while (i < _trees.Length && _trees[i].BaseY <= playerFootY)
            {
                _trees[i].Draw(_spriteBatch);
                i++;
            }

            DrawPlayer();

            for (; i < _trees.Length; i++)
            {
                _trees[i].Draw(_spriteBatch);
            }
        }

        private void DrawPlayer()
        {
            int row = _player.Facing switch
            {
                FacingDirection.Down => 0,
                FacingDirection.Up => 2,
                _ => 1, // Left and Right share the side row
            };
            if (_player.IsWalking)
            {
                row += 3;
            }

            int frame = _player.IsWalking
                ? (int)(_player.AnimationTime * WalkFramesPerSecond) % PlayerFrameCount
                : 0;

            var source = new Rectangle(
                frame * PlayerCellSize + PlayerCrop.X,
                row * PlayerCellSize + PlayerCrop.Y,
                PlayerCrop.Width,
                PlayerCrop.Height);

            float scale = TileSize / (float)PlayerSpriteHeight;
            int size = (int)Math.Round(PlayerCrop.Width * scale);
            int x = (int)_player.PixelPosition.X + (TileSize - size) / 2;
            int y = (int)_player.PixelPosition.Y + TileSize - size;
            var destination = new Rectangle(x, y, size, size);

            var effects = _player.Facing == FacingDirection.Left
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            _spriteBatch.Draw(_playerTexture, destination, source, Color.White, 0f, Vector2.Zero, effects, 0f);
        }
    }
}
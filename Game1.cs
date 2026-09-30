using System;
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
        private Texture2D _tileSheet = null!;
        private Texture2D _grassTexture = null!;
        private Texture2D _tallGrassOverlayTexture = null!;
        private Texture2D _rocksTexture = null!;
        private Texture2D _playerTexture = null!;
        private DrawTree[] _trees = Array.Empty<DrawTree>();
        private DrawRock[] _rocks = Array.Empty<DrawRock>();

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

            _tileSheet = LoadTexture("All free tiles.png");
            _trees = DrawTree.CreateForest(_map, _tileSheet, TileSize);
            _grassTexture = LoadTexture("grass.png");
            _tallGrassOverlayTexture = LoadTexture("tallgrass_overlay.png");
            _rocksTexture = LoadTexture("Rocks.png");
            _rocks = DrawRock.CreateFor(_map, _rocksTexture, TileSize);
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
            DrawRocks();
            DrawTreesAndPlayer();

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawTiles()
        {
            foreach (var (gridPos, tile) in _map.AllTiles())
            {
                var rect = new Rectangle(gridPos.X * TileSize, gridPos.Y * TileSize, TileSize, TileSize);

                // Ground sprite first — every tile stands on grass; tall
                // grass/trees/rocks draw an overlay sprite on top of it,
                // same layering the colored-rectangle version used.
                _spriteBatch.Draw(_grassTexture, rect, Color.White);

                switch (tile)
                {
                    case TileType.TallGrass:
                        _spriteBatch.Draw(_tallGrassOverlayTexture, rect, Color.White);
                        break;
                    case TileType.Tree:
                        // Drawn by DrawTree (one sprite per tree tile, in
                        // Draw), since tree sprites are taller than a tile.
                        break;
                }
            }
        }

        private void DrawRocks()
        {
            foreach (var rock in _rocks)
            {
                rock.Draw(_spriteBatch);
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
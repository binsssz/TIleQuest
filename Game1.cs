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
        private Texture2D _shadowsTexture = null!;
        private Texture2D _tree1Texture = null!;
        private Texture2D _tree2Texture = null!;
        private Texture2D _tree3Texture = null!;
        private Texture2D _rocksTexture = null!;
        private Texture2D _churchTexture = null!;
        private Texture2D _house1Texture = null!;
        private Texture2D _house2Texture = null!;
        private Texture2D _playerTexture = null!;
        private Texture2D _pixelTexture = null!;
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
            _map = new TileMap(width: 36, height: 28, tileSize: TileSize);
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
            _shadowsTexture = LoadTexture("Shadows.png");
            _rocksTexture = LoadTexture("Rocks.png");
            _rocks = DrawRock.CreateFor(_map, _rocksTexture, TileSize);
            _churchTexture = LoadTexture("CHURCH.png");
            _house1Texture = LoadTexture("HOUSE 1.png");
            _house2Texture = LoadTexture("HOUSE 2.png");
            _playerTexture = LoadTexture("player.png");
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
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
            DrawTreeShadows();
            DrawRocks();
            DrawVillageStructures();
            DrawVillageLanterns();
            DrawTreesAndPlayer();

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawTiles()
        {
            foreach (var (gridPos, tile) in _map.AllTiles())
            {
                var rect = new Rectangle(gridPos.X * TileSize, gridPos.Y * TileSize, TileSize, TileSize);

                Rectangle source = tile == TileType.DirtPath ? TileSprites.DirtPatchFill : TileSprites.Stone;
                _spriteBatch.Draw(_floorTilesTexture, rect, source, Color.White);
            }
        }

        private void DrawTreeShadows()
        {
            foreach (var tree in _trees)
            {
                tree.DrawShadow(_spriteBatch, _shadowsTexture);
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
                if (structure.Type == TileType.VillageHearth)
                {
                    DrawVillageHearth(structure);
                    continue;
                }

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

        private void DrawVillageHearth(VillageStructure hearth)
        {
            int x = hearth.Position.X * TileSize;
            int y = hearth.Position.Y * TileSize;
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 5, y + 24, 54, 31), new Color(70, 75, 82));
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 9, y + 19, 12, 10), new Color(145, 150, 155));
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 42, y + 19, 12, 10), new Color(145, 150, 155));
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 13, y + 42, 38, 7), new Color(100, 65, 42));
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 16, y + 36, 32, 7), new Color(125, 79, 45));
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 25, y + 17, 16, 24), new Color(225, 93, 34));
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 29, y + 23, 9, 16), new Color(255, 190, 54));
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 31, y + 28, 4, 9), new Color(255, 237, 147));
        }

        private void DrawVillageLanterns()
        {
            foreach (var (position, tile) in _map.AllTiles())
            {
                if (tile != TileType.VillageLantern)
                {
                    continue;
                }

                int x = position.X * TileSize;
                int y = position.Y * TileSize;
                _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 3, y + 3, 26, 26), new Color(255, 166, 64, 40));
                _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 9, y + 8, 14, 17), new Color(92, 61, 42));
                _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 11, y + 10, 10, 8), new Color(255, 190, 74));
                _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 13, y + 12, 6, 4), new Color(255, 239, 166));
                _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 14, y + 24, 4, 6), new Color(78, 58, 43));
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
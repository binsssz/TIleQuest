using System;
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
        private Texture2D _pixel = null!;

        private TileMap _map = null!;
        private Player _player = null!;
        private Camera2D _camera = null!;
        private readonly Random _random = new();

        // Simple placeholder feedback for the encounter system until Phase 4
        // adds a real battle screen: a brief white flash + console log.
        private float _encounterFlashSecondsRemaining;

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
            _player.OnTileEntered += HandleTileEntered;

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

            // A single white pixel, tinted per draw call, stands in for real
            // sprites for now — no Content pipeline needed for this milestone.
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        private void HandleTileEntered(Point gridPos)
        {
            if (_map.GetTile(gridPos) != TileType.TallGrass)
            {
                return;
            }

            const double encounterChance = 0.15;
            if (_random.NextDouble() < encounterChance)
            {
                _encounterFlashSecondsRemaining = 0.25f;
                Console.WriteLine("Wild encounter triggered! (battle screen goes here in Phase 4)");
            }
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

            if (_encounterFlashSecondsRemaining > 0)
            {
                _encounterFlashSecondsRemaining -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(transformMatrix: _camera.GetTransformationMatrix(), samplerState: SamplerState.PointClamp);

            DrawTiles();
            DrawPlayer();

            if (_encounterFlashSecondsRemaining > 0)
            {
                var viewport = GraphicsDevice.Viewport;
                var flashRect = new Rectangle((int)_camera.Position.X, (int)_camera.Position.Y, viewport.Width, viewport.Height);
                _spriteBatch.Draw(_pixel, flashRect, new Color(255, 255, 255, 60));
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawTiles()
        {
            foreach (var (gridPos, tile) in _map.AllTiles())
            {
                var rect = new Rectangle(gridPos.X * TileSize, gridPos.Y * TileSize, TileSize, TileSize);

                // Ground first — trees and rocks still stand on grass, they
                // don't replace it.
                Color groundColor = tile == TileType.TallGrass ? new Color(40, 110, 40) : new Color(140, 200, 120);
                _spriteBatch.Draw(_pixel, rect, groundColor);

                switch (tile)
                {
                    case TileType.Tree:
                        DrawTree(rect);
                        break;
                    case TileType.Rock:
                        DrawRock(rect);
                        break;
                }
            }
        }

        private void DrawTree(Rectangle tile)
        {
            // Trunk: a thin brown rectangle at the bottom-center of the tile.
            int trunkWidth = Math.Max(2, TileSize / 4);
            var trunk = new Rectangle(tile.Center.X - trunkWidth / 2, tile.Bottom - TileSize / 3, trunkWidth, TileSize / 3);
            _spriteBatch.Draw(_pixel, trunk, new Color(90, 60, 30));

            // Canopy: a darker green block covering most of the tile, placeholder
            // for a round treetop sprite later.
            var canopy = new Rectangle(tile.X + 2, tile.Y, tile.Width - 4, tile.Height * 2 / 3);
            _spriteBatch.Draw(_pixel, canopy, new Color(20, 90, 30));
        }

        private void DrawRock(Rectangle tile)
        {
            int inset = Math.Max(2, TileSize / 6);
            var body = new Rectangle(tile.X + inset, tile.Y + inset, tile.Width - inset * 2, tile.Height - inset * 2);
            _spriteBatch.Draw(_pixel, body, new Color(120, 120, 120));

            var highlight = new Rectangle(body.X + 2, body.Y + 2, Math.Max(2, body.Width / 3), Math.Max(2, body.Height / 3));
            _spriteBatch.Draw(_pixel, highlight, new Color(170, 170, 170));
        }

        private void DrawPlayer()
        {
            var playerRect = new Rectangle((int)_player.PixelPosition.X, (int)_player.PixelPosition.Y, TileSize, TileSize);
            _spriteBatch.Draw(_pixel, playerRect, Color.CornflowerBlue);

            // A small notch on the facing edge stands in for a directional
            // sprite until real character art is dropped in.
            const int notch = 6;
            Rectangle facingRect = _player.Facing switch
            {
                FacingDirection.Up => new Rectangle(playerRect.Center.X - notch / 2, playerRect.Top, notch, notch),
                FacingDirection.Down => new Rectangle(playerRect.Center.X - notch / 2, playerRect.Bottom - notch, notch, notch),
                FacingDirection.Left => new Rectangle(playerRect.Left, playerRect.Center.Y - notch / 2, notch, notch),
                _ => new Rectangle(playerRect.Right - notch, playerRect.Center.Y - notch / 2, notch, notch),
            };
            _spriteBatch.Draw(_pixel, facingRect, Color.DarkBlue);
        }
    }
}

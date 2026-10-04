using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        // CopyToOutputDirectory entry for Content/**/*.png).
        private Texture2D _floorTilesTexture = null!;
        private PathCorners _pathCorners = null!;
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
        private Texture2D _pixelTexture = null!;
        private Texture2D _bonfireTexture = null!;
        private Texture2D _bonfireFlameTexture = null!;
        private DrawTree[] _trees = Array.Empty<DrawTree>();
        private DrawRock[] _rocks = Array.Empty<DrawRock>();
        private readonly Dictionary<PropSheet, Texture2D> _propSheets = new();
        private Texture2D _wellTexture = null!;
        private DrawProp[] _props = Array.Empty<DrawProp>();
        // Trees and props together, back-to-front, so the player can walk
        // between them (see DrawTreesAndPlayer).
        private IDepthSorted[] _depthSorted = Array.Empty<IDepthSorted>();
        private bool _showHitboxes; // F3 debug overlay, see DrawHitboxes

        private TileMap _map = null!;
        private readonly Dictionary<WorldMapType, TileMap> _maps = new();
        private Player _player = null!;
        private Camera2D _camera = null!;
        private MapTravel? _pendingTravel;
        private KeyboardState _previousKeyboard;

        // player.png is a grid of 48x48 cells, 6 frames per row:
        //   rows 0-2 = standing (down, side, up), rows 3-5 = walking (down, side, up).
        // The side row faces right; facing left is that row flipped.
        private const int PlayerCellSize = 48;
        private const int PlayerFrameCount = 6;
        private const float WalkFramesPerSecond = 12f;
        private const int PlayerSpriteHeight = 21; // head-to-feet in source pixels, scaled to one tile
        private const int BonfireFrameSize = 32;
        private const int BonfireFrameCount = 4;
        private const float BonfireFramesPerSecond = 8f;
        private const int BonfireFlameFrameWidth = 32;
        private const int BonfireFlameFrameHeight = 48;

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
            _maps.Add(WorldMapType.Village, new TileMap(width: 36, height: 28, tileSize: TileSize, WorldMapType.Village));
            _maps.Add(WorldMapType.Forest, new TileMap(width: 36, height: 28, tileSize: TileSize, WorldMapType.Forest));
            _map = _maps[WorldMapType.Village];
            _player = new Player(_map.SpawnPoint, TileSize, tilesPerSecond: 8f);
            _player.OnTileEntered += OnPlayerEnteredTile;

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

            _tree1Texture = LoadTexture("Trees/Tree1.png");
            _tree2Texture = LoadTexture("Trees/Tree 2.png");
            _tree3Texture = LoadTexture("Trees/Tree 3.png");
            _floorTilesTexture = LoadTexture("Ground/Tiles/Floors_Tiles.png");
            _pathCorners = new PathCorners(GraphicsDevice, _floorTilesTexture);
            _vegetationTexture = LoadTexture("Vegetation/Vegetation.png");
            _shadowsTexture = LoadTexture("Shadows/Shadows.png");
            _rocksTexture = LoadTexture("Rocks/Rocks.png");
            _churchTexture = LoadTexture("Buildings/CHURCH.png");
            _house1Texture = LoadTexture("Buildings/HOUSE 1.png");
            _house2Texture = LoadTexture("Buildings/HOUSE 2.png");
            _playerTexture = LoadTexture("Characters/player.png");
            _wellTexture = LoadTexture("Traversal/PIT - DAY.png");
            _bonfireTexture = LoadTexture("Environment/Structures/Stations/Bonfire/Bonfire_01-Sheet.png");
            _bonfireFlameTexture = LoadTexture("Environment/Structures/Stations/Bonfire/Fire_01-Sheet.png");
            _propSheets[PropSheet.Rocks] = _rocksTexture;
            _propSheets[PropSheet.Vegetation] = _vegetationTexture;
            foreach (PropSheet sheet in new[] { PropSheet.Tools, PropSheet.Furniture, PropSheet.Farm })
            {
                _propSheets[sheet] = LoadTexture(PropCatalog.FileFor(sheet));
            }
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
            PrepareMapVisuals();
        }

        // Direct-load, no Content Pipeline: reads PNG bytes straight from the
        // output directory (copied there at build time per the .csproj) via
        // TitleContainer, which works the same on Windows/macOS/Linux.
        private Texture2D LoadTexture(string fileName)
        {
            using Stream stream = TitleContainer.OpenStream($"Content/{fileName}");
            return Texture2D.FromStream(GraphicsDevice, stream);
        }

        private void PrepareMapVisuals()
        {
            _trees = DrawTree.CreateForest(_map, _tree1Texture, _tree2Texture, _tree3Texture, TileSize);
            _rocks = DrawRock.CreateFor(_map, _rocksTexture, TileSize);
            _props = DrawProp.CreateFor(_map, _propSheets, TileSize);
            // OrderBy is stable, so trees keep their left-to-right order within a row.
            _depthSorted = _trees.Cast<IDepthSorted>().Concat(_props).OrderBy(item => item.BaseY).ToArray();
        }

        private bool WasPressed(KeyboardState keyboard, Keys key)
        {
            return keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
        }

        private void OnPlayerEnteredTile(Point position)
        {
            if (_map.TryGetTravel(position, out var travel))
            {
                _pendingTravel = travel;
            }
        }

        private void SwitchMap(MapTravel travel)
        {
            if (!_maps.TryGetValue(travel.Destination, out var destination))
            {
                throw new InvalidOperationException($"No map is registered for {travel.Destination}.");
            }
            if (!destination.IsWalkable(travel.ArrivalPoint))
            {
                throw new InvalidOperationException(
                    $"Arrival point {travel.ArrivalPoint} is blocked on the {travel.Destination} map.");
            }

            _map = destination;
            _player.Teleport(travel.ArrivalPoint);
            _camera = new Camera2D(
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight,
                _map.Width,
                _map.Height,
                TileSize);
            _camera.SnapTo(_player.PixelPosition);
            _pendingTravel = null;
            PrepareMapVisuals();
        }

        private void DrawTravelPrompt(MapTravel travel)
        {
            const int boxWidth = 340;
            const int boxHeight = 62;
            int x = (_graphics.PreferredBackBufferWidth - boxWidth) / 2;
            int y = _graphics.PreferredBackBufferHeight - boxHeight - 18;

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, boxWidth, boxHeight), new Color(18, 24, 32, 235));
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, boxWidth, 2), Color.Gold);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y + boxHeight - 2, boxWidth, 2), Color.Gold);

            string destination = travel.Destination == WorldMapType.Forest ? "FOREST" : "VILLAGE";
            BitmapFont.Draw(_spriteBatch, _pixelTexture, $"{destination}? Y=YES N=NO", new Point(x + 18, y + 20), 2, Color.White);
            _spriteBatch.End();
        }

        protected override void Update(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();
            if (WasPressed(keyboard, Keys.F3))
            {
                _showHitboxes = !_showHitboxes;
            }

            if (_pendingTravel is MapTravel travel)
            {
                if (WasPressed(keyboard, Keys.Y) || WasPressed(keyboard, Keys.Enter))
                {
                    SwitchMap(travel);
                }
                else if (WasPressed(keyboard, Keys.N) || WasPressed(keyboard, Keys.Escape))
                {
                    _pendingTravel = null;
                }
            }
            else if (WasPressed(keyboard, Keys.Escape))
            {
                Exit();
            }
            else
            {
                _player.Update(gameTime, _map, keyboard);
            }

            _camera.Update(gameTime, _player.PixelPosition, followSpeed: 8f);
            _previousKeyboard = keyboard;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(transformMatrix: _camera.GetTransformationMatrix(), samplerState: SamplerState.PointClamp);

            DrawTiles();
            DrawVillageGroundDetails();
            DrawVillagePlants();
            DrawTreeShadows();
            DrawRocks();
            DrawVillageStructures(gameTime);
            DrawVillageLanterns();
            DrawTreesAndPlayer();

            if (_showHitboxes)
            {
                DrawHitboxes();
            }

            _spriteBatch.End();

            if (_pendingTravel is MapTravel travel)
            {
                DrawTravelPrompt(travel);
            }

            base.Draw(gameTime);
        }

        private void DrawTiles()
        {
            foreach (var (gridPos, tile) in _map.AllTiles())
            {
                var rect = new Rectangle(gridPos.X * TileSize, gridPos.Y * TileSize, TileSize, TileSize);

                if (tile == TileType.VillagePaving)
                {
                    _spriteBatch.Draw(_floorTilesTexture, rect, TileSprites.Grass, Color.White);
                    DrawPavingBesideRoad(gridPos, rect);
                    continue;
                }

                Rectangle source = tile switch
                {
                    TileType.DirtPath => TileSprites.BrightCrackedRoad,
                    TileType.VillageGarden => TileSprites.DirtPatchFill,
                    TileType.VillageLantern => TileSprites.Stone,
                    TileType.VillageHearth => TileSprites.Stone,
                    TileType.Church or
                        TileType.House1 or TileType.House2 =>
                            IsBuildingBaseRow(gridPos, tile) ? TileSprites.Stone : TileSprites.Grass,
                    _ => TileSprites.Grass,
                };
                _spriteBatch.Draw(_floorTilesTexture, rect, source, Color.White);

                if (tile == TileType.ForestExit || tile == TileType.VillageExit)
                {
                    Color markerColor = tile == TileType.ForestExit ? Color.LimeGreen : Color.Goldenrod;
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X + 5, rect.Y + 5, TileSize - 10, TileSize - 10), markerColor);
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X + 10, rect.Y + 10, TileSize - 20, TileSize - 20), Color.Black);
                }
            }

            // Rounded road corners go on top of the finished floor.
            _pathCorners.Draw(_spriteBatch, _map, TileSize);
        }

        private void DrawPavingBesideRoad(Point position, Rectangle tileBounds)
        {
            bool north = _map.GetTile(new Point(position.X, position.Y - 1)) == TileType.DirtPath;
            bool east = _map.GetTile(new Point(position.X + 1, position.Y)) == TileType.DirtPath;
            bool south = _map.GetTile(new Point(position.X, position.Y + 1)) == TileType.DirtPath;
            bool west = _map.GetTile(new Point(position.X - 1, position.Y)) == TileType.DirtPath;

            if (!north && !east && !south && !west)
            {
                _spriteBatch.Draw(_floorTilesTexture, tileBounds, TileSprites.Stone, Color.White);
                return;
            }

            const int sourceDepth = 4;
            int stripDepth = TileSize * sourceDepth / TileSprites.GridSize;
            if (north)
            {
                DrawStoneStrip(
                    new Rectangle(tileBounds.X, tileBounds.Y, TileSize, stripDepth),
                    new Rectangle(TileSprites.Stone.X, TileSprites.Stone.Y, TileSprites.GridSize, sourceDepth));
            }
            if (east)
            {
                DrawStoneStrip(
                    new Rectangle(tileBounds.Right - stripDepth, tileBounds.Y, stripDepth, TileSize),
                    new Rectangle(TileSprites.Stone.Right - sourceDepth, TileSprites.Stone.Y, sourceDepth, TileSprites.GridSize));
            }
            if (south)
            {
                DrawStoneStrip(
                    new Rectangle(tileBounds.X, tileBounds.Bottom - stripDepth, TileSize, stripDepth),
                    new Rectangle(TileSprites.Stone.X, TileSprites.Stone.Bottom - sourceDepth, TileSprites.GridSize, sourceDepth));
            }
            if (west)
            {
                DrawStoneStrip(
                    new Rectangle(tileBounds.X, tileBounds.Y, stripDepth, TileSize),
                    new Rectangle(TileSprites.Stone.X, TileSprites.Stone.Y, sourceDepth, TileSprites.GridSize));
            }
        }

        private void DrawStoneStrip(Rectangle destination, Rectangle source)
        {
            _spriteBatch.Draw(_floorTilesTexture, destination, source, Color.White);
        }

        private bool IsBuildingBaseRow(Point position, TileType type)
        {
            foreach (var structure in _map.VillageStructures)
            {
                if (structure.Type != TileType.House2 &&
                    structure.Type == type &&
                    position.X >= structure.Position.X &&
                    position.X < structure.Position.X + structure.Width &&
                    position.Y == structure.Position.Y + structure.Height - 1)
                {
                    return true;
                }
            }

            return false;
        }

        // Share of village grass tiles that get a loose pebble.
        private const int PebbleChancePercent = 14;

        // Purely visual and deterministic (position hash), like DrawRock: tiny
        // pebbles on bare grass and reed clumps on tall-grass tiles. Both stay
        // walkable and never change the map.
        private void DrawVillageGroundDetails()
        {
            if (_map.MapType != WorldMapType.Village)
            {
                return;
            }

            int scale = TileSize / TileSprites.GridSize;
            foreach (var (position, tile) in _map.AllTiles())
            {
                int hash = unchecked(position.X * 19349663 ^ position.Y * 73856093) & 0x7fffffff;

                if (tile == TileType.Grass)
                {
                    if (hash % 100 >= PebbleChancePercent)
                    {
                        continue;
                    }

                    Rectangle source = TileSprites.FloorPebbles[(hash / 100) % TileSprites.FloorPebbles.Length];
                    int width = source.Width * scale;
                    int height = source.Height * scale;
                    int x = position.X * TileSize + 2 + (hash / 400) % Math.Max(1, TileSize - width - 3);
                    int y = position.Y * TileSize + 8 + (hash / 7000) % Math.Max(1, TileSize - height - 9);
                    _spriteBatch.Draw(_rocksTexture, new Rectangle(x, y, width, height), source, Color.White);
                }
                else if (tile == TileType.TallGrass)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Rectangle source = TileSprites.GrassTufts[(hash / 3 + i) % TileSprites.GrassTufts.Length];
                        int width = source.Width * scale;
                        int height = source.Height * scale;
                        int jitter = (hash >> (4 * i)) % 9 - 4;
                        int x = position.X * TileSize + (TileSize - width) / 2 + jitter;
                        int y = (position.Y + 1) * TileSize - height;
                        _spriteBatch.Draw(_vegetationTexture, new Rectangle(x, y, width, height), source, Color.White);
                    }
                }
            }
        }

        // F3 debug overlay. Red = the wall hitboxes of the church and houses
        // (TileMap / VillageStructure.GetCollisionBounds), orange = whole tiles
        // blocked by props, the well and the hearth. Handy for checking that a
        // hitbox still lines up with its sprite after a PNG or a number changes.
        private void DrawHitboxes()
        {
            var wallColor = new Color(255, 40, 40);
            var tileColor = new Color(255, 150, 0);

            foreach (var structure in _map.VillageStructures)
            {
                foreach (Rectangle bounds in structure.GetCollisionBounds(TileSize))
                {
                    DrawOutlinedBox(bounds, wallColor);
                }

                if (structure.Type == TileType.VillageHearth || structure.Type == TileType.Well)
                {
                    DrawOutlinedBox(
                        new Rectangle(
                            structure.Position.X * TileSize,
                            structure.Position.Y * TileSize,
                            structure.Width * TileSize,
                            structure.Height * TileSize),
                        tileColor);
                }
            }

            foreach (var prop in _map.VillageProps)
            {
                DrawOutlinedBox(
                    new Rectangle(prop.Tile.X * TileSize, prop.Tile.Y * TileSize, TileSize, TileSize),
                    tileColor);
            }
        }

        private void DrawOutlinedBox(Rectangle box, Color color)
        {
            _spriteBatch.Draw(_pixelTexture, box, color * 0.25f);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(box.X, box.Y, box.Width, 2), color);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(box.X, box.Bottom - 2, box.Width, 2), color);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(box.X, box.Y, 2, box.Height), color);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(box.Right - 2, box.Y, 2, box.Height), color);
        }

        private void DrawVillagePlants()
        {
            if (_map.MapType != WorldMapType.Village)
            {
                return;
            }

            foreach (var (position, tile) in _map.AllTiles())
            {
                if (tile != TileType.VillageFlower && tile != TileType.VillageGarden)
                {
                    continue;
                }

                int hash = unchecked(position.X * 19349663 ^ position.Y * 73856093);
                Rectangle source = TileSprites.Vegetation[(hash & 0x7fffffff) % TileSprites.Vegetation.Length];
                int width = (int)Math.Round(source.Width * TileSize / (float)TileSprites.GridSize);
                int height = (int)Math.Round(source.Height * TileSize / (float)TileSprites.GridSize);
                int x = position.X * TileSize + (TileSize - width) / 2;
                int y = (position.Y + 1) * TileSize - height;
                _spriteBatch.Draw(_vegetationTexture, new Rectangle(x, y, width, height), source, Color.White);
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

        private void DrawVillageStructures(GameTime gameTime)
        {
            foreach (var structure in _map.VillageStructures)
            {
                if (structure.Type == TileType.VillageHearth)
                {
                    DrawVillageHearth(structure, gameTime);
                    continue;
                }

                Texture2D texture = structure.Type switch
                {
                    TileType.Church => _churchTexture,
                    TileType.House1 => _house1Texture,
                    TileType.House2 => _house2Texture,
                    TileType.Well => _wellTexture,
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

        private void DrawVillageHearth(VillageStructure hearth, GameTime gameTime)
        {
            int frame = (int)(gameTime.TotalGameTime.TotalSeconds * BonfireFramesPerSecond) % BonfireFrameCount;
            var source = new Rectangle(frame * BonfireFrameSize, 0, BonfireFrameSize, BonfireFrameSize);
            var destination = new Rectangle(
                hearth.Position.X * TileSize,
                hearth.Position.Y * TileSize,
                hearth.Width * TileSize,
                hearth.Height * TileSize);
            _spriteBatch.Draw(_bonfireTexture, destination, source, Color.White);

            var flameSource = new Rectangle(
                frame * BonfireFlameFrameWidth,
                0,
                BonfireFlameFrameWidth,
                BonfireFlameFrameHeight);
            int footprintWidth = hearth.Width * TileSize;
            int flameWidth = footprintWidth * 3 / 4;
            int flameHeight = flameWidth * BonfireFlameFrameHeight / BonfireFlameFrameWidth;
            var flameDestination = new Rectangle(
                destination.X + (footprintWidth - flameWidth) / 2,
                destination.Y - TileSize / 2,
                flameWidth,
                flameHeight);
            _spriteBatch.Draw(_bonfireFlameTexture, flameDestination, flameSource, Color.White);
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
            while (i < _depthSorted.Length && _depthSorted[i].BaseY <= playerFootY)
            {
                _depthSorted[i].Draw(_spriteBatch);
                i++;
            }

            DrawPlayer();

            for (; i < _depthSorted.Length; i++)
            {
                _depthSorted[i].Draw(_spriteBatch);
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
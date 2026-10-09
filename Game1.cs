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
        private Texture2D _villageGroundTexture = null!;
        private Texture2D _villageObjectsTexture = null!;
        private PathCorners _pathCorners = null!;
        private Texture2D _vegetationTexture = null!;
        private Texture2D _shadowsTexture = null!;
        private Texture2D[] _treeTextures = Array.Empty<Texture2D>();
        private Texture2D _rocksTexture = null!;
        private Texture2D _churchTexture = null!;
        private Texture2D _house1Texture = null!;
        private Texture2D _house2Texture = null!;
        private Texture2D _playerTexture = null!;
        private Texture2D _wizardTexture = null!;
        private Texture2D _pixelTexture = null!;
        private Texture2D _bonfireTexture = null!;
        private Texture2D _bonfireFlameTexture = null!;
        private Texture2D _lampTexture = null!;
        private Texture2D _forestTerrainTexture = null!;
        private Texture2D _dungeonTexture = null!;
        private Texture2D _dungeonVignetteTexture = null!;
        private readonly Dictionary<string, Texture2D> _forestSprites = new(StringComparer.Ordinal);
        private DrawTree[] _trees = Array.Empty<DrawTree>();
        private DrawRock[] _rocks = Array.Empty<DrawRock>();
        private readonly Dictionary<PropSheet, Texture2D> _propSheets = new();
        private Texture2D _wellTexture = null!;
        private DrawProp[] _props = Array.Empty<DrawProp>();
        // Trees, props, and structures together, back-to-front, so the player can walk
        // between them (see DrawTreesAndPlayer).
        private IDepthSorted[] _depthSorted = Array.Empty<IDepthSorted>();
        private bool _showHitboxes; // F3 debug overlay, see DrawHitboxes

        private TileMap _map = null!;
        private readonly Dictionary<WorldMapType, TileMap> _maps = new();
        private Player _player = null!;
        private Camera2D _camera = null!;
        private MapTravel? _pendingTravel;
        private KeyboardState _previousKeyboard;

        // Combat. Enemies are kept per map so each map's mobs stay where the
        // player left them. Attack with Space or J.
        private readonly Dictionary<WorldMapType, List<Enemy>> _enemies = new();
        private Texture2D _enemyTexture = null!;
        private const int SwingDamage = 5;
        private const int EnemyMaxHealth = 20;
        private const int EnemyCellSize = 32;       // Idle-Sheet.png: 4 frames of 32x32 in one row
        private const int EnemyFrameCount = 4;
        private const float EnemyFramesPerSecond = 6f;
        private const int EnemySpriteHeight = 28;   // head-to-feet in source pixels, scaled to one tile
        private const int MaxTrainingEnemiesPerMap = 4;
        private static readonly Point[] TrainingEnemyOffsets =
        {
            new(3, 0), new(4, 2), new(5, -2), new(-4, 1), new(-3, -3), new(6, 1), new(2, 4), new(-5, -1),
        };

        // player.png is a grid of 48x48 cells, 6 frames per row. Rows 0-5 are
        // standing and walking; rows 6-8 contain the four-frame swing for
        // down, side, and up. The side row faces right and is flipped for left.
        private const int PlayerCellSize = 48;
        private const int PlayerFrameCount = 6;
        private const int PlayerSwingFrameCount = 4;
        private const float WalkFramesPerSecond = 12f;
        private const int PlayerSpriteHeight = 21; // head-to-feet in source pixels, scaled to one tile
        private const int BonfireFrameSize = 32;
        private const int BonfireFrameCount = 4;
        private const float BonfireFramesPerSecond = 8f;
        private const int BonfireFlameFrameWidth = 32;
        private const int BonfireFlameFrameHeight = 48;
        private const int LampHeightTiles = 4;
        private static readonly Rectangle LampSource = new(45, 17, 40, 94);

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
            _maps.Add(WorldMapType.Village, new TileMap(width: 48, height: 36, tileSize: TileSize, WorldMapType.Village));
            _maps.Add(WorldMapType.Forest, new TileMap(width: 36, height: 29, tileSize: TileSize, WorldMapType.Forest));
            _maps.Add(WorldMapType.Dungeon, new TileMap(width: 36, height: 28, tileSize: TileSize, WorldMapType.Dungeon));
            _map = _maps[WorldMapType.Village];
            _player = new Player(_map.SpawnPoint, TileSize, tilesPerSecond: 8f);
            _player.OnTileEntered += OnPlayerEnteredTile;
            _player.OnSwingImpact += OnPlayerSwingImpact;
            _enemies[WorldMapType.Village] = new List<Enemy>();
            _enemies[WorldMapType.Forest] = new List<Enemy>();
            _enemies[WorldMapType.Dungeon] = new List<Enemy>();

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

            _treeTextures = new[]
            {
                LoadTexture("Trees/Tree1.png"),
                LoadTexture("Trees/Tree 2.png"),
                LoadTexture("Trees/Tree 3.png"),
                LoadTexture("Trees/Tree 4.png"),
                LoadTexture("Trees/Tree 5.png"),
                LoadTexture("Trees/Tree 6.png"),
                LoadTexture("Trees/Tree 7.png"),
                LoadTexture("Trees/Tree 8.png"),
            };
            _villageGroundTexture = LoadTexture("village_ground.png");
            _villageObjectsTexture = LoadTexture("village_objects.png");
            _floorTilesTexture = LoadTexture("Ground/Tiles/Floors_Tiles.png");
            _pathCorners = new PathCorners(GraphicsDevice, _floorTilesTexture);
            _vegetationTexture = LoadTexture("Vegetation/Vegetation.png");
            _shadowsTexture = LoadTexture("Shadows/Shadows.png");
            _rocksTexture = LoadTexture("Rocks/Rocks.png");
            _churchTexture = LoadTexture("Buildings/CHURCH.png");
            _house1Texture = LoadTexture("Buildings/HOUSE 1.png");
            _house2Texture = LoadTexture("Buildings/HOUSE 2.png");
            _playerTexture = LoadTexture("Characters/player.png");
            _wizardTexture = LoadTexture("npc_sprite.png");
            _enemyTexture = LoadTexture("Entities/Mobs/Orc Crew/Orc/Idle/Idle-Sheet.png");
            _wellTexture = LoadTexture("Traversal/PIT - DAY.png");
            _bonfireTexture = LoadTexture("Environment/Structures/Stations/Bonfire/Bonfire_01-Sheet.png");
            _bonfireFlameTexture = LoadTexture("Environment/Structures/Stations/Bonfire/Fire_01-Sheet.png");
            _lampTexture = LoadTexture("Icons/Lamp.png");
            _forestTerrainTexture = LoadTexture("Environment/Tilesets/Wall_Tiles.png");
            _dungeonTexture = LoadTexture("dungeon.png");
            foreach (string file in ForestLayout.Sprites.Select(sprite => sprite.File).Distinct(StringComparer.Ordinal))
            {
                _forestSprites.Add(file, LoadTexture(file));
            }
            ValidateVillageLayer(_villageGroundTexture, "village_ground.png");
            ValidateVillageLayer(_villageObjectsTexture, "village_objects.png");
            ValidateDungeonBackground();

            _propSheets[PropSheet.Rocks] = _rocksTexture;
            _propSheets[PropSheet.Vegetation] = _vegetationTexture;
            foreach (PropSheet sheet in new[] { PropSheet.Tools, PropSheet.Furniture, PropSheet.Farm })
            {
                _propSheets[sheet] = LoadTexture(PropCatalog.FileFor(sheet));
            }
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
            CreateDungeonVignette();
            PrepareMapVisuals();

            // After the forest trunk tiles are set, so enemies never spawn on one.
            SpawnTrainingEnemies();
        }

        // Direct-load, no Content Pipeline: reads PNG bytes straight from the
        // output directory (copied there at build time per the .csproj) via
        // TitleContainer, which works the same on Windows/macOS/Linux.
        private Texture2D LoadTexture(string fileName)
        {
            using Stream stream = TitleContainer.OpenStream($"Content/{fileName}");
            return Texture2D.FromStream(GraphicsDevice, stream);
        }

        private void ValidateVillageLayer(Texture2D texture, string fileName)
        {
            TileMap village = _maps[WorldMapType.Village];
            if (texture.Width != village.Width * 16 || texture.Height != village.Height * 16)
            {
                throw new InvalidDataException(
                    $"Village layer '{fileName}' is {texture.Width}x{texture.Height}px; " +
                    $"expected {village.Width * 16}x{village.Height * 16}px.");
            }
        }

        private void ValidateDungeonBackground()
        {
            TileMap dungeon = _maps[WorldMapType.Dungeon];
            if (_dungeonTexture.Width != dungeon.Width * 16 ||
                _dungeonTexture.Height != dungeon.Height * 16)
            {
                throw new InvalidDataException(
                    $"Dungeon background is {_dungeonTexture.Width}x{_dungeonTexture.Height}px; " +
                    $"expected {dungeon.Width * 16}x{dungeon.Height * 16}px.");
            }
        }

        private void CreateDungeonVignette()
        {
            Viewport viewport = GraphicsDevice.Viewport;
            int width = viewport.Width;
            int height = viewport.Height;
            var pixels = new Color[width * height];
            float maxDistance = MathF.Sqrt(2f);

            for (int y = 0; y < height; y++)
            {
                float normalizedY = 2f * y / (height - 1) - 1f;
                for (int x = 0; x < width; x++)
                {
                    float normalizedX = 2f * x / (width - 1) - 1f;
                    float distance = MathF.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY) / maxDistance;
                    float fade = Math.Clamp((distance - 0.2f) / 0.8f, 0f, 1f);
                    fade = fade * fade * (3f - 2f * fade);
                    byte alpha = (byte)(70f + 160f * fade);
                    pixels[y * width + x] = new Color(0, 0, 0, (int)alpha);
                }
            }

            _dungeonVignetteTexture = new Texture2D(GraphicsDevice, width, height);
            _dungeonVignetteTexture.SetData(pixels);
        }

        private void PrepareMapVisuals()
        {
            _trees = DrawTree.CreateForest(_map, _treeTextures, TileSize);
            _rocks = DrawRock.CreateFor(_map, _rocksTexture, TileSize);
            _props = DrawProp.CreateFor(_map, _propSheets, TileSize);
            IEnumerable<IDepthSorted> depthSorted = _trees.Cast<IDepthSorted>().Concat(_props);
            if (_map.MapType == WorldMapType.Village)
            {
                depthSorted = depthSorted.Concat(
                    _map.VillageStructures.Select(structure => new DepthSortedStructure(this, structure, TileSize)))
                    .Concat(_map.AllTiles()
                        .Where(entry => entry.Value == TileType.VillageLantern)
                        .Select(entry => new DepthSortedLamp(_lampTexture, entry.Key, TileSize)));
            }
            // On the same depth row, draw structures first so neighboring props
            // and trees remain visible over their roof edges.
            _depthSorted = depthSorted
                .OrderBy(item => item.BaseY)
                .ThenBy(item => item is DepthSortedStructure ? 0 : 1)
                .ToArray();
        }

        // Places a few stationary targets near each map's spawn point so the
        // swing has something to hit.
        private void SpawnTrainingEnemies()
        {
            foreach (var (mapType, map) in _maps)
            {
                List<Enemy> enemies = _enemies[mapType];
                foreach (Point offset in TrainingEnemyOffsets)
                {
                    if (enemies.Count >= MaxTrainingEnemiesPerMap)
                    {
                        break;
                    }

                    var tile = new Point(map.SpawnPoint.X + offset.X, map.SpawnPoint.Y + offset.Y);
                    if (map.IsWalkable(tile))
                    {
                        enemies.Add(new Enemy(tile, TileSize, EnemyMaxHealth));
                    }
                }
            }
        }

        private bool IsEnemyAt(Point tile)
        {
            return _enemies[_map.MapType].Any(enemy => enemy.IsAlive && enemy.GridPosition == tile);
        }

        // Called once per swing, when the blow lands. Every living enemy standing
        // in one of the swing tiles takes damage and is shoved one tile away
        // from the player, if that tile is free.
        private void OnPlayerSwingImpact(IReadOnlyList<Point> swingTiles)
        {
            Point push = _player.FacingVector;
            foreach (Enemy enemy in _enemies[_map.MapType])
            {
                if (!enemy.IsAlive || !swingTiles.Contains(enemy.GridPosition))
                {
                    continue;
                }

                enemy.TakeDamage(SwingDamage);
                if (!enemy.IsAlive)
                {
                    continue;
                }

                var destination = new Point(enemy.GridPosition.X + push.X, enemy.GridPosition.Y + push.Y);
                if (_map.IsWalkable(destination) && destination != _player.GridPosition && !IsEnemyAt(destination))
                {
                    enemy.KnockBackTo(destination);
                }
            }
        }

        private void UpdateEnemies(GameTime gameTime)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            List<Enemy> enemies = _enemies[_map.MapType];
            foreach (Enemy enemy in enemies)
            {
                enemy.Update(deltaSeconds);
            }

            enemies.RemoveAll(enemy => enemy.ShouldRemove);
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
            const int boxHeight = 78;
            int x = (_graphics.PreferredBackBufferWidth - boxWidth) / 2;
            int y = _graphics.PreferredBackBufferHeight - boxHeight - 18;

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, boxWidth, boxHeight), new Color(18, 24, 32, 235));
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, boxWidth, 2), Color.Gold);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y + boxHeight - 2, boxWidth, 2), Color.Gold);

            string destination = travel.Destination switch
            {
                WorldMapType.Village => "VILLAGE",
                WorldMapType.Forest => "FOREST",
                WorldMapType.Dungeon => "DUNGEON",
                _ => throw new ArgumentOutOfRangeException(nameof(travel), travel.Destination, "Unknown travel destination."),
            };
            BitmapFont.Draw(_spriteBatch, _pixelTexture, $"ENTER THE {destination}?", new Point(x + 18, y + 12), 2, Color.White);
            BitmapFont.Draw(_spriteBatch, _pixelTexture, "Y=YES N=NO", new Point(x + 18, y + 48), 2, Color.White);
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
                bool attackPressed = WasPressed(keyboard, Keys.Space) || WasPressed(keyboard, Keys.J);
                if (_map.MapType == WorldMapType.Village &&
                    WasPressed(keyboard, Keys.E) &&
                    IsPlayerBesideWizard())
                {
                    _pendingTravel = new MapTravel(
                        WorldMapType.Forest,
                        _maps[WorldMapType.Forest].SpawnPoint);
                }

                _player.Update(gameTime, _map, keyboard, attackPressed, IsEnemyAt);
                UpdateEnemies(gameTime);
            }

            _camera.Update(gameTime, _player.PixelPosition, followSpeed: 8f);
            _previousKeyboard = keyboard;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(transformMatrix: _camera.GetTransformationMatrix(), samplerState: SamplerState.PointClamp);

            if (_map.MapType == WorldMapType.Village)
            {
                DrawVillageGround();
                DrawVillageObjectsBehindPlayer();
                bool wizardBehindPlayer =
                    (VillageCollisionLayout.WizardPosition.Y + 1) * TileSize <=
                    _player.PixelPosition.Y + TileSize;
                if (wizardBehindPlayer)
                {
                    DrawWizard();
                }
                DrawPlayer();
                if (!wizardBehindPlayer)
                {
                    DrawWizard();
                }
                DrawVillageObjectsInFrontOfPlayer();
            }
            else if (_map.MapType == WorldMapType.Dungeon)
            {
                DrawDungeon();
                DrawTreesAndPlayer(gameTime);
            }
            else
            {
                if (_map.MapType == WorldMapType.Forest && !ForestLayout.UseGeneratedVisuals)
                {
                    DrawForestSpriteLayer(ForestLayout.SpriteLayer.Ground);
                    DrawForestExitMarkers();
                    DrawForestSpriteLayer(ForestLayout.SpriteLayer.BehindPlayer);
                }
                else
                {
                    DrawTiles();
                }
                DrawGroundDetails();
                DrawVillagePlants();
                DrawTreeShadows();
                DrawRocks();
                DrawTreesAndPlayer(gameTime);
                if (_map.MapType == WorldMapType.Forest && !ForestLayout.UseGeneratedVisuals)
                {
                    DrawForestSpriteLayer(ForestLayout.SpriteLayer.OverPlayer);
                }
            }

            if (_showHitboxes)
            {
                DrawHitboxes();
            }

            _spriteBatch.End();

            if (_map.MapType == WorldMapType.Dungeon)
            {
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                _spriteBatch.Draw(
                    _dungeonVignetteTexture,
                    GraphicsDevice.Viewport.Bounds,
                    Color.White);
                _spriteBatch.End();
            }

            if (_pendingTravel is MapTravel travel)
            {
                DrawTravelPrompt(travel);
            }

            base.Draw(gameTime);
        }

        private void DrawForestExitMarkers()
        {
            foreach (var (position, tile) in _map.AllTiles())
            {
                if (tile == TileType.ForestExit)
                {
                    DrawTravelMarker(position, Color.LimeGreen);
                }
            }
        }

        private void DrawDungeon()
        {
            _spriteBatch.Draw(
                _dungeonTexture,
                new Rectangle(0, 0, _map.Width * TileSize, _map.Height * TileSize),
                Color.White);
        }

        private void DrawForestSpriteLayer(ForestLayout.SpriteLayer layer)
        {
            int scale = TileSize / TileSprites.GridSize;
            foreach (ForestLayout.ForestSprite sprite in ForestLayout.Sprites)
            {
                if (sprite.Layer != layer)
                {
                    continue;
                }

                Texture2D texture = _forestSprites[sprite.File];
                var destination = new Rectangle(
                    sprite.TileX * TileSize,
                    sprite.TileY * TileSize,
                    texture.Width * scale,
                    texture.Height * scale);
                _spriteBatch.Draw(
                    texture,
                    destination,
                    sourceRectangle: null,
                    Color.White,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    effects: sprite.Flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    layerDepth: 0f);
            }
        }

        private bool IsPlayerBesideWizard()
        {
            Point playerPosition = _player.GridPosition;
            Point wizardPosition = VillageCollisionLayout.WizardPosition;
            return Math.Abs(playerPosition.X - wizardPosition.X) +
                   Math.Abs(playerPosition.Y - wizardPosition.Y) <= 1;
        }

        private void DrawWizard()
        {
            Point position = VillageCollisionLayout.WizardPosition;
            var destination = new Rectangle(
                position.X * TileSize,
                (position.Y + 1) * TileSize - _wizardTexture.Height,
                _wizardTexture.Width,
                _wizardTexture.Height);
            _spriteBatch.Draw(_wizardTexture, destination, Color.White);
        }

        private void DrawVillageGround()
        {
            _spriteBatch.Draw(
                _villageGroundTexture,
                new Rectangle(0, 0, _map.Width * TileSize, _map.Height * TileSize),
                Color.White);
        }

        private int GetVillageObjectSplitY()
        {
            float scale = TileSize / 16f;
            return Math.Clamp(
                (int)MathF.Floor((_player.PixelPosition.Y + TileSize) / scale),
                0,
                _villageObjectsTexture.Height);
        }

        private void DrawVillageObjectsBehindPlayer()
        {
            int splitY = GetVillageObjectSplitY();
            if (splitY == 0)
            {
                return;
            }

            var source = new Rectangle(0, 0, _villageObjectsTexture.Width, splitY);
            var destination = new Rectangle(0, 0, _map.Width * TileSize, splitY * 2);
            _spriteBatch.Draw(_villageObjectsTexture, destination, source, Color.White);
        }

        private void DrawVillageObjectsInFrontOfPlayer()
        {
            int splitY = GetVillageObjectSplitY();
            if (splitY == _villageObjectsTexture.Height)
            {
                return;
            }

            var source = new Rectangle(
                0,
                splitY,
                _villageObjectsTexture.Width,
                _villageObjectsTexture.Height - splitY);
            var destination = new Rectangle(
                0,
                splitY * 2,
                _map.Width * TileSize,
                (_villageObjectsTexture.Height - splitY) * 2);
            _spriteBatch.Draw(_villageObjectsTexture, destination, source, Color.White);
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
                    TileType.DirtPath => _map.MapType == WorldMapType.Forest
                        ? TileSprites.DirtPatchFill
                        : TileSprites.BrightCrackedRoad,
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
                    DrawTravelMarker(gridPos, markerColor);
                }
            }

            // Rounded road corners go on top of the finished floor.
            _pathCorners.Draw(_spriteBatch, _map, TileSize);
            DrawForestTerrain();
        }

        private void DrawTravelMarker(Point position, Color color)
        {
            int x = position.X * TileSize;
            int y = position.Y * TileSize;
            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(x + 5, y + 5, TileSize - 10, TileSize - 10),
                color);
            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(x + 10, y + 10, TileSize - 20, TileSize - 20),
                Color.Black);
        }

        private void DrawForestTerrain()
        {
            if (_map.MapType != WorldMapType.Forest)
            {
                return;
            }

            foreach (var (position, tile) in _map.AllTiles())
            {
                if (tile == TileType.ForestHill)
                {
                    bool top = _map.GetTile(new Point(position.X, position.Y - 1)) != TileType.ForestHill;
                    bool bottom = _map.GetTile(new Point(position.X, position.Y + 1)) != TileType.ForestHill;
                    bool left = _map.GetTile(new Point(position.X - 1, position.Y)) != TileType.ForestHill;
                    bool right = _map.GetTile(new Point(position.X + 1, position.Y)) != TileType.ForestHill;
                    int sourceRow = top ? 0 : bottom ? 5 :
                        _map.GetTile(new Point(position.X, position.Y - 1)) != TileType.ForestHill ? 1 :
                        2 + Math.Abs(position.X * 31 + position.Y * 17) % 3;
                    int sourceColumn = left ? 0 : right ? 5 :
                        1 + Math.Abs(position.X * 13 + position.Y * 7) % 4;
                    var source = new Rectangle(
                        sourceColumn * TileSprites.GridSize,
                        sourceRow * TileSprites.GridSize,
                        TileSprites.GridSize,
                        TileSprites.GridSize);
                    var destination = new Rectangle(
                        position.X * TileSize, position.Y * TileSize, TileSize, TileSize);
                    _spriteBatch.Draw(_forestTerrainTexture, destination, source, Color.White);
                }
                else if (tile == TileType.ForestCave &&
                         _map.GetTile(new Point(position.X - 1, position.Y)) != TileType.ForestCave &&
                         _map.GetTile(new Point(position.X, position.Y - 1)) != TileType.ForestCave)
                {
                    var destination = new Rectangle(
                        position.X * TileSize, position.Y * TileSize, TileSize * 6, TileSize * 6);
                    _spriteBatch.Draw(
                        _forestTerrainTexture,
                        destination,
                        new Rectangle(0, 304, 96, 96),
                        Color.White);
                }
            }

            foreach (var (position, tile) in _map.AllTiles())
            {
                if (tile != TileType.ForestHill ||
                    _map.GetTile(new Point(position.X, position.Y + 1)) == TileType.ForestHill)
                {
                    continue;
                }

                bool left = _map.GetTile(new Point(position.X - 1, position.Y)) != TileType.ForestHill;
                bool right = _map.GetTile(new Point(position.X + 1, position.Y)) != TileType.ForestHill;
                int column = left ? 0 : right ? 5 : 1 + Math.Abs(position.X * 13 + position.Y * 7) % 4;
                int x = position.X * TileSize;
                int y = position.Y * TileSize;
                _spriteBatch.Draw(
                    _forestTerrainTexture,
                    new Rectangle(x, y, TileSize, TileSize * 2),
                    new Rectangle(column * TileSprites.GridSize, 96, TileSprites.GridSize, TileSprites.GridSize * 2),
                    Color.White);
                _spriteBatch.Draw(
                    _forestTerrainTexture,
                    new Rectangle(x, y + TileSize, TileSize, TileSize * 2),
                    new Rectangle(column * TileSprites.GridSize, 128, TileSprites.GridSize, TileSprites.GridSize * 2),
                    Color.White);
            }
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
        private void DrawGroundDetails()
        {
            int scale = TileSize / TileSprites.GridSize;
            foreach (var (position, tile) in _map.AllTiles())
            {
                int hash = unchecked(position.X * 19349663 ^ position.Y * 73856093) & 0x7fffffff;

                if (_map.MapType == WorldMapType.Village && tile == TileType.Grass)
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

            // The tiles the player's swing would cover from where they stand.
            foreach (Point swingTile in _player.GetSwingTiles())
            {
                DrawOutlinedBox(
                    new Rectangle(swingTile.X * TileSize, swingTile.Y * TileSize, TileSize, TileSize),
                    Color.Cyan);
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
            foreach (var (position, tile) in _map.AllTiles())
            {
                if (tile != TileType.VillageFlower && tile != TileType.VillageGarden &&
                    tile != TileType.ForestFlower && tile != TileType.ForestFoxglove &&
                    tile != TileType.ForestMushroom)
                {
                    continue;
                }

                int hash = unchecked(position.X * 19349663 ^ position.Y * 73856093);
                Rectangle source = tile switch
                {
                    TileType.ForestFoxglove =>
                        TileSprites.ForestFoxgloves[(hash & 0x7fffffff) % TileSprites.ForestFoxgloves.Length],
                    TileType.ForestMushroom =>
                        TileSprites.ForestMushrooms[(hash & 0x7fffffff) % TileSprites.ForestMushrooms.Length],
                    _ => TileSprites.Vegetation[(hash & 0x7fffffff) % TileSprites.Vegetation.Length],
                };
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

        private void DrawVillageStructure(VillageStructure structure, SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (structure.Type == TileType.VillageHearth)
            {
                DrawVillageHearth(structure, spriteBatch, gameTime);
                return;
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
                return;
            }

            int footprintWidth = structure.Width * TileSize;
            int width = (int)Math.Round(texture.Width * VillageStructure.RenderScale);
            int height = (int)Math.Round(texture.Height * VillageStructure.RenderScale);
            int x = structure.Position.X * TileSize + (footprintWidth - width) / 2;
            int y = (structure.Position.Y + structure.Height) * TileSize - height;
            var destination = new Rectangle(x, y, width, height);
            spriteBatch.Draw(texture, destination, Color.White);
        }

        private void DrawVillageHearth(VillageStructure hearth, SpriteBatch spriteBatch, GameTime gameTime)
        {
            int frame = (int)(gameTime.TotalGameTime.TotalSeconds * BonfireFramesPerSecond) % BonfireFrameCount;
            var source = new Rectangle(frame * BonfireFrameSize, 0, BonfireFrameSize, BonfireFrameSize);
            var destination = new Rectangle(
                hearth.Position.X * TileSize,
                hearth.Position.Y * TileSize,
                hearth.Width * TileSize,
                hearth.Height * TileSize);
            spriteBatch.Draw(_bonfireTexture, destination, source, Color.White);

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
            spriteBatch.Draw(_bonfireFlameTexture, flameDestination, flameSource, Color.White);
        }

        // Depth sorting: trees are sorted top-to-bottom, so draw the ones whose
        // base is at or above the player's feet first, then the player, then
        // the trees lower on the screen. That way the player walks in front of
        // a tree when south of it and behind it (hidden by the canopy) when
        // north of it.
        private void DrawTreesAndPlayer(GameTime gameTime)
        {
            int playerFootY = (int)_player.PixelPosition.Y + TileSize;

            int i = 0;
            while (i < _depthSorted.Length && _depthSorted[i].BaseY <= playerFootY)
            {
                _depthSorted[i].Draw(_spriteBatch, gameTime);
                i++;
            }

            DrawPlayer();

            for (; i < _depthSorted.Length; i++)
            {
                _depthSorted[i].Draw(_spriteBatch, gameTime);
            }
        }

        private sealed class DepthSortedStructure : IDepthSorted
        {
            private readonly Game1 _game;
            private readonly VillageStructure _structure;

            public DepthSortedStructure(Game1 game, VillageStructure structure, int tileSize)
            {
                _game = game;
                _structure = structure;
                Rectangle[] collisionBounds = structure.GetCollisionBounds(tileSize);
                BaseY = collisionBounds.Length == 0
                    ? (structure.Position.Y + structure.Height) * tileSize
                    : structure.Type == TileType.House2
                        ? collisionBounds.Min(bounds => bounds.Bottom)
                        : collisionBounds.Max(bounds => bounds.Bottom);
            }

            public int BaseY { get; }

            public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
            {
                _game.DrawVillageStructure(_structure, spriteBatch, gameTime);
            }
        }

        private sealed class DepthSortedLamp : IDepthSorted
        {
            private readonly Texture2D _texture;
            private readonly Point _tile;
            private readonly int _tileSize;

            public DepthSortedLamp(Texture2D texture, Point tile, int tileSize)
            {
                _texture = texture;
                _tile = tile;
                _tileSize = tileSize;
                BaseY = (_tile.Y + 1) * _tileSize;
            }

            public int BaseY { get; }

            public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
            {
                int height = LampHeightTiles * _tileSize;
                int width = (int)Math.Round(height * LampSource.Width / (float)LampSource.Height);
                int centerX = _tile.X * _tileSize + _tileSize / 2;
                var destination = new Rectangle(centerX - width / 2, BaseY - height, width, height);
                spriteBatch.Draw(_texture, destination, LampSource, Color.White);
            }
        }

        // Draws the player together with the enemies and the swing effect, so
        // enemies north of the player's feet go behind the player and the rest
        // in front. (Enemies are not depth sorted against trees/structures.)
        private void DrawPlayer()
        {
            float playerFootY = _player.PixelPosition.Y + TileSize;
            List<Enemy> enemies = _enemies[_map.MapType];

            foreach (Enemy enemy in enemies.Where(e => e.PixelPosition.Y + TileSize <= playerFootY)
                                           .OrderBy(e => e.PixelPosition.Y))
            {
                DrawEnemy(enemy);
            }

            DrawPlayerSprite();

            foreach (Enemy enemy in enemies.Where(e => e.PixelPosition.Y + TileSize > playerFootY)
                                           .OrderBy(e => e.PixelPosition.Y))
            {
                DrawEnemy(enemy);
            }

            if (_player.IsAttacking)
            {
                DrawSwingEffect();
            }
        }

        private void DrawEnemy(Enemy enemy)
        {
            int frame = (int)(enemy.AnimationTime * EnemyFramesPerSecond) % EnemyFrameCount;
            var source = new Rectangle(frame * EnemyCellSize, 0, EnemyCellSize, EnemyCellSize);

            float scale = TileSize / (float)EnemySpriteHeight;
            int size = (int)Math.Round(EnemyCellSize * scale);
            int x = (int)enemy.PixelPosition.X + (TileSize - size) / 2;
            int y = (int)enemy.PixelPosition.Y + TileSize - size;

            Color tint = enemy.HitFlashRemaining > 0f ? new Color(255, 110, 110) : Color.White;
            tint *= enemy.DeathFade;
            _spriteBatch.Draw(_enemyTexture, new Rectangle(x, y, size, size), source, tint);

            if (enemy.IsAlive && enemy.Health < enemy.MaxHealth)
            {
                int barWidth = TileSize - 8;
                int barX = (int)enemy.PixelPosition.X + 4;
                int barY = (int)enemy.PixelPosition.Y - 4;
                int filled = (int)Math.Round(barWidth * (enemy.Health / (float)enemy.MaxHealth));
                _spriteBatch.Draw(_pixelTexture, new Rectangle(barX - 1, barY - 1, barWidth + 2, 5), Color.Black * 0.8f);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(barX, barY, barWidth, 3), new Color(120, 20, 20));
                _spriteBatch.Draw(_pixelTexture, new Rectangle(barX, barY, filled, 3), Color.LimeGreen);
            }
        }

        // A bright band sweeps across the swing tiles from one side to the other
        // as the swing progresses.
        private void DrawSwingEffect()
        {
            IReadOnlyList<Point> tiles = _player.GetSwingTiles();
            float sweepPosition = _player.AttackProgress * tiles.Count;
            Color swingColor = Color.Lerp(Color.White, Color.Gold, 0.4f);

            for (int i = 0; i < tiles.Count; i++)
            {
                float intensity = Math.Max(0f, 1f - Math.Abs(sweepPosition - (i + 0.5f)));
                if (intensity <= 0f)
                {
                    continue;
                }

                var box = new Rectangle(tiles[i].X * TileSize, tiles[i].Y * TileSize, TileSize, TileSize);
                _spriteBatch.Draw(_pixelTexture, box, swingColor * (0.55f * intensity));
            }
        }

        private void DrawPlayerSprite()
        {
            bool isAttacking = _player.IsAttacking;
            int row = _player.Facing switch
            {
                FacingDirection.Down => isAttacking ? 6 : 0,
                FacingDirection.Up => isAttacking ? 8 : 2,
                _ => isAttacking ? 7 : 1, // Left and Right share the side row
            };
            if (_player.IsWalking && !isAttacking)
            {
                row += 3;
            }

            int frame = isAttacking
                ? Math.Min(PlayerSwingFrameCount - 1, (int)(_player.AttackProgress * PlayerSwingFrameCount))
                : _player.IsWalking
                    ? (int)(_player.AnimationTime * WalkFramesPerSecond) % PlayerFrameCount
                    : 0;

            var source = isAttacking
                ? new Rectangle(frame * PlayerCellSize, row * PlayerCellSize, PlayerCellSize, PlayerCellSize)
                : new Rectangle(
                    frame * PlayerCellSize + PlayerCrop.X,
                    row * PlayerCellSize + PlayerCrop.Y,
                    PlayerCrop.Width,
                    PlayerCrop.Height);

            float scale = TileSize / (float)PlayerSpriteHeight;
            int size = (int)Math.Round(source.Width * scale);
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
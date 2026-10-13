using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        // CopyToOutputDirectory entry for Assets/**/*.png).
        private Texture2D _floorTilesTexture = null!;
        private Texture2D _villageGroundTexture = null!;
        private Texture2D _villageObjectsGroundTexture = null!;
        private Texture2D _villageObjectsMainTexture = null!;
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
        private Texture2D _traderTexture = null!;
        private Texture2D _pixelTexture = null!;
        private Texture2D _hearthGlowTexture = null!;
        private Texture2D _txPropsTexture = null!;
        private Texture2D _lampTexture = null!;
        private Texture2D _forestTerrainTexture = null!;
        private Texture2D _dungeonTexture = null!;
        private Texture2D _dungeonVignetteTexture = null!;
        private Texture2D _healthBarTexture = null!;
        private Texture2D _energyBarTexture = null!;
        private Texture2D _inventoryIconsTexture = null!;
        private readonly Dictionary<string, Texture2D> _forestSprites = new(StringComparer.Ordinal);
        private DrawTree[] _trees = Array.Empty<DrawTree>();
        private DrawRock[] _rocks = Array.Empty<DrawRock>();
        private readonly Dictionary<PropSheet, Texture2D> _propSheets = new();
        private Texture2D _wellTexture = null!;
        private DrawProp[] _props = Array.Empty<DrawProp>();
        // Depth-aware scenery, sorted back-to-front so actors can pass behind it.
        private IDepthSorted[] _depthSorted = Array.Empty<IDepthSorted>();
        private bool _showHitboxes; // F3 debug overlay, see DrawHitboxes

        private TileMap _map = null!;
        private readonly Dictionary<WorldMapType, TileMap> _maps = new();
        private readonly Inventory _inventory = new();
        private readonly ActionHistory _history = new();   // LIFO undo stack for crafting (U key)
        private readonly QuestLog _questLog = new();
        private Player _player = null!;
        private Camera2D _camera = null!;
        private MapTravel? _pendingTravel;
        private KeyboardState _previousKeyboard;

        // Night defense. Day: explore, gather, craft. Night: light the village
        // hearth and defend it from a queued wave of goblins and skeletons
        // (FIFO WaveSpawner). Each enemy's strike damage comes from its spawn info.
        private const int MaxNights = 5;
        // Time from the start of one wave-enemy swing to the start of its next (tuning knob).
        private const float EnemyAttackCycleSeconds = 1.0f;
        private const float MarchSpeedScale = 2f;        // EnemySpawnInfo.Speed -> tiles per second
        private const float NightDimAlpha = 0.5f;
        private const float BannerSeconds = 3f;
        private GameState _gameState = GameState.Day;
        private GameScreen _screen = GameScreen.Title;   // the game opens on the title menu
        private GameScreen _previousMenuScreen = GameScreen.Title;
        private int _titleSelection;
        private string _menuMessage = string.Empty;

        // Ending screen (Victory / Defeat). The short input delay keeps a player
        // who is mashing Space to swing from dismissing the screen by accident.
        private const float EndingInputDelaySeconds = 1.0f;
        private EndingKind _endingKind;
        private string _endingReason = string.Empty;
        private int _endingSelection;
        private float _endingInputDelay;
        private int _dayCount = 1;
        private int _nightNumber;                        // 0 until the first night starts
        private int _kills;                              // wave enemies defeated (Milestone 4 score)
        private bool _allNightsSurvived;
        private readonly PlayerStats _stats = new();
        // Wave enemy HearthDamage x this = damage to the player per hit (tuning knob).
        // Hearth damage itself is unscaled.
        private const int PlayerDamageScale = 1;
        private bool _pendingNightStart;
        private string _bannerText = string.Empty;
        private float _bannerRemaining;
        private readonly WaveSpawner _spawner = new();
        private EnemySpawnInfo? _pendingWaveSpawn;
        private readonly Hearth _hearth = new(VillageCollisionLayout.HearthTile, VillageCollisionLayout.HearthSize);
        private HearthPathField? _hearthPaths;
        private Point[] _entryPoints = Array.Empty<Point>();

        // Combat. Enemies are kept per map so each map's mobs stay where the
        // player left them. Attack with Space or J.
        private readonly Dictionary<WorldMapType, List<Enemy>> _enemies = new();
        private readonly List<TreasureChest> _dungeonChests = new();
        private TreasureChest? _openChest;
        private int _chestSelection;
        private Texture2D _enemyTexture = null!;
        private Texture2D _enemyHandsTexture = null!;
        private Texture2D _enemyRunTexture = null!;
        private Texture2D _skeletonWeaponTexture = null!;

        // Skeleton sprite sets, indexed by (int)SkeletonVariant. They share the
        // orc's layout: Idle is 4 frames of 32x32, Run is 6 frames of 64x64.
        private static readonly string[] SkeletonFolders =
        {
            "Skeleton - Base", "Skeleton - Warrior", "Skeleton - Rogue", "Skeleton - Mage",
        };
        private readonly Texture2D[] _skeletonIdleTextures = new Texture2D[SkeletonFolders.Length];
        private readonly Texture2D[] _skeletonRunTextures = new Texture2D[SkeletonFolders.Length];
        private const int SwingDamage = 5;
        private bool _traderOpen;
        private int _traderSelection;
        private bool _questPanelOpen;
        private bool _bonusQuestInserted;
        private bool _hasTalkedToWizard;

        private enum InteractableKind
        {
            Trader,
            FoodVendor,
            Hearth,
            Wizard,
        }

        private readonly record struct InteractableCandidate(InteractableKind Kind, Point Position);

        // Food vendor (Peasant_A, same 64x64 4-frame layout as the trader).
        // She is not in the village yet: set FoodVendorTile to a walkable
        // village tile, and block that tile in CollisionMapLayouts.cs the way
        // the trader's is, to place her. While it is null she is not drawn
        // and cannot be talked to.
        private static readonly Point? FoodVendorTile = new Point(32, 29);
        private Texture2D _foodVendorTexture = null!;
        private bool _foodShopOpen;
        private int _foodSelection;
        private bool _inventoryOpen;
        private int _inventorySelection;
        private int _inventoryScroll;
        private string? _equippedWeaponName;
        // Tavern_A Idle sheet: 4 frames of 64x64 in one row, the woman's
        // feet on source row 48.
        private const int TraderCellSize = 64;
        private const int TraderFrameCount = 4;
        private const float TraderFramesPerSecond = 4f;
        private const int TraderFeetRow = 48;
        private const float TraderScale = 1.1f;
        private const int EnemyMaxHealth = 20;
        private const int EnemyCellSize = 32;       // Idle-Sheet.png: 4 frames of 32x32 in one row
        private const int EnemyFrameCount = 4;
        private const float EnemyFramesPerSecond = 6f;
        private const int EnemySpriteHeight = 28;   // head-to-feet in source pixels, scaled to one tile

        // Wave-enemy attack animation (draw-only; never moves GridPosition/PixelPosition).
        // Swing phases by Enemy.AttackProgress: wind-up 0 -> impact (Enemy.AttackImpactProgress),
        // lunge until EnemyLungeEndProgress, then recovery back to the resting offset.
        private const float EnemyWindupBackPixels = 3f;     // lean away from the target during wind-up
        private const float EnemyLungePixels = 8f;          // snap toward the target at impact
        private const int EnemyWindupHandLiftPixels = 6;    // goblin hands rise during wind-up
        private const float EnemyLungeEndProgress = 0.8f;
        private const float EnemySlashSeconds = 0.12f;      // how long the slash arc stays visible
        private static readonly Color EnemyWindupTint = new(255, 170, 170);

        // Orc/Run/Run-Sheet.png: 6 frames of 64x64 in one row, side view facing
        // right, feet on the bottom edge. Used while a goblin is marching; the
        // sheet is flipped when it walks left. Frames per tile walked keeps the
        // legs in step with the march speed, which grows from night to night.
        private const int EnemyRunCellSize = 64;
        private const int EnemyRunFrameCount = 6;
        private const float EnemyRunFramesPerTile = 4.8f;
        private static readonly Rectangle SkeletonWeaponSource = new(64, 0, 16, 48);
        private static readonly Point SkeletonIdleWeaponHand = new(23, 19);
        private static readonly Point SkeletonRunWeaponHand = new(48, 38);
        private static readonly Point SkeletonIdleBackHandCenter = new(9, 19);
        private static readonly Point SkeletonIdleFrontHandCenter = new(24, 19);
        private static readonly Point SkeletonRunBackHandCenter = new(18, 38);
        private static readonly Point SkeletonRunFrontHandCenter = new(48, 38);
        private const float SkeletonWeaponScale = 0.65f;

        // The goblin sprite has no arms, so its hands are separate sprites drawn
        // on top. Hands.png is 2 columns (left hand, right hand) of 16x16 cells;
        // rows 4 and 5 are the green goblin hands (try 5 for the other pose),
        // while row 2 is a compact closed fist used for skeletons.
        private const int HandCellSize = 16;
        private const int GoblinHandRow = 4;
        private const int SkeletonHandRow = 2;
        // Where each hand's centre sits in the 32x32 idle frame, in source pixels
        // (x to the right, y down). Nudge these two to move the hands.
        private static readonly Point GoblinBackHandCenter = new(7, 19);
        private static readonly Point GoblinFrontHandCenter = new(22, 19);
        // The upper body bobs between idle frames (the legs stay put), so the
        // hands move down with it. One entry per idle frame.
        private static readonly int[] GoblinBodyBobY = { 0, 2, 2, 1 };
        // The dungeon is packed with stationary skeleton targets. Placement is a
        // seeded shuffle of the floor tiles (same layout every run), keeping each
        // skeleton at least DungeonSkeletonSpacing tiles from the others and more
        // than DungeonSkeletonSafeRadius tiles from the arrival point.
        private const int DungeonSkeletonCount = 32;
        private const int DungeonSkeletonSpacing = 2;
        private const int DungeonSkeletonSafeRadius = 4;
        private const int DungeonSkeletonSeed = 7;

        // Forest and dungeon enemies notice the player within this many walking
        // tiles, then chase and attack with the same swing cycle as the night wave.
        private const int HostileAggroTiles = 7;
        private const float HostileMarchTilesPerSecond = 1.5f;   // skeleton variants scale this by SpeedScale

        // player.png is a grid of 48x48 cells, 6 frames per row. Rows 0-5 are
        // standing and walking; rows 6-8 contain the four-frame swing for
        // down, side, and up. The side row faces right and is flipped for left.
        private const int PlayerCellSize = 48;
        private const int PlayerFrameCount = 6;
        private const int PlayerSwingFrameCount = 4;
        private const float WalkFramesPerSecond = 12f;
        private const int PlayerSpriteHeight = 21; // head-to-feet in source pixels, scaled to one tile

        // The village hearth is two props from TX Props.png (16px art, drawn at
        // the village's 2x scale): a round stone platform lying flat on the
        // plaza, with the praying-woman statue standing on its centre slab. The
        // blocked 2x2 footprint is the statue's base; the platform is floor.
        private static readonly Rectangle HearthPlatformSource = new(353, 269, 94, 72);
        private static readonly Rectangle HearthStatueSource = new(445, 20, 37, 73);
        private const int HearthArtScale = 2;
        private const int HearthPlatformSlabBottom = 42;   // source px from the platform's top to where the statue's feet land
        private const int HearthBarWidth = TileSize * 3;
        private const int LampHeightTiles = 4;
        private static readonly Rectangle LampSource = new(45, 17, 40, 94);
        private static readonly Rectangle BarFillSource = new(4, 2, 3, 6);

        // Same 32x32 crop of every cell, so the character doesn't jitter
        // between frames. The bottom edge is where the feet/shadow sit.
        private static readonly Rectangle PlayerCrop = new(8, 12, 32, 32);

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 960,
                PreferredBackBufferHeight = 640
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _questLog.QuestCompleted += OnQuestCompleted;
        }

        protected override void Initialize()
        {
            BuildWorld();
            base.Initialize();
        }

        // Builds a fresh world: maps, player, per-map enemy lists, dungeon chests
        // and camera. Initialize calls it once at launch; ResetGame calls it again
        // for a replay (the caller then runs PrepareMapVisuals and
        // SpawnDungeonSkeletons, as LoadContent does at launch).
        private void BuildWorld(int? forestSeed = null)
        {
            _maps.Clear();
            _maps.Add(WorldMapType.Village, new TileMap(width: 48, height: 36, tileSize: TileSize, WorldMapType.Village));
            _maps.Add(WorldMapType.Forest, new TileMap(
                width: 36, height: 29, tileSize: TileSize, WorldMapType.Forest, forestSeed));
            _maps.Add(WorldMapType.Dungeon, new TileMap(width: 36, height: 28, tileSize: TileSize, WorldMapType.Dungeon));
            _map = _maps[WorldMapType.Village];
            _player = new Player(_map.SpawnPoint, TileSize, tilesPerSecond: 8f);
            _player.OnTileEntered += OnPlayerEnteredTile;
            _player.OnSwingImpact += OnPlayerSwingImpact;
            _enemies[WorldMapType.Village] = new List<Enemy>();
            _enemies[WorldMapType.Forest] = new List<Enemy>();
            _enemies[WorldMapType.Dungeon] = new List<Enemy>();
            // Left tile of each 2-wide chest in the dungeon's top room.
            _dungeonChests.Clear();
            foreach (Point chestTile in new[] { new Point(4, 3), new Point(7, 3), new Point(20, 3) })
            {
                _dungeonChests.Add(new TreasureChest(
                    chestTile,
                    new[]
                    {
                        new Item("Gold", 2),
                        new Item("Iron", 3),
                    }));
            }

            _camera = new Camera2D(
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight,
                _map.Width,
                _map.Height,
                TileSize);
            _camera.SnapTo(_player.PixelPosition);
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
            _villageObjectsGroundTexture = LoadTexture("village_objects_ground_decor.png");
            _villageObjectsMainTexture = LoadTexture("village_objects_main.png");
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
            _traderTexture = LoadTexture("Entities/Npc's/Citizen_F/Tavern_A/Idle/Idle_Side-Sheet.png");
            _foodVendorTexture = LoadTexture("Entities/Npc's/Citizen_F/Peasant_A/Idle/Idle-Sheet.png");
            _enemyTexture = LoadTexture("Entities/Mobs/Orc Crew/Orc/Idle/Idle-Sheet.png");
            _enemyHandsTexture = LoadTexture("Weapons/Hands/Hands.png");
            _enemyRunTexture = LoadTexture("Entities/Mobs/Orc Crew/Orc/Run/Run-Sheet.png");
            _skeletonWeaponTexture = LoadTexture("Weapons/bone/bone.png");
            for (int i = 0; i < SkeletonFolders.Length; i++)
            {
                string folder = $"Entities/Mobs/Skeleton Crew/{SkeletonFolders[i]}";
                _skeletonIdleTextures[i] = LoadTexture($"{folder}/Idle/Idle-Sheet.png");
                _skeletonRunTextures[i] = LoadTexture($"{folder}/Run/Run-Sheet.png");
            }
            _wellTexture = LoadTexture("Traversal/PIT - DAY.png");
            _txPropsTexture = LoadTexture("TX Props.png");
            _lampTexture = LoadTexture("Icons/Lamp.png");
            _forestTerrainTexture = LoadTexture("Environment/Tilesets/Wall_Tiles.png");
            _dungeonTexture = LoadTexture("dungeon.png");
            _healthBarTexture = LoadTexture("UI/bar_red.png");
            _energyBarTexture = LoadTexture("UI/bar_blue.png");
            _inventoryIconsTexture = LoadTexture("Free_Icons_all_16x16.png");
            foreach (string file in ForestLayout.Sprites.Select(sprite => sprite.File).Distinct(StringComparer.Ordinal))
            {
                _forestSprites.Add(file, LoadTexture(file));
            }
            ValidateVillageLayer(_villageGroundTexture, "village_ground.png");
            ValidateVillageLayer(_villageObjectsGroundTexture, "village_objects_ground_decor.png");
            ValidateVillageLayer(_villageObjectsMainTexture, "village_objects_main.png");
            ValidateDungeonBackground();

            _propSheets[PropSheet.Rocks] = _rocksTexture;
            _propSheets[PropSheet.Vegetation] = _vegetationTexture;
            foreach (PropSheet sheet in new[] { PropSheet.ObjectRocks, PropSheet.Tools, PropSheet.Furniture, PropSheet.Farm })
            {
                _propSheets[sheet] = LoadTexture(PropCatalog.FileFor(sheet));
            }
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
            CreateDungeonVignette();
            CreateHearthGlow();
            PrepareMapVisuals();

            // After the forest trunk tiles are set, so enemies never spawn on one.
            SpawnDungeonSkeletons();
        }

        // Direct-load, no Content Pipeline: reads PNG bytes straight from the
        // output directory (copied there at build time per the .csproj) via
        // TitleContainer, which works the same on Windows/macOS/Linux.
        private Texture2D LoadTexture(string fileName)
        {
            using Stream stream = TitleContainer.OpenStream($"Assets/{fileName}");
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
                    float fade = Math.Clamp((distance - 0.25f) / 0.75f, 0f, 1f);
                    fade = fade * fade * (3f - 2f * fade);
                    byte alpha = (byte)(35f + 120f * fade);
                    pixels[y * width + x] = new Color(0, 0, 0, (int)alpha);
                }
            }

            _dungeonVignetteTexture = new Texture2D(GraphicsDevice, width, height);
            _dungeonVignetteTexture.SetData(pixels);
        }

        // A soft white radial falloff (premultiplied, for additive blending).
        // Tinted warm and stretched over the platform at night so the hearth
        // reads as a light source in the dark village.
        private void CreateHearthGlow()
        {
            const int size = 128;
            var pixels = new Color[size * size];
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - radius) / radius;
                    float dy = (y + 0.5f - radius) / radius;
                    float falloff = Math.Max(0f, 1f - MathF.Sqrt(dx * dx + dy * dy));
                    int value = (int)Math.Round(MathF.Pow(falloff, 1.5f) * 255f);
                    pixels[y * size + x] = new Color(value, value, value, value);
                }
            }
 
            _hearthGlowTexture = new Texture2D(GraphicsDevice, size, size);
            _hearthGlowTexture.SetData(pixels);
        }

        private void PrepareMapVisuals()
        {
            _trees = DrawTree.CreateForest(_map, _treeTextures, TileSize);
            _rocks = DrawRock.CreateFor(_map, _rocksTexture, TileSize);
            _props = DrawProp.CreateFor(_map, _propSheets, TileSize);
            IEnumerable<IDepthSorted> depthSorted = _trees
                .Cast<IDepthSorted>()
                .Concat(_props)
                .Concat(_rocks);
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

        // Fills the dungeon with stationary skeleton targets (all four variants,
        // in rotation). Candidate tiles are every floor tile that is not a travel
        // tile, not near the arrival point, and not in front of a chest; they are
        // shuffled with a fixed seed and picked greedily with a minimum spacing,
        // so a corridor or doorway can never be walled off.
        private void SpawnDungeonSkeletons()
        {
            TileMap dungeon = _maps[WorldMapType.Dungeon];
            List<Enemy> enemies = _enemies[WorldMapType.Dungeon];
            var variants = (SkeletonVariant[])Enum.GetValues(typeof(SkeletonVariant));

            var candidates = new List<Point>();
            for (int y = 0; y < dungeon.Height; y++)
            {
                for (int x = 0; x < dungeon.Width; x++)
                {
                    var tile = new Point(x, y);
                    if (dungeon.IsWalkable(tile) &&
                        !dungeon.TryGetTravel(tile, out _) &&
                        ChebyshevDistance(tile, dungeon.SpawnPoint) > DungeonSkeletonSafeRadius &&
                        !_dungeonChests.Any(chest => chest.Tiles.Any(chestTile => ChebyshevDistance(tile, chestTile) <= 1)) &&
                        !enemies.Any(enemy => enemy.GridPosition == tile))
                    {
                        candidates.Add(tile);
                    }
                }
            }

            var random = new Random(DungeonSkeletonSeed);
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            int placed = 0;
            foreach (Point tile in candidates)
            {
                if (placed >= DungeonSkeletonCount)
                {
                    break;
                }

                if (enemies.Any(enemy => ChebyshevDistance(enemy.GridPosition, tile) < DungeonSkeletonSpacing))
                {
                    continue;
                }

                SkeletonVariant variant = variants[placed % variants.Length];
                int health = (int)Math.Round(EnemyMaxHealth * SkeletonStats.HealthScale(variant));
                enemies.Add(new Enemy(tile, TileSize, health)
                {
                    Kind = EnemyKind.Skeleton,
                    Variant = variant,
                    IsHostile = true,
                    HearthDamage = SkeletonStats.HearthDamage(variant),
                    MarchTilesPerSecond = HostileMarchTilesPerSecond * SkeletonStats.SpeedScale(variant),
                });
                placed++;
            }

            Console.WriteLine($"[Dungeon] Placed {placed} skeleton(s) (target {DungeonSkeletonCount}).");
        }

        private static int ChebyshevDistance(Point a, Point b)
        {
            return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
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
            bool mapChanged = false;
            foreach (Point swingTile in swingTiles)
            {
                if (_map.TryHarvestResource(swingTile, out var harvestedItem, out bool nodeDepleted))
                {
                    _inventory.Add(harvestedItem.Name, harvestedItem.Quantity);
                    _questLog.OnResourceGathered(harvestedItem.Name, harvestedItem.Quantity);
                    mapChanged |= nodeDepleted;
                }
            }

            if (mapChanged)
            {
                PrepareMapVisuals();
            }

            Point push = _player.FacingVector;
            List<Enemy> enemies = _enemies[_map.MapType];
            var hitEnemies = new bool[enemies.Count];
            foreach (Point swingTile in swingTiles)
            {
                int enemyIndex = LinearSearchUtil.FindIndex(
                    enemies,
                    candidate => candidate.IsAlive && candidate.GridPosition == swingTile);
                if (enemyIndex >= 0)
                {
                    hitEnemies[enemyIndex] = true;
                }

            }

            for (int index = 0; index < enemies.Count; index++)
            {
                if (!hitEnemies[index])
                {
                    continue;
                }
                Enemy enemy = enemies[index];
                enemy.TakeDamage(SwingDamage + WeaponBonus());
                if (!enemy.IsAlive)
                {
                    _questLog.OnEnemyKilled();
                    foreach (var drop in enemy.DropLoot())
                    {
                        _inventory.Add(drop.Name, drop.Quantity);
                        _questLog.OnResourceGathered(drop.Name, drop.Quantity);
                    }
                    if (enemy.IsWaveEnemy)
                    {
                        _kills++;
                    }
                    continue;
                }

                var destination = new Point(enemy.GridPosition.X + push.X, enemy.GridPosition.Y + push.Y);
                if (_map.IsWalkable(destination) && destination != _player.GridPosition && !IsEnemyAt(destination))
                {
                    enemy.KnockBackTo(destination);
                }
            }
        }

        private bool IsPlayerBesideHearth()
        {
            return _map.MapType == WorldMapType.Village &&
                   _hearth.IsAdjacentTo(_player.GridPosition, includeDiagonals: true);
        }

        private void ShowBanner(string text)
        {
            _bannerText = text;
            _bannerRemaining = BannerSeconds;
        }

        private void OnQuestCompleted(QuestNode quest)
        {
            ShowBanner($"QUEST COMPLETE: {quest.Title.ToUpperInvariant()}");
        }

        // Day -> Night: builds the BFS distance field from the hearth, keeps the
        // entry points that can actually reach it, and queues tonight's wave.
        private void StartNight()
        {
            _pendingNightStart = false;

            var paths = new HearthPathField(_hearth.Tiles, _map.IsWalkable, _map.Width, _map.Height);
            Point[] entries = VillageCollisionLayout.EnemyEntryPoints
                .Where(point => paths.IsReachable(point))
                .ToArray();
            if (entries.Length == 0)
            {
                Console.WriteLine("[Night] WARNING - no village entry point can reach the hearth; night not started.");
                return;
            }

            _hearthPaths = paths;
            _entryPoints = entries;
            _pendingWaveSpawn = null;
            _nightNumber++;
            _gameState = GameState.Night;
            _spawner.QueueWave(_nightNumber);
            ShowBanner($"NIGHT {_nightNumber} BEGINS");
            Console.WriteLine(
                $"[Night] Night {_nightNumber} begins: {_spawner.RemainingInWave} enemies queued, " +
                $"{entries.Length} entry point(s).");
        }

        private void UpdateNight(float deltaSeconds)
        {
            List<Enemy> enemies = _enemies[WorldMapType.Village];

            // Queue -> world: the spawner hands over the next goblin once its
            // delay has run out.
            _pendingWaveSpawn ??= _spawner.Update(deltaSeconds);
            if (_pendingWaveSpawn is EnemySpawnInfo spawn && TrySpawnWaveEnemy(spawn, enemies))
            {
                _pendingWaveSpawn = null;
            }

            foreach (Enemy enemy in enemies)
            {
                if (!enemy.IsWaveEnemy)
                {
                    continue;
                }

                // The impact moment of a swing: damage lands only if the target
                // is still in reach right now, otherwise the swing misses.
                if (enemy.ConsumeImpact())
                {
                    ResolveEnemyImpact(enemy);
                }

                if (!enemy.IsAlive || enemy.IsSliding || enemy.IsStunned)
                {
                    continue;
                }

                // A swinging enemy stands still and keeps facing its target.
                if (enemy.IsAttacking)
                {
                    FaceEnemyTarget(enemy, enemy.AttackTargetsPlayer);
                    continue;
                }

                // An enemy beside the player swings at the player first;
                // otherwise it swings at the hearth if adjacent, else keeps marching.
                if (!_stats.IsDead && IsAdjacentToPlayer(enemy.GridPosition))
                {
                    FaceEnemyTarget(enemy, targetsPlayer: true);
                    Vector2 toPlayer = new(
                        _player.GridPosition.X - enemy.GridPosition.X,
                        _player.GridPosition.Y - enemy.GridPosition.Y);
                    if (enemy.TryBeginAttack(EnemyAttackCycleSeconds, toPlayer, targetsPlayer: true))
                    {
                        Console.WriteLine($"[Night] {EnemyLogName(enemy)} swing begins (at player)");
                    }
                    continue;
                }

                if (_hearth.IsAdjacentTo(enemy.GridPosition))
                {
                    FaceEnemyTarget(enemy, targetsPlayer: false);
                    if (enemy.TryBeginAttack(EnemyAttackCycleSeconds, HearthDirectionFrom(enemy.GridPosition), targetsPlayer: false))
                    {
                        Console.WriteLine($"[Night] {EnemyLogName(enemy)} swing begins (at hearth)");
                    }
                    continue;
                }

                Point? next = _hearthPaths!.NextStep(enemy.GridPosition, tile => IsTileTakenForEnemy(tile, enemy));
                if (next is Point step)
                {
                    enemy.MarchTo(step);
                }
            }

            if (_hearth.IsDestroyed)
            {
                FailNight("THE HEARTH FELL");
                return;
            }

            if (_stats.IsDead)
            {
                FailNight("YOU FELL", playerFell: true);
                return;
            }

            if (_spawner.IsWaveComplete && _pendingWaveSpawn == null &&
                !enemies.Any(enemy => enemy.IsWaveEnemy && enemy.IsAlive))
            {
                EndNight();
            }
        }

        private static string EnemyLogName(Enemy enemy)
        {
            return enemy.Kind == EnemyKind.Skeleton ? "skeleton" : "goblin";
        }

        private void FaceEnemyTarget(Enemy enemy, bool targetsPlayer)
        {
            enemy.FaceToward(targetsPlayer
                ? _player.GridPosition.X
                : _hearth.Position.X + (_hearth.Size - 1) / 2f);
        }

        // Unit step from an enemy tile to the nearest hearth tile (the enemy is
        // orthogonally adjacent, so this is one of the four axis directions).
        private Vector2 HearthDirectionFrom(Point tile)
        {
            int nearestX = Math.Clamp(tile.X, _hearth.Position.X, _hearth.Position.X + _hearth.Size - 1);
            int nearestY = Math.Clamp(tile.Y, _hearth.Position.Y, _hearth.Position.Y + _hearth.Size - 1);
            return new Vector2(nearestX - tile.X, nearestY - tile.Y);
        }

        // Called once per swing at the impact moment. The player is hit only if
        // still alive and orthogonally adjacent; the hearth only if still adjacent.
        private void ResolveEnemyImpact(Enemy enemy)
        {
            string name = EnemyLogName(enemy);
            string tag = enemy.IsWaveEnemy ? "[Night]" : "[Hostile]";
            if (enemy.AttackTargetsPlayer)
            {
                if (!_stats.IsDead && IsAdjacentToPlayer(enemy.GridPosition))
                {
                    bool landed = _stats.TakeDamage(enemy.HearthDamage * PlayerDamageScale);
                    Console.WriteLine(landed
                        ? $"{tag} {name} impact hit (player, {enemy.HearthDamage * PlayerDamageScale} damage)"
                        : $"{tag} {name} impact hit (player) but the damage grace period absorbed it");
                }
                else
                {
                    Console.WriteLine($"{tag} {name} impact missed (player moved away)");
                }

                return;
            }

            if (_hearth.IsAdjacentTo(enemy.GridPosition))
            {
                _hearth.TakeDamage(enemy.HearthDamage);
                Console.WriteLine($"{tag} {name} impact hit (hearth, {enemy.HearthDamage} damage)");
            }
            else
            {
                Console.WriteLine($"{tag} {name} impact missed (hearth no longer adjacent)");
            }
        }

        // Forest and dungeon enemies: when the player is within HostileAggroTiles
        // walking tiles they step toward the player and swing when adjacent. Same
        // swing cycle as the night wave (impact only lands if the player is still
        // adjacent), so stepping away dodges it. Only the map the player is on updates.
        private void UpdateHostileEnemies()
        {
            List<Enemy> enemies = _enemies[_map.MapType];
            Dictionary<Point, int>? field = null;

            foreach (Enemy enemy in enemies)
            {
                if (!enemy.IsHostile)
                {
                    continue;
                }

                if (enemy.ConsumeImpact())
                {
                    ResolveEnemyImpact(enemy);
                }

                if (!enemy.IsAlive || enemy.IsSliding || enemy.IsStunned || _stats.IsDead)
                {
                    continue;
                }

                if (enemy.IsAttacking)
                {
                    enemy.FaceToward(_player.GridPosition.X);
                    continue;
                }

                if (IsAdjacentToPlayer(enemy.GridPosition))
                {
                    enemy.FaceToward(_player.GridPosition.X);
                    Vector2 toPlayer = new(
                        _player.GridPosition.X - enemy.GridPosition.X,
                        _player.GridPosition.Y - enemy.GridPosition.Y);
                    if (enemy.TryBeginAttack(EnemyAttackCycleSeconds, toPlayer, targetsPlayer: true))
                    {
                        Console.WriteLine($"[Hostile] {EnemyLogName(enemy)} swing begins (at player)");
                    }

                    continue;
                }

                if (ChebyshevDistance(enemy.GridPosition, _player.GridPosition) > HostileAggroTiles)
                {
                    continue;
                }

                field ??= BuildPlayerDistanceField();
                if (!field.TryGetValue(enemy.GridPosition, out int here))
                {
                    continue;   // out of walking range (behind a wall, say)
                }

                Point? best = null;
                int bestDistance = here;
                foreach (Point offset in NeighbourOffsets)
                {
                    var tile = new Point(enemy.GridPosition.X + offset.X, enemy.GridPosition.Y + offset.Y);
                    if (field.TryGetValue(tile, out int distance) && distance < bestDistance &&
                        tile != _player.GridPosition && !_map.TryGetTravel(tile, out _) &&
                        !enemies.Any(other => other != enemy && other.IsAlive && other.GridPosition == tile))
                    {
                        best = tile;
                        bestDistance = distance;
                    }
                }

                if (best is Point step)
                {
                    enemy.MarchTo(step);
                }
            }

            if (_stats.IsDead)
            {
                FallInTheWilds();
            }
        }

        private static readonly Point[] NeighbourOffsets =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        };

        // Walking distance from the player, out to HostileAggroTiles, over walkable tiles.
        private Dictionary<Point, int> BuildPlayerDistanceField()
        {
            var distances = new Dictionary<Point, int> { [_player.GridPosition] = 0 };
            var queue = new Queue<Point>();
            queue.Enqueue(_player.GridPosition);
            while (queue.Count > 0)
            {
                Point current = queue.Dequeue();
                int next = distances[current] + 1;
                if (next > HostileAggroTiles)
                {
                    continue;
                }

                foreach (Point offset in NeighbourOffsets)
                {
                    var tile = new Point(current.X + offset.X, current.Y + offset.Y);
                    if (!distances.ContainsKey(tile) && _map.IsWalkable(tile))
                    {
                        distances[tile] = next;
                        queue.Enqueue(tile);
                    }
                }
            }

            return distances;
        }

        // The player died in the forest or dungeon: back to the village at full health.
        private void FallInTheWilds()
        {
            _stats.RestoreFully();
            SwitchMap(new MapTravel(WorldMapType.Village, _maps[WorldMapType.Village].SpawnPoint));
            ShowBanner("YOU FELL");
            Console.WriteLine("[Hostile] The player fell and woke up in the village.");
        }

        private bool TrySpawnWaveEnemy(EnemySpawnInfo info, List<Enemy> enemies)
        {
            int start = Random.Shared.Next(_entryPoints.Length);
            for (int i = 0; i < _entryPoints.Length; i++)
            {
                Point candidate = _entryPoints[(start + i) % _entryPoints.Length];
                if (candidate != _player.GridPosition &&
                    !enemies.Any(enemy => enemy.IsAlive && enemy.GridPosition == candidate))
                {
                    enemies.Add(new Enemy(candidate, TileSize, info.Health)
                    {
                        IsWaveEnemy = true,
                        Kind = info.Kind,
                        Variant = info.Variant,
                        HearthDamage = info.HearthDamage,
                        MarchTilesPerSecond = info.Speed * MarchSpeedScale,
                    });
                    return true;
                }
            }

            return false;
        }

        private bool IsTileTakenForEnemy(Point tile, Enemy self)
        {
            return tile == _player.GridPosition ||
                   _enemies[WorldMapType.Village].Any(
                       other => other != self && other.IsAlive && other.GridPosition == tile);
        }

        // Wave cleared: back to day, a new day is counted, and harvested trees
        // and rocks grow back.
        private void EndNight()
        {
            _gameState = GameState.Day;
            _hearthPaths = null;
            _dayCount++;
            _history.ClearForNewDay();   // yesterday's crafting can no longer be undone
            RestoreResourceNodesForNewDay();

            if (_nightNumber >= MaxNights)
            {
                _allNightsSurvived = true;   // the Victory screen opens at the end of this method
            }
            else
            {
                ShowBanner($"DAWN OF DAY {_dayCount}");
            }

            if (_nightNumber == 2 && !_bonusQuestInserted)
            {
                InsertBonusQuest();
            }
            _questLog.OnNightSurvived(_nightNumber);

            Console.WriteLine($"[Night] Night {_nightNumber} cleared. Day {_dayCount} begins. Kills so far: {_kills}.");

            if (_allNightsSurvived)
            {
                BeginEnding(EndingKind.Victory, "THE VILLAGE IS SAFE");
            }
        }

        // The night is lost (hearth destroyed, or the player fell): open the
        // Defeat ending. The queued wave is dropped, but the world is otherwise
        // left as it is, frozen behind the screen, until the player picks
        // PLAY AGAIN or RETURN TO TITLE (both rebuild the world in ResetGame).
        private void FailNight(string banner, bool playerFell = false)
        {
            _spawner.ClearWave();
            _pendingWaveSpawn = null;
            Console.WriteLine(playerFell
                ? $"[Night] The player fell on night {_nightNumber}. Defeat."
                : $"[Night] The hearth was destroyed on night {_nightNumber}. Defeat.");
            BeginEnding(EndingKind.Defeat, banner);
        }

        // Opens the ending screen. Update's non-Playing branch only handles menu
        // input from here on, so nothing in the world moves behind it.
        private void BeginEnding(EndingKind kind, string reason)
        {
            _endingKind = kind;
            _endingReason = reason;
            _endingSelection = 0;
            _endingInputDelay = EndingInputDelaySeconds;
            _pendingTravel = null;
            _pendingNightStart = false;
            _questPanelOpen = false;
            _screen = GameScreen.Ending;
            Console.WriteLine($"[Ending] {kind}: {reason}. Nights survived {_dayCount - 1} of {MaxNights}, kills {_kills}.");
        }

        // Puts every piece of run state back to a fresh launch: a new world, bag,
        // quests, stats, hearth, and day/night counters. Used by both PLAY AGAIN
        // and RETURN TO TITLE on the ending screen, so NEW GAME afterwards starts
        // from a clean world.
        private void ResetGame()
        {
            BuildWorld();
            PrepareMapVisuals();
            SpawnDungeonSkeletons();

            _inventory.Clear();
            _history.ClearForNewDay();
            _questLog.Reset();
            _stats.RestoreFully();
            _hearth.RestoreFully();
            _spawner.ClearWave();
            _pendingWaveSpawn = null;
            _hearthPaths = null;
            _entryPoints = Array.Empty<Point>();

            _gameState = GameState.Day;
            _dayCount = 1;
            _nightNumber = 0;
            _kills = 0;
            _allNightsSurvived = false;
            _pendingNightStart = false;
            _pendingTravel = null;
            _bannerText = string.Empty;
            _bannerRemaining = 0f;

            _openChest = null;
            _chestSelection = 0;
            _traderOpen = false;
            _traderSelection = 0;
            _foodShopOpen = false;
            _foodSelection = 0;
            _inventoryOpen = false;
            _inventorySelection = 0;
            _inventoryScroll = 0;
            _equippedWeaponName = null;
            _questPanelOpen = false;
            _bonusQuestInserted = false;
            _hasTalkedToWizard = false;
            _endingSelection = 0;
            _endingInputDelay = 0f;
            _menuMessage = string.Empty;
            Console.WriteLine("[Ending] Game state reset for a new run.");
        }

        private void SaveGame()
        {
            try
            {
                GameSaveStore.Save(CaptureGameState());
                _menuMessage = "GAME SAVED";
                Console.WriteLine($"[Save] Game saved to {GameSaveStore.SavePath}.");
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                _menuMessage = "SAVE FAILED - CHECK THE SAVE FOLDER";
                Console.WriteLine($"[Save] Failed to save the game: {exception.Message}");
            }
        }

        private GameSaveData CaptureGameState()
        {
            var save = new GameSaveData
            {
                Map = _map.MapType,
                ForestSeed = _maps[WorldMapType.Forest].ForestSeed,
                PlayerPosition = SavePoint(_player.GridPosition),
                PlayerFacing = _player.Facing,
                Health = _stats.Health,
                Energy = _stats.EnergyAmount,
                HearthHealth = _hearth.Health,
                GameState = _gameState,
                DayCount = _dayCount,
                NightNumber = _nightNumber,
                Kills = _kills,
                AllNightsSurvived = _allNightsSurvived,
                BonusQuestInserted = _bonusQuestInserted,
                HasTalkedToWizard = _hasTalkedToWizard,
                EquippedWeaponName = _equippedWeaponName,
                QuestIndex = _questLog.CurrentIndex,
                QuestProgress = _questLog.CurrentProgress,
                SpawnTimer = _spawner.TimeUntilNextSpawn,
            };
            foreach (KeyValuePair<string, long> total in _questLog.GetGatheredResourceTotals())
            {
                save.GatheredResources.Add(new SavedQuestResource
                {
                    Type = total.Key,
                    Quantity = total.Value,
                });
            }

            foreach (ItemCategory category in ItemCatalog.Categories)
            {
                foreach ((string type, int quantity) in _inventory.GetItems(category))
                {
                    save.Inventory.Add(new SavedItem { Name = type, Quantity = quantity });
                }
            }

            foreach (TileMap map in _maps.Values)
            {
                var savedMap = new SavedMap { Map = map.MapType };
                foreach ((Point tile, int hitsRemaining) in map.GetResourceNodeStates())
                {
                    savedMap.ResourceNodes.Add(new SavedResourceNode
                    {
                        Tile = SavePoint(tile),
                        HitsRemaining = hitsRemaining,
                    });
                }
                foreach (KeyValuePair<Point, ResourceType> pickup in map.Pickups)
                {
                    savedMap.Pickups.Add(new SavedPickup
                    {
                        Tile = SavePoint(pickup.Key),
                        Type = pickup.Value,
                    });
                }
                save.Maps.Add(savedMap);
            }

            foreach (TreasureChest chest in _dungeonChests)
            {
                var savedChest = new SavedChest { Tile = SavePoint(chest.Tiles[0]) };
                foreach (Item item in chest.Items)
                {
                    savedChest.Items.Add(new SavedItem { Name = item.Name, Quantity = item.Quantity });
                }
                save.Chests.Add(savedChest);
            }

            foreach (KeyValuePair<WorldMapType, List<Enemy>> pair in _enemies)
            {
                foreach (Enemy enemy in pair.Value)
                {
                    if (!enemy.IsAlive)
                    {
                        continue;
                    }

                    save.Enemies.Add(new SavedEnemy
                    {
                        Map = pair.Key,
                        Tile = SavePoint(enemy.GridPosition),
                        Health = enemy.Health,
                        MaxHealth = enemy.MaxHealth,
                        IsWaveEnemy = enemy.IsWaveEnemy,
                        IsHostile = enemy.IsHostile,
                        Kind = enemy.Kind,
                        Variant = enemy.Variant,
                        HearthDamage = enemy.HearthDamage,
                        MarchTilesPerSecond = enemy.MarchTilesPerSecond,
                    });
                }
            }

            foreach (EnemySpawnInfo spawn in _spawner.GetPendingSpawns())
            {
                save.PendingSpawns.Add(SaveSpawn(spawn));
            }
            if (_pendingWaveSpawn is EnemySpawnInfo pendingSpawn)
            {
                save.PendingWaveSpawn = SaveSpawn(pendingSpawn);
            }

            return save;
        }

        private void LoadGame()
        {
            try
            {
                GameSaveData save = GameSaveStore.Load();
                ValidateGameSave(save);
                RestoreGameState(save);
                _menuMessage = string.Empty;
                _screen = GameScreen.Playing;
                Console.WriteLine($"[Save] Game loaded from {GameSaveStore.SavePath}.");
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or JsonException or
                ArgumentException or InvalidOperationException)
            {
                _menuMessage = "LOAD FAILED - SAVE FILE IS INVALID OR UNAVAILABLE";
                Console.WriteLine($"[Save] Failed to load the game: {exception.Message}");
            }
        }

        private static void ValidateGameSave(GameSaveData save)
        {
            if (!Enum.IsDefined(save.Map) ||
                !Enum.IsDefined(save.GameState) ||
                !Enum.IsDefined(save.PlayerFacing) ||
                save.Health < 0 || save.Health > PlayerStats.MaxHealth ||
                float.IsNaN(save.Energy) || float.IsInfinity(save.Energy) ||
                save.Energy < 0f || save.Energy > PlayerStats.MaxEnergy ||
                save.HearthHealth < 0 || save.HearthHealth > Hearth.DefaultMaxHealth ||
                save.DayCount < 1 || save.DayCount > MaxNights + 1 ||
                save.NightNumber < 0 || save.NightNumber > MaxNights ||
                save.Kills < 0 || save.QuestIndex < 0 || save.QuestProgress < 0 ||
                save.PlayerPosition == null ||
                float.IsNaN(save.SpawnTimer) || float.IsInfinity(save.SpawnTimer) ||
                save.SpawnTimer < 0f ||
                save.Inventory == null || save.Maps == null || save.Chests == null ||
                save.Enemies == null || save.PendingSpawns == null ||
                save.GatheredResources == null ||
                save.Maps.Count != Enum.GetValues<WorldMapType>().Length)
            {
                throw new InvalidDataException("The save file contains invalid game state.");
            }

            var seenMaps = new HashSet<WorldMapType>();
            foreach (SavedMap map in save.Maps)
            {
                if (map == null || !Enum.IsDefined(map.Map) || !seenMaps.Add(map.Map) ||
                    map.ResourceNodes == null || map.Pickups == null)
                {
                    throw new InvalidDataException("The save file contains invalid map data.");
                }
                foreach (SavedResourceNode node in map.ResourceNodes)
                {
                    if (node?.Tile == null || node.HitsRemaining < 0 || node.HitsRemaining > 4 ||
                        !IsPointWithinMap(map.Map, node.Tile))
                    {
                        throw new InvalidDataException("The save file contains invalid resource-node data.");
                    }
                }
                foreach (SavedPickup pickup in map.Pickups)
                {
                    if (pickup?.Tile == null || !Enum.IsDefined(pickup.Type) ||
                        !IsPointWithinMap(map.Map, pickup.Tile))
                    {
                        throw new InvalidDataException("The save file contains invalid pickup data.");
                    }
                }
            }
            if (!IsPointWithinMap(save.Map, save.PlayerPosition))
            {
                throw new InvalidDataException("The saved player position is outside the map.");
            }

            foreach (SavedItem item in save.Inventory)
            {
                ValidateSavedItem(item);
            }
            var gatheredResourceTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (SavedQuestResource resource in save.GatheredResources)
            {
                if (resource == null || !ItemCatalog.IsKnown(resource.Type) ||
                    ItemCatalog.CategoryOf(resource.Type) != ItemCategory.Resource ||
                    resource.Quantity <= 0 || !gatheredResourceTypes.Add(resource.Type))
                {
                    throw new InvalidDataException("The save file contains invalid quest resource totals.");
                }
            }
            foreach (SavedChest chest in save.Chests)
            {
                if (chest == null || chest.Tile == null || chest.Items == null ||
                    !IsPointWithinMap(WorldMapType.Dungeon, chest.Tile))
                {
                    throw new InvalidDataException("The save file contains invalid chest data.");
                }
                foreach (SavedItem item in chest.Items)
                {
                    ValidateSavedItem(item);
                }
            }
            foreach (SavedEnemy enemy in save.Enemies)
            {
                if (enemy == null || enemy.Tile == null || !Enum.IsDefined(enemy.Map) ||
                    !Enum.IsDefined(enemy.Kind) || !Enum.IsDefined(enemy.Variant) ||
                    enemy.MaxHealth <= 0 || enemy.Health <= 0 || enemy.Health > enemy.MaxHealth ||
                    enemy.HearthDamage < 0 || enemy.MarchTilesPerSecond < 0f ||
                    float.IsNaN(enemy.MarchTilesPerSecond) || float.IsInfinity(enemy.MarchTilesPerSecond) ||
                    !IsPointWithinMap(enemy.Map, enemy.Tile))
                {
                    throw new InvalidDataException("The save file contains invalid enemy data.");
                }
            }
            foreach (SavedSpawn spawn in save.PendingSpawns)
            {
                ValidateSavedSpawn(spawn);
            }
            if (save.PendingWaveSpawn != null)
            {
                ValidateSavedSpawn(save.PendingWaveSpawn);
            }

            int questCount = save.BonusQuestInserted ? 9 : 8;
            if (save.QuestIndex > questCount ||
                (save.QuestIndex == questCount && save.QuestProgress != 0) ||
                (save.QuestIndex < questCount &&
                 save.QuestProgress > RequiredQuestAmount(save.QuestIndex, save.BonusQuestInserted)))
            {
                throw new InvalidDataException("The save file contains invalid quest progress.");
            }

            if (save.EquippedWeaponName != null &&
                (!ItemCatalog.IsKnown(save.EquippedWeaponName) ||
                 ItemCatalog.CategoryOf(save.EquippedWeaponName) != ItemCategory.Weapon))
            {
                throw new InvalidDataException("The save file contains an invalid equipped weapon.");
            }
        }

        private static bool IsPointWithinMap(WorldMapType map, SavedPoint point)
        {
            (int width, int height) = map switch
            {
                WorldMapType.Village => (48, 36),
                WorldMapType.Forest => (36, 29),
                WorldMapType.Dungeon => (36, 28),
                _ => (0, 0),
            };
            return point.X >= 0 && point.X < width && point.Y >= 0 && point.Y < height;
        }

        private static int RequiredQuestAmount(int index, bool bonusQuestInserted)
        {
            if (bonusQuestInserted && index == 5)
            {
                return 5;
            }
            if (bonusQuestInserted && index > 5)
            {
                index--;
            }
            return index switch
            {
                0 => 10,
                1 => 5,
                _ => 1,
            };
        }

        private static void ValidateSavedItem(SavedItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Name) ||
                !ItemCatalog.IsKnown(item.Name) || item.Quantity <= 0)
            {
                throw new InvalidDataException("The save file contains an invalid item.");
            }
        }

        private static void ValidateSavedSpawn(SavedSpawn spawn)
        {
            if (spawn == null || !Enum.IsDefined(spawn.Kind) || !Enum.IsDefined(spawn.Variant) ||
                spawn.SpawnDelaySeconds < 0f || spawn.Health <= 0 || spawn.Speed < 0f ||
                spawn.HearthDamage < 0 ||
                float.IsNaN(spawn.SpawnDelaySeconds) || float.IsInfinity(spawn.SpawnDelaySeconds) ||
                float.IsNaN(spawn.Speed) || float.IsInfinity(spawn.Speed))
            {
                throw new InvalidDataException("The save file contains invalid wave data.");
            }
        }

        private void RestoreGameState(GameSaveData save)
        {
            BuildWorld(save.ForestSeed);
            SpawnDungeonSkeletons();

            foreach (SavedMap savedMap in save.Maps)
            {
                TileMap map = _maps[savedMap.Map];
                foreach (SavedResourceNode node in savedMap.ResourceNodes)
                {
                    if (node?.Tile == null || !map.RestoreResourceNodeHits(
                            new Point(node.Tile.X, node.Tile.Y), node.HitsRemaining))
                    {
                        Console.WriteLine($"[Save] Ignored an unavailable resource node on {savedMap.Map}.");
                    }
                }

                var pickups = new List<KeyValuePair<Point, ResourceType>>(savedMap.Pickups.Count);
                foreach (SavedPickup pickup in savedMap.Pickups)
                {
                    if (pickup?.Tile == null || !Enum.IsDefined(pickup.Type))
                    {
                        throw new InvalidDataException("The save file contains an invalid pickup.");
                    }
                    pickups.Add(new KeyValuePair<Point, ResourceType>(
                        new Point(pickup.Tile.X, pickup.Tile.Y), pickup.Type));
                }
                map.RestorePickups(pickups);
            }

            foreach (SavedChest savedChest in save.Chests)
            {
                TreasureChest? chest = _dungeonChests.Find(candidate =>
                    candidate.Tiles[0].X == savedChest.Tile.X &&
                    candidate.Tiles[0].Y == savedChest.Tile.Y);
                if (chest == null)
                {
                    Console.WriteLine("[Save] Ignored an unavailable chest in the saved world.");
                    continue;
                }

                var items = new List<Item>(savedChest.Items.Count);
                foreach (SavedItem item in savedChest.Items)
                {
                    items.Add(new Item(item.Name, item.Quantity));
                }
                chest.RestoreItems(items);
            }

            foreach (List<Enemy> enemies in _enemies.Values)
            {
                enemies.Clear();
            }
            foreach (SavedEnemy savedEnemy in save.Enemies)
            {
                TileMap map = _maps[savedEnemy.Map];
                var tile = new Point(savedEnemy.Tile.X, savedEnemy.Tile.Y);
                if (!map.IsWalkable(tile))
                {
                    Console.WriteLine($"[Save] Ignored an enemy on an unavailable tile in {savedEnemy.Map}.");
                    continue;
                }

                var enemy = new Enemy(tile, TileSize, savedEnemy.MaxHealth)
                {
                    IsWaveEnemy = savedEnemy.IsWaveEnemy,
                    IsHostile = savedEnemy.IsHostile,
                    Kind = savedEnemy.Kind,
                    Variant = savedEnemy.Variant,
                    HearthDamage = savedEnemy.HearthDamage,
                    MarchTilesPerSecond = savedEnemy.MarchTilesPerSecond,
                };
                enemy.RestoreHealth(savedEnemy.Health);
                _enemies[savedEnemy.Map].Add(enemy);
            }

            _inventory.Clear();
            foreach (SavedItem item in save.Inventory)
            {
                _inventory.Add(item.Name, item.Quantity);
            }

            _questLog.Reset();
            _bonusQuestInserted = false;
            if (save.BonusQuestInserted)
            {
                InsertBonusQuest();
            }
            _questLog.RestoreProgress(save.QuestIndex, save.QuestProgress);
            var gatheredResources = new List<KeyValuePair<string, long>>(save.GatheredResources.Count);
            foreach (SavedQuestResource resource in save.GatheredResources)
            {
                gatheredResources.Add(new KeyValuePair<string, long>(resource.Type, resource.Quantity));
            }
            _questLog.RestoreGatheredResourceTotals(gatheredResources);

            _stats.RestoreState(save.Health, save.Energy);
            _hearth.RestoreHealth(save.HearthHealth);
            _gameState = save.GameState;
            _dayCount = save.DayCount;
            _nightNumber = save.NightNumber;
            _kills = save.Kills;
            _allNightsSurvived = save.AllNightsSurvived;
            _hasTalkedToWizard = save.HasTalkedToWizard;
            _equippedWeaponName = save.EquippedWeaponName != null &&
                                  ItemCatalog.IsKnown(save.EquippedWeaponName) &&
                                  _inventory.Has(save.EquippedWeaponName)
                ? save.EquippedWeaponName
                : null;
            _history.ClearForNewDay();

            _map = _maps[save.Map];
            _camera = new Camera2D(
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight,
                _map.Width,
                _map.Height,
                TileSize);
            Point playerPosition = new(save.PlayerPosition.X, save.PlayerPosition.Y);
            if (!_map.IsWalkable(playerPosition))
            {
                playerPosition = _map.SpawnPoint;
                Console.WriteLine($"[Save] Player position was unavailable; restored at the {save.Map} entrance.");
            }
            _player.RestoreState(playerPosition, save.PlayerFacing);

            var pendingSpawns = new List<EnemySpawnInfo>(save.PendingSpawns.Count);
            foreach (SavedSpawn spawn in save.PendingSpawns)
            {
                pendingSpawns.Add(RestoreSpawn(spawn));
            }
            _spawner.RestoreState(pendingSpawns, save.SpawnTimer);
            _pendingWaveSpawn = save.PendingWaveSpawn == null
                ? null
                : RestoreSpawn(save.PendingWaveSpawn);

            _hearthPaths = null;
            _entryPoints = Array.Empty<Point>();
            if (_gameState == GameState.Night)
            {
                TileMap village = _maps[WorldMapType.Village];
                _hearthPaths = new HearthPathField(
                    _hearth.Tiles, village.IsWalkable, village.Width, village.Height);
                _entryPoints = VillageCollisionLayout.EnemyEntryPoints
                    .Where(point => _hearthPaths.IsReachable(point))
                    .ToArray();
            }

            _pendingTravel = null;
            _pendingNightStart = false;
            _openChest = null;
            _traderOpen = false;
            _foodShopOpen = false;
            _inventoryOpen = false;
            _questPanelOpen = false;
            _bannerText = string.Empty;
            _bannerRemaining = 0f;
            _camera.SnapTo(_player.PixelPosition);
            PrepareMapVisuals();
        }

        private static SavedPoint SavePoint(Point point) => new() { X = point.X, Y = point.Y };

        private static SavedSpawn SaveSpawn(EnemySpawnInfo spawn) => new()
        {
            Kind = spawn.Kind,
            Variant = spawn.Variant,
            SpawnDelaySeconds = spawn.SpawnDelaySeconds,
            Health = spawn.Health,
            Speed = spawn.Speed,
            HearthDamage = spawn.HearthDamage,
        };

        private static EnemySpawnInfo RestoreSpawn(SavedSpawn spawn) => new(
            spawn.Kind, spawn.Variant, spawn.SpawnDelaySeconds,
            spawn.Health, spawn.Speed, spawn.HearthDamage);

        private void InsertBonusQuest()
        {
            QuestNode? nightTwoQuest = _questLog.Find(quest =>
                quest.Kind == QuestKind.SurviveNight &&
                quest.Target is int night && night == 2);
            if (nightTwoQuest == null)
            {
                throw new InvalidOperationException("The night-two quest is missing.");
            }

            if (_questLog.Find(quest => quest.Kind == QuestKind.DefeatEnemies) == null)
            {
                _questLog.InsertAfterCurrent(
                    new QuestNode("Defeat 5 enemies", QuestKind.DefeatEnemies, 5, 5),
                    nightTwoQuest);
            }
            _bonusQuestInserted = true;
        }

        private void RestoreResourceNodesForNewDay()
        {
            bool restoredAny = false;
            foreach (TileMap map in _maps.Values)
            {
                restoredAny |= map.RestoreResourceNodes(tile =>
                    (map == _map && tile == _player.GridPosition) ||
                    _enemies[map.MapType].Any(enemy => enemy.IsAlive && enemy.GridPosition == tile));
            }

            if (restoredAny)
            {
                PrepareMapVisuals();
            }
        }

        private bool IsAdjacentToPlayer(Point tile)
        {
            Point player = _player.GridPosition;
            return Math.Abs(player.X - tile.X) + Math.Abs(player.Y - tile.Y) == 1;
        }

        private int FoesRemaining()
        {
            return _spawner.RemainingInWave +
                   (_pendingWaveSpawn == null ? 0 : 1) +
                   _enemies[WorldMapType.Village].Count(enemy => enemy.IsWaveEnemy && enemy.IsAlive);
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

            if (_map.TryCollectPickup(position, out var pickup))
            {
                _inventory.Add(pickup.Name, pickup.Quantity);
                _questLog.OnResourceGathered(pickup.Name, pickup.Quantity);
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

        private CraftingRecipe? EquippedWeapon()
        {
            if (_equippedWeaponName != null)
            {
                if (_inventory.Has(_equippedWeaponName))
                {
                    return RecipeBook.FindByName(_equippedWeaponName);
                }

                _equippedWeaponName = null;
            }

            return Crafting.GetBestWeapon(_inventory);
        }

        // The menu list: every owned item, grouped Weapon / Food / Resource, in the
        // inventory's current display order (catalog order, or value order after 'I').
        private List<(string Name, ItemCategory Category, int Quantity)> GetInventoryEntries()
        {
            var entries = new List<(string Name, ItemCategory Category, int Quantity)>();
            foreach (ItemCategory category in ItemCatalog.Categories)
            {
                foreach ((string type, int qty) in _inventory.GetItems(category))
                {
                    entries.Add((type, category, qty));
                }
            }

            return entries;
        }

        private void UpdateInventoryMenu(KeyboardState keyboard)
        {
            List<(string Name, ItemCategory Category, int Quantity)> entries = GetInventoryEntries();
            if (WasPressed(keyboard, Keys.Escape))
            {
                _inventoryOpen = false;
                return;
            }

            // 'I' inside the menu toggles value-sorted order (see Inventory).
            if (WasPressed(keyboard, Keys.I))
            {
                if (_inventory.IsSortedByValue)
                {
                    _inventory.ResetOrder();
                }
                else
                {
                    _inventory.SortByValue();
                }
                entries = GetInventoryEntries();
            }

            if (entries.Count == 0)
            {
                _inventorySelection = 0;
                _inventoryScroll = 0;
                return;
            }

            _inventorySelection = Math.Clamp(_inventorySelection, 0, entries.Count - 1);
            if (WasPressed(keyboard, Keys.Up) || WasPressed(keyboard, Keys.W))
            {
                _inventorySelection = (_inventorySelection + entries.Count - 1) % entries.Count;
            }
            else if (WasPressed(keyboard, Keys.Down) || WasPressed(keyboard, Keys.S))
            {
                _inventorySelection = (_inventorySelection + 1) % entries.Count;
            }
            else if (WasPressed(keyboard, Keys.Enter) || WasPressed(keyboard, Keys.E))
            {
                var selected = entries[_inventorySelection];
                if (selected.Category == ItemCategory.Weapon &&
                    RecipeBook.FindByName(selected.Name) is CraftingRecipe recipe)
                {
                    _equippedWeaponName = recipe.Name;
                    ShowBanner($"EQUIPPED {recipe.Name.ToUpperInvariant()}");
                }
                else if (selected.Category == ItemCategory.Food)
                {
                    switch (FoodShop.TryEat(_inventory, _stats, selected.Name))
                    {
                        case EatResult.Eaten:
                            ShowBanner($"ATE {selected.Name.ToUpperInvariant()}");
                            entries = GetInventoryEntries();
                            _inventorySelection = Math.Min(_inventorySelection, Math.Max(0, entries.Count - 1));
                            break;
                        case EatResult.AlreadyFull:
                            ShowBanner("ENERGY IS ALREADY FULL");
                            break;
                        default:
                            ShowBanner("ITEM CANNOT BE USED");
                            break;
                    }
                }
                else
                {
                    ShowBanner("ITEM CANNOT BE USED");
                }
            }
            else if (WasPressed(keyboard, Keys.D))
            {
                var selected = entries[_inventorySelection];
                if (_inventory.TryRemove(selected.Name))
                {
                    if (_equippedWeaponName == selected.Name && !_inventory.Has(selected.Name))
                    {
                        _equippedWeaponName = null;
                    }
                    ShowBanner($"DROPPED {selected.Name.ToUpperInvariant()}");
                    entries = GetInventoryEntries();
                    _inventorySelection = Math.Min(_inventorySelection, Math.Max(0, entries.Count - 1));
                }
            }

            const int visibleRows = 10;
            _inventoryScroll = Math.Clamp(_inventoryScroll, 0, Math.Max(0, entries.Count - visibleRows));
            if (_inventorySelection < _inventoryScroll)
            {
                _inventoryScroll = _inventorySelection;
            }
            else if (_inventorySelection >= _inventoryScroll + visibleRows)
            {
                _inventoryScroll = _inventorySelection - visibleRows + 1;
            }
        }

        private void DrawInventoryMenu()
        {
            List<(string Name, ItemCategory Category, int Quantity)> entries = GetInventoryEntries();
            Viewport viewport = GraphicsDevice.Viewport;
            const int panelMargin = 36;
            const int rowHeight = 42;
            const int visibleRows = 10;
            var panel = new Rectangle(
                panelMargin,
                panelMargin,
                viewport.Width - panelMargin * 2,
                viewport.Height - panelMargin * 2);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_pixelTexture, viewport.Bounds, new Color(0, 0, 0, 190));
            DrawHudPanel(panel);
            BitmapFont.Draw(_spriteBatch, _pixelTexture, "INVENTORY", new Point(panel.X + 16, panel.Y + 14), 3, Color.Wheat);
            BitmapFont.Draw(
                _spriteBatch, _pixelTexture,
                "UP DOWN SELECT   ENTER USE   D DROP ONE   ESC CLOSE",
                new Point(panel.X + 16, panel.Y + 48), 1, Color.LightGray);

            int visibleCount = Math.Min(visibleRows, Math.Max(0, entries.Count - _inventoryScroll));
            for (int rowIndex = 0; rowIndex < visibleCount; rowIndex++)
            {
                int itemIndex = _inventoryScroll + rowIndex;
                var entry = entries[itemIndex];
                int rowY = panel.Y + 72 + rowIndex * rowHeight;
                var row = new Rectangle(panel.X + 12, rowY, panel.Width - 24, rowHeight - 4);
                bool selected = itemIndex == _inventorySelection;
                _spriteBatch.Draw(
                    _pixelTexture,
                    row,
                    selected ? new Color(60, 46, 30, 245) : new Color(32, 25, 20, 235));
                DrawHudBorder(row, selected ? Color.Gold : new Color(96, 74, 46));
                DrawInventoryItemIcon(entry.Name, new Rectangle(row.X + 8, row.Y + 6, 24, 24));
                BitmapFont.Draw(
                    _spriteBatch, _pixelTexture,
                    entry.Name.ToUpperInvariant(),
                    new Point(row.X + 42, row.Y + 7), 2, Color.White);
                BitmapFont.Draw(
                    _spriteBatch, _pixelTexture,
                    entry.Category.ToString().ToUpperInvariant(),
                    new Point(row.X + 300, row.Y + 10), 1, Color.LightGray);
                BitmapFont.Draw(
                    _spriteBatch, _pixelTexture,
                    $"X {entry.Quantity}",
                    new Point(row.Right - 72, row.Y + 10), 1, Color.Wheat);

                if (entry.Category == ItemCategory.Weapon &&
                    EquippedWeapon()?.Name == entry.Name)
                {
                    BitmapFont.Draw(
                        _spriteBatch, _pixelTexture, "EQUIPPED",
                        new Point(row.Right - 150, row.Y + 10), 1, Color.LimeGreen);
                }

                if (entry.Category == ItemCategory.Food && FoodBook.FindByName(entry.Name) is FoodItem foodInfo)
                {
                    BitmapFont.Draw(
                        _spriteBatch, _pixelTexture, $"+{foodInfo.Energy} ENERGY",
                        new Point(row.Right - 150, row.Y + 10), 1, Color.LimeGreen);
                }
            }

            if (entries.Count == 0)
            {
                BitmapFont.Draw(
                    _spriteBatch, _pixelTexture, "INVENTORY EMPTY",
                    new Point(panel.X + 24, panel.Y + 92), 2, Color.LightGray);
            }

            _spriteBatch.End();
        }

        protected override void Update(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();
            _bannerRemaining = Math.Max(0f, _bannerRemaining - (float)gameTime.ElapsedGameTime.TotalSeconds);
            _endingInputDelay = Math.Max(0f, _endingInputDelay - (float)gameTime.ElapsedGameTime.TotalSeconds);
            if (WasPressed(keyboard, Keys.F3))
            {
                _showHitboxes = !_showHitboxes;
            }

            if (_screen != GameScreen.Playing)
            {
                // Title / How to Play: menu input only, the world stays frozen.
                UpdateMenuScreen(keyboard);
                _previousKeyboard = keyboard;
                base.Update(gameTime);
                return;
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
            else if (_pendingNightStart)
            {
                if (WasPressed(keyboard, Keys.Y) || WasPressed(keyboard, Keys.Enter))
                {
                    StartNight();
                }
                else if (WasPressed(keyboard, Keys.N) || WasPressed(keyboard, Keys.Escape))
                {
                    _pendingNightStart = false;
                }
            }
            else if (_inventoryOpen)
            {
                UpdateInventoryMenu(keyboard);
            }
            else if (_openChest != null)
            {
                UpdateChestMenu(keyboard);
            }
            else if (_traderOpen)
            {
                UpdateTraderMenu(keyboard);
            }
            else if (_foodShopOpen)
            {
                UpdateFoodShopMenu(keyboard);
            }
            else if (WasPressed(keyboard, Keys.Escape))
            {
                _screen = GameScreen.Pause;
                _titleSelection = 0;
            }
            else
            {
                bool attackPressed = WasPressed(keyboard, Keys.Space) || WasPressed(keyboard, Keys.J);
                bool interactPressed = WasPressed(keyboard, Keys.E);
                if (WasPressed(keyboard, Keys.Q))
                {
                    _questPanelOpen = !_questPanelOpen;
                }

                bool interacted = interactPressed && TryOpenNearbyChest();
                if (!interacted && interactPressed)
                {
                    interacted = TryInteractWithNearbyWorldObject();
                }
                if (WasPressed(keyboard, Keys.U))
                {
                    TryUndoLastAction();
                }

                if (!interacted && (WasPressed(keyboard, Keys.I) || WasPressed(keyboard, Keys.E)))
                {
                    if (WasPressed(keyboard, Keys.I))
                    {
                        // 'I' sorts every row by market value, highest first. The order
                        // lasts until the inventory next changes or 'I' is pressed
                        // again inside the menu (which switches back to catalog order).
                        _inventory.SortByValue();
                    }
                    _inventoryOpen = true;
                    _inventorySelection = 0;
                    _inventoryScroll = 0;
                    interacted = true;
                }

                if (!interacted)
                {
                    _player.Update(gameTime, _map, keyboard, attackPressed, IsEnemyAt);
                    _stats.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
                    UpdateEnemies(gameTime);
                    if (_gameState == GameState.Night)
                    {
                        UpdateNight((float)gameTime.ElapsedGameTime.TotalSeconds);
                    }

                    // UpdateNight can end the run this frame; the world stops there.
                    if (_screen == GameScreen.Playing && _map.MapType != WorldMapType.Village)
                    {
                        UpdateHostileEnemies();
                    }
                }
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
                DrawVillageObjectLayer(_villageObjectsGroundTexture);
                DrawHearthPlatform(_spriteBatch);
                DrawVillageActors(gameTime);
                DrawVillageObjectLayer(_villageObjectsMainTexture);
                DrawTrader(gameTime);
                DrawFoodVendor(gameTime);
            }
            else if (_map.MapType == WorldMapType.Dungeon)
            {
                DrawDungeon();
                DrawPickups(gameTime);
                DrawActors(gameTime);
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
                DrawActors(gameTime);
                DrawDepthSortedObjects(gameTime);
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

            if (_gameState == GameState.Night && _map.MapType == WorldMapType.Village)
            {
                DrawNightOverlay(gameTime);
            }

            if (_screen != GameScreen.Playing)
            {
                // The village (already drawn above) shows through a dim overlay.
                DrawMenuScreen(gameTime);
                base.Draw(gameTime);
                return;
            }

            DrawInventoryHud();
            DrawDayNightHud();
            DrawQuestHud();

            if (_openChest != null)
            {
                DrawChestPopup(_openChest);
            }

            if (_traderOpen)
            {
                DrawTraderMenu();
            }

            if (_foodShopOpen)
            {
                DrawFoodShopMenu();
            }

            if (_inventoryOpen)
            {
                DrawInventoryMenu();
            }

            if (_pendingTravel is MapTravel travel)
            {
                DrawTravelPrompt(travel);
            }

            if (_pendingNightStart)
            {
                DrawNightPrompt();
            }

            base.Draw(gameTime);
        }

        private void DrawHearthHealthBar()
        {
            if (_gameState != GameState.Night && _hearth.Health >= _hearth.MaxHealth)
            {
                return;
            }

            int barWidth = HearthBarWidth;
            int barX = HearthFootprintCenterX() - barWidth / 2;
            int barY = HearthStatueDestination().Y - 14;   // above the statue's head
            int filled = (int)Math.Round(barWidth * (_hearth.Health / (float)_hearth.MaxHealth));
            _spriteBatch.Draw(_pixelTexture, new Rectangle(barX - 1, barY - 1, barWidth + 2, 7), Color.Black * 0.8f);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(barX, barY, barWidth, 5), new Color(90, 30, 20));
            _spriteBatch.Draw(_pixelTexture, new Rectangle(barX, barY, filled, 5), Color.Orange);
        }

        // Dims the village for the night, then lets the hearth's light cut
        // through the dark with a warm additive glow (it brightens whatever
        // stands on the plaza instead of repainting over it), and finally draws
        // the hearth's health bar above the dimming so it stays readable.
        private void DrawNightOverlay(GameTime gameTime)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_pixelTexture, GraphicsDevice.Viewport.Bounds, new Color(6, 10, 38) * NightDimAlpha);
            _spriteBatch.End();

            _spriteBatch.Begin(
                blendState: BlendState.Additive,
                samplerState: SamplerState.LinearClamp,
                transformMatrix: _camera.GetTransformationMatrix());
            DrawHearthGlow(gameTime);
            _spriteBatch.End();

            _spriteBatch.Begin(transformMatrix: _camera.GetTransformationMatrix(), samplerState: SamplerState.PointClamp);
            DrawHearthHealthBar();
            _spriteBatch.End();
        }

        // The glow pulses gently and fades as the hearth takes damage, so the
        // plaza visibly darkens when the night is going badly.
        private void DrawHearthGlow(GameTime gameTime)
        {
            float seconds = (float)gameTime.TotalGameTime.TotalSeconds;
            float health = _hearth.Health / (float)_hearth.MaxHealth;
            float strength = (0.8f + 0.2f * MathF.Sin(seconds * 2.4f)) * (0.35f + 0.65f * health) * 0.65f;

            Rectangle platform = HearthPlatformDestination();
            int width = platform.Width * 3;
            int height = platform.Height * 3;
            var destination = new Rectangle(
                platform.Center.X - width / 2,
                platform.Center.Y - height / 2,
                width,
                height);
            _spriteBatch.Draw(_hearthGlowTexture, destination, new Color(255, 205, 140) * strength);
        }

        // Top-centre status: day or night counter, wave progress, hearth HP, plus
        // the "beside the hearth" hint and the fading event banner. Text uses
        // Text uses uppercase glyphs supported by BitmapFont.
        private void DrawDayNightHud()
        {
            Viewport viewport = GraphicsDevice.Viewport;
            bool night = _gameState == GameState.Night;
            string title = night ? $"NIGHT {_nightNumber} OF {MaxNights}" : $"DAY {_dayCount}";
            string detail = night
                ? $"FOES LEFT - {FoesRemaining()}  HEARTH HP - {_hearth.Health}"
                : $"NIGHTS SURVIVED - {_dayCount - 1} OF {MaxNights}";

            int panelWidth = Math.Max(360, Math.Max(title.Length, detail.Length) * 12 + 28);
            const int panelHeight = 58;
            int panelX = (viewport.Width - panelWidth) / 2;

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawHudPanel(new Rectangle(panelX, 10, panelWidth, panelHeight));
            BitmapFont.Draw(_spriteBatch, _pixelTexture, title, new Point(panelX + 14, 18), 2,
                night ? Color.Orange : Color.Wheat);
            BitmapFont.Draw(_spriteBatch, _pixelTexture, detail, new Point(panelX + 14, 42), 2, Color.White);

            if (!night && !_allNightsSurvived && !_pendingNightStart && _pendingTravel == null &&
                _openChest == null && IsPlayerBesideHearth())
            {
                const string hint = "READY TO DEFEND THE HEARTH?  E=YES";
                int hintWidth = hint.Length * 12 + 20;
                int hintX = (viewport.Width - hintWidth) / 2;
                int hintY = viewport.Height - 52;
                _spriteBatch.Draw(_pixelTexture, new Rectangle(hintX, hintY, hintWidth, 30), new Color(18, 15, 14, 225));
                BitmapFont.Draw(_spriteBatch, _pixelTexture, hint, new Point(hintX + 10, hintY + 8), 2, Color.Gold);
            }

            if (!night && !_traderOpen && _pendingTravel == null && _pendingNightStart == false &&
                _openChest == null && IsPlayerBesideTrader())
            {
                const string tradeHint = "TRADER  E=CRAFT  U=UNDO";
                int hintWidth = tradeHint.Length * 12 + 20;
                int hintX = (viewport.Width - hintWidth) / 2;
                int hintY = viewport.Height - 52;
                _spriteBatch.Draw(_pixelTexture, new Rectangle(hintX, hintY, hintWidth, 30), new Color(18, 15, 14, 225));
                BitmapFont.Draw(_spriteBatch, _pixelTexture, tradeHint, new Point(hintX + 10, hintY + 8), 2, Color.Gold);
            }

            if (!night && !_foodShopOpen && !_traderOpen && _pendingTravel == null && _pendingNightStart == false &&
                _openChest == null && IsPlayerBesideFoodVendor())
            {
                const string foodHint = "FOOD VENDOR  E=BUY";
                int hintWidth = foodHint.Length * 12 + 20;
                int hintX = (viewport.Width - hintWidth) / 2;
                int hintY = viewport.Height - 52;
                _spriteBatch.Draw(_pixelTexture, new Rectangle(hintX, hintY, hintWidth, 30), new Color(18, 15, 14, 225));
                BitmapFont.Draw(_spriteBatch, _pixelTexture, foodHint, new Point(hintX + 10, hintY + 8), 2, Color.Gold);
            }

            if (_bannerRemaining > 0f)
            {
                float alpha = Math.Clamp(_bannerRemaining / 0.8f, 0f, 1f);
                const int bannerScale = 3;
                int bannerX = (viewport.Width - _bannerText.Length * 6 * bannerScale) / 2;
                BitmapFont.Draw(_spriteBatch, _pixelTexture, _bannerText, new Point(bannerX + 3, 87), bannerScale, Color.Black * (0.8f * alpha));
                BitmapFont.Draw(_spriteBatch, _pixelTexture, _bannerText, new Point(bannerX, 84), bannerScale, Color.Gold * alpha);
            }

            _spriteBatch.End();
        }

        // ---- Title menu and How to Play -------------------------------------
        // Text uses uppercase letters, digits, and punctuation supported by BitmapFont.

        private (string Label, bool Enabled)[] TitleEntries => new[]
        {
            ("NEW GAME", true),
            ("LOAD GAME", GameSaveStore.Exists),
            ("HOW TO PLAY", true),
            ("QUIT", true),
        };

        private static readonly (string Label, bool Enabled)[] PauseEntries =
        {
            ("RESUME", true),
            ("SAVE GAME", true),
            ("HOW TO PLAY", true),
            ("RETURN TO TITLE", true),
            ("QUIT", true),
        };

        private static readonly (string Label, bool Enabled)[] EndingEntries =
        {
            ("PLAY AGAIN", true),
            ("RETURN TO TITLE", true),
            ("QUIT", true),
        };

        private void UpdateMenuScreen(KeyboardState keyboard)
        {
            bool confirm = WasPressed(keyboard, Keys.Enter) || WasPressed(keyboard, Keys.E) || WasPressed(keyboard, Keys.Space);

            if (_screen == GameScreen.Ending)
            {
                UpdateEndingMenu(keyboard, confirm);
                return;
            }

            if (_screen == GameScreen.HowToPlay)
            {
                if (confirm || WasPressed(keyboard, Keys.Escape))
                {
                    _screen = _previousMenuScreen;
                }
                return;
            }

            if (_screen == GameScreen.Pause)
            {
                if (WasPressed(keyboard, Keys.Escape))
                {
                    _screen = GameScreen.Playing;
                    return;
                }

                int step = 0;
                if (WasPressed(keyboard, Keys.Up) || WasPressed(keyboard, Keys.W))
                {
                    step = -1;
                }
                else if (WasPressed(keyboard, Keys.Down) || WasPressed(keyboard, Keys.S))
                {
                    step = 1;
                }

                if (step != 0)
                {
                    _titleSelection = (_titleSelection + step + PauseEntries.Length) % PauseEntries.Length;
                }
                else if (confirm && PauseEntries[_titleSelection].Enabled)
                {
                    switch (PauseEntries[_titleSelection].Label)
                    {
                        case "RESUME":
                            _screen = GameScreen.Playing;
                            break;
                        case "SAVE GAME":
                            SaveGame();
                            break;
                        case "HOW TO PLAY":
                            _previousMenuScreen = GameScreen.Pause;
                            _screen = GameScreen.HowToPlay;
                            break;
                        case "RETURN TO TITLE":
                            _titleSelection = 0;
                            _screen = GameScreen.Title;
                            break;
                        case "QUIT":
                            Exit();
                            break;
                    }
                }

                return;
            }

            if (WasPressed(keyboard, Keys.Escape))
            {
                Exit();
                return;
            }

            (string Label, bool Enabled)[] titleEntries = TitleEntries;
            if (_titleSelection >= titleEntries.Length || !titleEntries[_titleSelection].Enabled)
            {
                _titleSelection = 0;
            }

            int stepTitle = 0;
            if (WasPressed(keyboard, Keys.Up) || WasPressed(keyboard, Keys.W))
            {
                stepTitle = -1;
            }
            else if (WasPressed(keyboard, Keys.Down) || WasPressed(keyboard, Keys.S))
            {
                stepTitle = 1;
            }

            if (stepTitle != 0)
            {
                // Skip disabled entries (Load Game) so the cursor never rests on them.
                do
                {
                    _titleSelection = (_titleSelection + stepTitle + titleEntries.Length) % titleEntries.Length;
                }
                while (!titleEntries[_titleSelection].Enabled);
            }
            else if (confirm && titleEntries[_titleSelection].Enabled)
            {
                switch (titleEntries[_titleSelection].Label)
                {
                    case "NEW GAME":
                        ResetGame();
                        _menuMessage = string.Empty;
                        _screen = GameScreen.Playing;
                        break;
                    case "LOAD GAME":
                        LoadGame();
                        break;
                    case "HOW TO PLAY":
                        _previousMenuScreen = GameScreen.Title;
                        _screen = GameScreen.HowToPlay;
                        break;
                    case "QUIT":
                        Exit();
                        break;
                }
            }
        }

        private void DrawMenuScreen(GameTime gameTime)
        {
            Viewport viewport = GraphicsDevice.Viewport;
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_pixelTexture, viewport.Bounds, new Color(10, 10, 14) * 0.65f);

            if (_screen == GameScreen.HowToPlay)
            {
                DrawHowToPlay(viewport);
            }
            else if (_screen == GameScreen.Pause)
            {
                DrawPauseMenu(viewport, gameTime);
            }
            else if (_screen == GameScreen.Ending)
            {
                DrawEndingScreen(viewport, gameTime);
            }
            else
            {
                DrawTitle(viewport, gameTime);
            }

            _spriteBatch.End();
        }

        private void DrawCenteredText(Viewport viewport, string text, int y, int scale, Color color)
        {
            int x = (viewport.Width - text.Length * 6 * scale) / 2;
            BitmapFont.Draw(_spriteBatch, _pixelTexture, text, new Point(x, y), scale, color);
        }

        private void DrawPauseMenu(Viewport viewport, GameTime gameTime)
        {
            const string title = "PAUSED";
            int titleX = (viewport.Width - title.Length * 6 * 4) / 2;
            BitmapFont.Draw(_spriteBatch, _pixelTexture, title, new Point(titleX + 3, 120 + 3), 4, Color.Black * 0.8f);
            BitmapFont.Draw(_spriteBatch, _pixelTexture, title, new Point(titleX, 120), 4, Color.Gold);

            const int rowHeight = 44;
            const int panelWidth = 360;
            int panelHeight = PauseEntries.Length * rowHeight + 20;
            var panel = new Rectangle((viewport.Width - panelWidth) / 2, 190, panelWidth, panelHeight);
            DrawHudPanel(panel);

            for (int i = 0; i < PauseEntries.Length; i++)
            {
                (string label, bool enabled) = PauseEntries[i];
                bool selected = i == _titleSelection;
                var row = new Rectangle(panel.X + 10, panel.Y + 10 + i * rowHeight, panel.Width - 20, rowHeight - 6);
                _spriteBatch.Draw(_pixelTexture, row, selected ? new Color(60, 46, 30, 245) : new Color(32, 25, 20, 235));
                DrawHudBorder(row, selected ? Color.Gold : new Color(96, 74, 46));

                Color color = !enabled ? new Color(110, 104, 98) : selected ? Color.White : Color.Wheat;
                BitmapFont.Draw(_spriteBatch, _pixelTexture, label, new Point(row.X + 16, row.Y + 9), 3, color);
            }

            if (_menuMessage.Length > 0)
            {
                DrawCenteredText(viewport, _menuMessage, panel.Bottom + 14, 1,
                    _menuMessage.Contains("FAILED", StringComparison.Ordinal) ? Color.IndianRed : Color.LightGreen);
            }

            float pulse = 0.55f + 0.45f * MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds * 3f);
            DrawCenteredText(viewport, "W/S SELECT    ENTER CONFIRM    ESC RESUME", viewport.Height - 44, 1, Color.LightGray * pulse);
        }

        private void DrawTitle(Viewport viewport, GameTime gameTime)
        {
            const string title = "TILEQUEST";
            int titleX = (viewport.Width - title.Length * 6 * 8) / 2;
            BitmapFont.Draw(_spriteBatch, _pixelTexture, title, new Point(titleX + 5, 95 + 5), 8, Color.Black * 0.8f);
            BitmapFont.Draw(_spriteBatch, _pixelTexture, title, new Point(titleX, 95), 8, Color.Gold);
            DrawCenteredText(viewport, "DEFEND THE HEARTH FOR FIVE NIGHTS", 170, 2, Color.Wheat);

            const int rowHeight = 44;
            const int panelWidth = 360;
            (string Label, bool Enabled)[] entries = TitleEntries;
            int panelHeight = entries.Length * rowHeight + 20;
            var panel = new Rectangle((viewport.Width - panelWidth) / 2, 240, panelWidth, panelHeight);
            DrawHudPanel(panel);

            for (int i = 0; i < entries.Length; i++)
            {
                (string label, bool enabled) = entries[i];
                bool selected = i == _titleSelection;
                var row = new Rectangle(panel.X + 10, panel.Y + 10 + i * rowHeight, panel.Width - 20, rowHeight - 6);
                _spriteBatch.Draw(_pixelTexture, row, selected ? new Color(60, 46, 30, 245) : new Color(32, 25, 20, 235));
                DrawHudBorder(row, selected ? Color.Gold : new Color(96, 74, 46));

                Color color = !enabled ? new Color(110, 104, 98) : selected ? Color.White : Color.Wheat;
                BitmapFont.Draw(_spriteBatch, _pixelTexture, label, new Point(row.X + 16, row.Y + 9), 3, color);
                if (!enabled)
                {
                    BitmapFont.Draw(_spriteBatch, _pixelTexture, "NO SAVE", new Point(row.Right - 16 - 7 * 6 * 2, row.Y + 14), 2, color);
                }
            }

            if (_menuMessage.Length > 0)
            {
                DrawCenteredText(viewport, _menuMessage, panel.Bottom + 14, 1,
                    _menuMessage.Contains("FAILED", StringComparison.Ordinal) ? Color.IndianRed : Color.LightGreen);
            }

            float pulse = 0.55f + 0.45f * MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds * 3f);
            DrawCenteredText(viewport, "W/S SELECT    ENTER CONFIRM    ESC QUIT", viewport.Height - 44, 1, Color.LightGray * pulse);
        }

        private void UpdateEndingMenu(KeyboardState keyboard, bool confirm)
        {
            // Ignore input for a moment after the ending opens (see EndingInputDelaySeconds).
            if (_endingInputDelay > 0f)
            {
                return;
            }

            int step = 0;
            if (WasPressed(keyboard, Keys.Up) || WasPressed(keyboard, Keys.W))
            {
                step = -1;
            }
            else if (WasPressed(keyboard, Keys.Down) || WasPressed(keyboard, Keys.S))
            {
                step = 1;
            }

            if (step != 0)
            {
                _endingSelection = (_endingSelection + step + EndingEntries.Length) % EndingEntries.Length;
            }
            else if (confirm && EndingEntries[_endingSelection].Enabled)
            {
                switch (EndingEntries[_endingSelection].Label)
                {
                    case "PLAY AGAIN":
                        ResetGame();
                        _screen = GameScreen.Playing;
                        break;
                    case "RETURN TO TITLE":
                        ResetGame();
                        _titleSelection = 0;
                        _screen = GameScreen.Title;
                        break;
                    case "QUIT":
                        Exit();
                        break;
                }
            }
        }

        private void DrawEndingScreen(Viewport viewport, GameTime gameTime)
        {
            bool victory = _endingKind == EndingKind.Victory;
            if (!victory)
            {
                // A red wash over the usual dim overlay marks the loss.
                _spriteBatch.Draw(_pixelTexture, viewport.Bounds, new Color(90, 12, 12) * 0.25f);
            }

            string title = victory ? "VICTORY" : "DEFEAT";
            Color titleColor = victory ? Color.Gold : new Color(220, 70, 60);
            int titleX = (viewport.Width - title.Length * 6 * 8) / 2;
            BitmapFont.Draw(_spriteBatch, _pixelTexture, title, new Point(titleX + 5, 60 + 5), 8, Color.Black * 0.8f);
            BitmapFont.Draw(_spriteBatch, _pixelTexture, title, new Point(titleX, 60), 8, titleColor);

            string subtitle = victory
                ? "THE HEARTH BURNS ON. THE VILLAGE IS SAFE."
                : $"{_endingReason} ON NIGHT {_nightNumber}";
            DrawCenteredText(viewport, subtitle, 135, 2, Color.Wheat);

            // Run summary panel.
            var summary = new (string Label, string Value)[]
            {
                ("NIGHTS SURVIVED", $"{_dayCount - 1} OF {MaxNights}"),
                ("ENEMIES DEFEATED", _kills.ToString()),
                ("QUESTS COMPLETED", $"{_questLog.CompletedCount} OF {_questLog.Count}"),
                ("HEARTH HP", $"{_hearth.Health} OF {_hearth.MaxHealth}"),
            };
            const int lineHeight = 26;
            const int summaryWidth = 420;
            var summaryPanel = new Rectangle(
                (viewport.Width - summaryWidth) / 2, 170, summaryWidth, summary.Length * lineHeight + 24);
            DrawHudPanel(summaryPanel);
            for (int i = 0; i < summary.Length; i++)
            {
                int y = summaryPanel.Y + 14 + i * lineHeight;
                BitmapFont.Draw(_spriteBatch, _pixelTexture, summary[i].Label,
                    new Point(summaryPanel.X + 20, y), 2, Color.Wheat);
                BitmapFont.Draw(_spriteBatch, _pixelTexture, summary[i].Value,
                    new Point(summaryPanel.Right - 20 - summary[i].Value.Length * 12, y), 2, Color.White);
            }

            // Selection menu, same rows as the pause menu.
            const int rowHeight = 44;
            const int panelWidth = 360;
            int panelHeight = EndingEntries.Length * rowHeight + 20;
            var panel = new Rectangle((viewport.Width - panelWidth) / 2, 330, panelWidth, panelHeight);
            DrawHudPanel(panel);

            for (int i = 0; i < EndingEntries.Length; i++)
            {
                (string label, bool enabled) = EndingEntries[i];
                bool selected = i == _endingSelection;
                var row = new Rectangle(panel.X + 10, panel.Y + 10 + i * rowHeight, panel.Width - 20, rowHeight - 6);
                _spriteBatch.Draw(_pixelTexture, row, selected ? new Color(60, 46, 30, 245) : new Color(32, 25, 20, 235));
                DrawHudBorder(row, selected ? Color.Gold : new Color(96, 74, 46));

                Color color = !enabled ? new Color(110, 104, 98) : selected ? Color.White : Color.Wheat;
                BitmapFont.Draw(_spriteBatch, _pixelTexture, label, new Point(row.X + 16, row.Y + 9), 3, color);
            }

            if (_endingInputDelay <= 0f)
            {
                float pulse = 0.55f + 0.45f * MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds * 3f);
                DrawCenteredText(viewport, "W/S SELECT    ENTER CONFIRM", viewport.Height - 44, 1, Color.LightGray * pulse);
            }
        }

        private void DrawHowToPlay(Viewport viewport)
        {
            var lines = new (string Text, Color Color)[]
            {
                ("GOAL", Color.Gold),
                ("SURVIVE FIVE NIGHTS AND KEEP THE HEARTH ALIVE.", Color.White),
                ("BY DAY GATHER AND CRAFT. BY NIGHT DEFEND THE HEARTH.", Color.White),
                ("", Color.White),
                ("CONTROLS", Color.Gold),
                ("WASD OR ARROWS - MOVE", Color.White),
                ("SPACE OR J - SWING AT NODES AND ENEMIES", Color.White),
                ("E - TALK / USE / OPEN BAG    I - BAG SORTED BY VALUE", Color.White),
                ("Q - TOGGLE QUEST LOG", Color.White),
                ("U - UNDO LAST CRAFT IN THE DAY", Color.White),
                ("Y OR ENTER - CONFIRM    N OR ESC - CANCEL", Color.White),
                ("ESC - PAUSE MENU", Color.White),
                ("PAUSE MENU - SAVE GAME    TITLE - LOAD GAME", Color.White),
                ("", Color.White),
                ("TIPS", Color.Gold),
                ("HIT STUMPS AND BOULDERS FOR WOOD AND STONE.", Color.White),
                ("THE TRADER CRAFTS WEAPONS. THE VENDOR SELLS FOOD FOR GOLD.", Color.White),
                ("TALK TO THE WIZARD AND COMPLETE YOUR QUESTS BEFORE NIGHT 1.", Color.White),
                ("STAND BESIDE THE HEARTH AND PRESS E TO START A NIGHT.", Color.White),
            };

            const int lineHeight = 26;
            int panelWidth = 780;
            int panelHeight = 86 + lines.Length * lineHeight;
            var panel = new Rectangle((viewport.Width - panelWidth) / 2, (viewport.Height - panelHeight) / 2, panelWidth, panelHeight);
            DrawHudPanel(panel);
            BitmapFont.Draw(_spriteBatch, _pixelTexture, "HOW TO PLAY", new Point(panel.X + 20, panel.Y + 16), 3, Color.Wheat);

            for (int i = 0; i < lines.Length; i++)
            {
                BitmapFont.Draw(
                    _spriteBatch, _pixelTexture, lines[i].Text,
                    new Point(panel.X + 20, panel.Y + 58 + i * lineHeight), 2, lines[i].Color);
            }

            BitmapFont.Draw(
                _spriteBatch, _pixelTexture, "ENTER OR ESC - BACK",
                new Point(panel.X + 20, panel.Bottom - 22), 1, Color.Wheat);
        }

        private void DrawNightPrompt()
        {
            const int boxWidth = 340;
            const int boxHeight = 78;
            int x = (_graphics.PreferredBackBufferWidth - boxWidth) / 2;
            int y = _graphics.PreferredBackBufferHeight - boxHeight - 18;

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, boxWidth, boxHeight), new Color(18, 24, 32, 235));
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, boxWidth, 2), Color.Orange);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y + boxHeight - 2, boxWidth, 2), Color.Orange);
            BitmapFont.Draw(_spriteBatch, _pixelTexture, "LIGHT THE HEARTH?", new Point(x + 18, y + 12), 2, Color.White);
            BitmapFont.Draw(
                _spriteBatch,
                _pixelTexture,
                $"NIGHT {_nightNumber + 1} OF {MaxNights}  Y=YES N=NO",
                new Point(x + 18, y + 48),
                2,
                Color.White);
            _spriteBatch.End();
        }

        // Two-row inventory HUD in the top-left corner, left of the day/night
        // panel (which starts at x = 300) and above the event banner. Row 1 =
        // weapons then food, row 2 = resources. Each slot is an icon plus its
        // quantity; only owned items (qty > 0) get a slot, packed from the left.
        // Rows keep their position when empty. Reads only from Inventory.GetItems.
        private void DrawInventoryHud()
        {
            Viewport viewport = GraphicsDevice.Viewport;
            DrawPlayerStatsHud(viewport);

            const int slotWidth = 32;
            const int slotHeight = 32;
            const int slotGap = 3;
            const int originX = 12;
            const int originY = 10;
            const int rowGap = 4;

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            for (int row = 0; row < 2; row++)
            {
                int slotY = originY + row * (slotHeight + rowGap);
                int column = 0;

                IEnumerable<(string type, int qty)> stacks = row == 0
                    ? _inventory.GetItems(ItemCategory.Weapon).Concat(_inventory.GetItems(ItemCategory.Food))
                    : _inventory.GetItems(ItemCategory.Resource);

                foreach ((string type, int qty) in stacks)
                {
                    int x = originX + column * (slotWidth + slotGap);
                    var slotBounds = new Rectangle(x, slotY, slotWidth, slotHeight);
                    _spriteBatch.Draw(_pixelTexture, slotBounds, new Color(32, 25, 20, 235));
                    DrawHudBorder(slotBounds, new Color(156, 119, 69));

                    DrawInventoryItemIcon(type, new Rectangle(x + (slotWidth - 16) / 2, slotY + 4, 16, 16));

                    string quantityText = qty.ToString();
                    int quantityWidth = quantityText.Length * 6;
                    BitmapFont.Draw(
                        _spriteBatch,
                        _pixelTexture,
                        quantityText,
                        new Point(x + (slotWidth - quantityWidth) / 2, slotY + slotHeight - 11),
                        1,
                        Color.White);
                    column++;
                }
            }

            // The best crafted weapon, in the bottom-right corner (clear of the
            // bottom-centre travel / trade prompts and the bottom-left bars).
            const int gearWidth = 220;
            var gearBounds = new Rectangle(viewport.Width - gearWidth - 14, viewport.Height - 24 - 14, gearWidth, 24);
            DrawHudPanel(gearBounds);
            BitmapFont.Draw(
                _spriteBatch, _pixelTexture,
                $"WEAPON {GearLabel(EquippedWeapon())}",
                new Point(gearBounds.X + 8, gearBounds.Y + 8), 1, Color.Wheat);

            _spriteBatch.End();
        }

        private void DrawQuestHud()
        {
            Viewport viewport = GraphicsDevice.Viewport;
            QuestNode? current = _questLog.Current;
            string objective = current == null
                ? "ALL OBJECTIVES COMPLETE!"
                : $"OBJECTIVE: {current.Title.ToUpperInvariant()} ({current.Progress}/{current.RequiredAmount})";
            int objectiveWidth = Math.Min(viewport.Width - 24, objective.Length * 6 + 18);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawHudPanel(new Rectangle(12, 112, objectiveWidth, 18));
            BitmapFont.Draw(_spriteBatch, _pixelTexture, objective, new Point(20, 117), 1, Color.Wheat);

            if (_questPanelOpen)
            {
                const int panelX = 12;
                const int panelY = 138;
                const int panelWidth = 430;
                const int lineHeight = 14;
                int panelHeight = 32 + _questLog.Count * lineHeight;
                DrawHudPanel(new Rectangle(panelX, panelY, panelWidth, panelHeight));
                BitmapFont.Draw(
                    _spriteBatch, _pixelTexture, "QUEST LOG (Q CLOSE)",
                    new Point(panelX + 10, panelY + 7), 1, Color.Gold);

                int lineY = panelY + 22;
                foreach (QuestNode quest in _questLog.Enumerate())
                {
                    string state = quest.IsCompleted
                        ? "DONE"
                        : ReferenceEquals(quest, _questLog.Current) ? "CURRENT" : "UPCOMING";
                    string line = $"{state}: {quest.Title.ToUpperInvariant()} ({quest.Progress}/{quest.RequiredAmount})";
                    Color color = quest.IsCompleted
                        ? new Color(145, 190, 145)
                        : ReferenceEquals(quest, _questLog.Current) ? Color.Wheat : Color.LightGray;
                    BitmapFont.Draw(_spriteBatch, _pixelTexture, line, new Point(panelX + 10, lineY), 1, color);
                    lineY += lineHeight;
                }
            }

            _spriteBatch.End();
        }

        private static string GearLabel(CraftingRecipe? recipe)
        {
            return recipe == null ? "NONE" : $"{recipe.Name.ToUpperInvariant()} +{recipe.Power}";
        }

        private bool TryOpenNearbyChest()
        {
            if (_map.MapType != WorldMapType.Dungeon)
            {
                return false;
            }

            Point playerTile = _player.GridPosition;
            _openChest = _dungeonChests
                .Select(chest => new
                {
                    Chest = chest,
                    Distance = chest.Tiles.Min(tile =>
                        Math.Abs(tile.X - playerTile.X) +
                        Math.Abs(tile.Y - playerTile.Y)),
                })
                .Where(entry => entry.Distance <= 1)
                .OrderBy(entry => entry.Distance)
                .Select(entry => entry.Chest)
                .FirstOrDefault();

            if (_openChest == null)
            {
                return false;
            }

            _chestSelection = 0;
            return true;
        }

        private bool TryInteractWithNearbyWorldObject()
        {
            if (_gameState != GameState.Day || _map.MapType != WorldMapType.Village)
            {
                return false;
            }

            var interactables = new List<InteractableCandidate>
            {
                new(InteractableKind.Trader, VillageCollisionLayout.TraderPosition),
            };
            if (FoodVendorTile is Point vendorTile)
            {
                interactables.Add(new InteractableCandidate(InteractableKind.FoodVendor, vendorTile));
            }
            interactables.Add(new InteractableCandidate(InteractableKind.Hearth, _hearth.Position));
            interactables.Add(new InteractableCandidate(InteractableKind.Wizard, VillageCollisionLayout.WizardPosition));

            Point playerTile = _player.GridPosition;
            int nearbyIndex = LinearSearchUtil.FindIndex(
                interactables,
                interactable => IsAdjacentToInteractable(interactable, playerTile));
            if (nearbyIndex < 0)
            {
                return false;
            }

            switch (interactables[nearbyIndex].Kind)
            {
                case InteractableKind.Trader:
                    _traderOpen = true;
                    _traderSelection = 0;
                    break;
                case InteractableKind.FoodVendor:
                    _foodShopOpen = true;
                    _foodSelection = 0;
                    break;
                case InteractableKind.Hearth:
                    if (_allNightsSurvived)
                    {
                        ShowBanner("THE VILLAGE IS SAFE");
                    }
                    else if (_nightNumber == 0 && !_hasTalkedToWizard)
                    {
                        ShowBanner("TALK TO THE WIZARD FIRST");
                    }
                    else if (_nightNumber == 0 && !_questLog.IsReadyForNightOne)
                    {
                        ShowBanner("COMPLETE THE QUESTS FIRST");
                    }
                    else
                    {
                        _pendingNightStart = true;
                    }
                    break;
                case InteractableKind.Wizard:
                    _hasTalkedToWizard = true;
                    _pendingTravel = new MapTravel(
                        WorldMapType.Forest,
                        _maps[WorldMapType.Forest].SpawnPoint);
                    break;
                default:
                    throw new InvalidOperationException("Unknown interactable kind.");
            }

            return true;
        }

        private bool IsAdjacentToInteractable(InteractableCandidate interactable, Point playerTile)
        {
            if (interactable.Kind == InteractableKind.Hearth)
            {
                return _hearth.IsAdjacentTo(playerTile, includeDiagonals: true);
            }

            return Math.Abs(playerTile.X - interactable.Position.X) +
                   Math.Abs(playerTile.Y - interactable.Position.Y) <= 1;
        }

        private void UpdateChestMenu(KeyboardState keyboard)
        {
            if (WasPressed(keyboard, Keys.Escape))
            {
                _openChest = null;
            }
            else if (WasPressed(keyboard, Keys.E))
            {
                Item? item = _openChest!.TakeItem(_chestSelection);
                if (item != null)
                {
                    _inventory.Add(item.Name, item.Quantity);
                    if (ItemCatalog.CategoryOf(item.Name) == ItemCategory.Resource)
                    {
                        _questLog.OnResourceGathered(item.Name, item.Quantity);
                    }
                }
            }
            else if (WasPressed(keyboard, Keys.A))
            {
                _chestSelection = (_chestSelection + 7) % 8;
            }
            else if (WasPressed(keyboard, Keys.D))
            {
                _chestSelection = (_chestSelection + 1) % 8;
            }
            else if (WasPressed(keyboard, Keys.W))
            {
                _chestSelection = (_chestSelection + 4) % 8;
            }
            else if (WasPressed(keyboard, Keys.S))
            {
                _chestSelection = (_chestSelection + 4) % 8;
            }
        }

        private void DrawChestPopup(TreasureChest chest)
        {
            Viewport viewport = GraphicsDevice.Viewport;
            const int slotWidth = 72;
            const int slotHeight = 60;
            const int slotGap = 6;
            const int panelWidth = 332;
            const int panelHeight = 202;
            int panelX = (viewport.Width - panelWidth) / 2;
            int panelY = (viewport.Height - panelHeight) / 2;

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawHudPanel(new Rectangle(panelX, panelY, panelWidth, panelHeight));
            BitmapFont.Draw(_spriteBatch, _pixelTexture, " CHEST", new Point(panelX + 12, panelY + 10), 2, Color.Wheat);

            for (int slot = 0; slot < 8; slot++)
            {
                int column = slot % 4;
                int row = slot / 4;
                int x = panelX + 12 + column * (slotWidth + slotGap);
                int y = panelY + 34 + row * (slotHeight + slotGap);
                var bounds = new Rectangle(x, y, slotWidth, slotHeight);
                _spriteBatch.Draw(_pixelTexture, bounds, new Color(32, 25, 20, 245));
                DrawHudBorder(bounds, slot == _chestSelection ? Color.Gold : new Color(156, 119, 69));

                if (slot >= chest.Items.Count)
                {
                    continue;
                }

                Item item = chest.Items[slot];
                var icon = new Rectangle(x + (slotWidth - 18) / 2, y + 5, 18, 18);
                DrawInventoryItemIcon(item.Name, icon);
                BitmapFont.Draw(
                    _spriteBatch,
                    _pixelTexture,
                    item.Name.Length > 10 ? item.Name[..10].ToUpperInvariant() : item.Name.ToUpperInvariant(),
                    new Point(x + 5, y + 30),
                    1,
                    Color.White);
                string quantity = item.Quantity.ToString();
                BitmapFont.Draw(
                    _spriteBatch,
                    _pixelTexture,
                    quantity,
                    new Point(x + slotWidth - quantity.Length * 6 - 5, y + slotHeight - 10),
                    1,
                    Color.Wheat);
            }

            BitmapFont.Draw(
                _spriteBatch,
                _pixelTexture,
                "WASD TO NAVIGATE | E TO GET ITEM | ESC TO CLOSE",
                new Point(panelX + 12, panelY + panelHeight - 16),
                1,
                Color.Wheat);
            _spriteBatch.End();
        }

        private void DrawPlayerStatsHud(Viewport viewport)
        {
            const int panelWidth = 164;
            const int panelHeight = 104;
            int x = 16;
            int y = viewport.Height - panelHeight - 14;
            var panel = new Rectangle(x, y, panelWidth, panelHeight);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawHudPanel(panel);
            BitmapFont.Draw(_spriteBatch, _pixelTexture, $"HEALTH {_stats.Health}", new Point(x + 12, y + 7), 2, Color.Wheat);
            var healthMeter = new Rectangle(x + 12, y + 22, 124, 22);
            _spriteBatch.Draw(_pixelTexture, healthMeter, new Color(45, 28, 24));
            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(healthMeter.X + 3, healthMeter.Y + 3, healthMeter.Width - 6, healthMeter.Height - 6),
                new Color(20, 16, 17));
            DrawBarFill(_healthBarTexture, healthMeter, _stats.HealthFraction);
            DrawHudBorder(healthMeter, _stats.HurtFlashRemaining > 0f ? Color.White : new Color(156, 119, 69));

            BitmapFont.Draw(_spriteBatch, _pixelTexture, $"ENERGY {_stats.Energy}", new Point(x + 12, y + 52), 2, Color.Wheat);
            var energyMeter = new Rectangle(x + 12, y + 67, 124, 22);
            _spriteBatch.Draw(_pixelTexture, energyMeter, new Color(28, 32, 43));
            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(energyMeter.X + 3, energyMeter.Y + 3, energyMeter.Width - 6, energyMeter.Height - 6),
                new Color(16, 19, 26));
            DrawBarFill(_energyBarTexture, energyMeter, _stats.EnergyFraction);
            DrawHudBorder(energyMeter, new Color(156, 119, 69));
            _spriteBatch.End();
        }

        // Fills the inside of a meter from the left, in proportion to fraction (0 to 1).
        private void DrawBarFill(Texture2D texture, Rectangle meter, float fraction)
        {
            int innerWidth = meter.Width - 6;
            int filled = (int)Math.Round(innerWidth * Math.Clamp(fraction, 0f, 1f));
            if (filled <= 0)
            {
                return;
            }

            _spriteBatch.Draw(
                texture,
                new Rectangle(meter.X + 3, meter.Y + 3, filled, meter.Height - 6),
                BarFillSource,
                Color.White);
        }

        private void DrawHudPanel(Rectangle bounds)
        {
            _spriteBatch.Draw(_pixelTexture, bounds, new Color(18, 15, 14, 225));
            DrawHudBorder(bounds, new Color(195, 154, 91));
        }

        private void DrawHudBorder(Rectangle bounds, Color color)
        {
            _spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), color);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), color);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), color);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), color);
        }

        private static Color GetInventoryItemColor(string itemName)
        {
            return itemName.ToUpperInvariant() switch
            {
                "WOOD" => new Color(139, 91, 48),
                "STONE" => new Color(133, 137, 137),
                "IRON" => new Color(185, 190, 192),
                "GOLD" => new Color(229, 183, 49),
                "BONE" => new Color(224, 216, 185),
                _ => new Color(117, 95, 143),
            };
        }

        private void DrawInventoryItemIcon(string itemName, Rectangle destination)
        {
            Rectangle? source = itemName.ToUpperInvariant() switch
            {
                "WOOD" => new Rectangle(0, 64, 16, 16),
                "STONE" or "ROCK" => new Rectangle(144, 0, 16, 16),
                "IRON" => new Rectangle(0, 32, 16, 16),
                "GOLD" => new Rectangle(48, 32, 16, 16),
                "BONE" => new Rectangle(64, 272, 16, 16),
                "WOOD SWORD" or "WOODEN CLUB" => new Rectangle(32, 128, 16, 16),
                "STONE SWORD" or "STONE MACE" => new Rectangle(16, 128, 16, 16),
                "IRON SWORD" => new Rectangle(0, 128, 16, 16),
                "GOLD BLADE" => new Rectangle(48, 128, 16, 16),
                "BREAD" => new Rectangle(64, 80, 16, 16),
                "STEW" => new Rectangle(80, 80, 16, 16),
                "ROAST MEAT" => new Rectangle(112, 80, 16, 16),
                "PIE" => new Rectangle(80, 96, 16, 16),
                _ => null,
            };

            if (source is Rectangle iconSource)
            {
                _spriteBatch.Draw(_inventoryIconsTexture, destination, iconSource, Color.White);
                return;
            }

            _spriteBatch.Draw(_pixelTexture, destination, GetInventoryItemColor(itemName));
            BitmapFont.Draw(
                _spriteBatch,
                _pixelTexture,
                itemName[..1].ToUpperInvariant(),
                new Point(destination.X + (destination.Width - 6) / 2, destination.Y + (destination.Height - 8) / 2),
                1,
                Color.White);
        }

        // Iron and gold lying on the floor; walking onto the tile collects
        // them (see OnPlayerEnteredTile).
        private void DrawPickups(GameTime gameTime)
        {
            float time = (float)gameTime.TotalGameTime.TotalSeconds;
            foreach (var (tile, type) in _map.Pickups)
            {
                bool isGold = type == ResourceType.Gold;
                float phase = tile.X * 0.7f + tile.Y * 1.3f;
                int bob = (int)MathF.Round(MathF.Sin(time * 3f + phase) * 2f);
                int centerX = tile.X * TileSize + TileSize / 2;
                int baseY = tile.Y * TileSize + TileSize - 8;
                Rectangle[] variants = isGold ? TileSprites.GoldPickups : TileSprites.IronPickups;
                Rectangle source = variants[(tile.X + tile.Y) & 3];

                _spriteBatch.Draw(_pixelTexture, new Rectangle(centerX - 7, baseY, 14, 3), Color.Black * 0.35f);
                _spriteBatch.Draw(
                    _rocksTexture,
                    new Rectangle(centerX - 8, baseY - 20 + bob, 16, 16),
                    source,
                    Color.White);
            }
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

        // ---- Village Trader: crafting (binary search) and undo (stack) --------

        private bool IsPlayerBesideTrader()
        {
            if (_map.MapType != WorldMapType.Village)
            {
                return false;
            }

            Point player = _player.GridPosition;
            Point trader = VillageCollisionLayout.TraderPosition;
            return Math.Abs(player.X - trader.X) + Math.Abs(player.Y - trader.Y) <= 1;
        }

        // Extra swing damage from the best crafted weapon in the inventory.
        private int WeaponBonus()
        {
            return EquippedWeapon()?.Power ?? 0;
        }

        private void UpdateTraderMenu(KeyboardState keyboard)
        {
            int count = RecipeBook.SortedByCost.Count;
            if (WasPressed(keyboard, Keys.Escape))
            {
                _traderOpen = false;
            }
            else if (WasPressed(keyboard, Keys.W) || WasPressed(keyboard, Keys.Up))
            {
                _traderSelection = (_traderSelection + count - 1) % count;
            }
            else if (WasPressed(keyboard, Keys.S) || WasPressed(keyboard, Keys.Down))
            {
                _traderSelection = (_traderSelection + 1) % count;
            }
            else if (WasPressed(keyboard, Keys.E) || WasPressed(keyboard, Keys.Enter))
            {
                CraftSelectedRecipe();
            }
            else if (WasPressed(keyboard, Keys.U))
            {
                TryUndoLastAction();
            }
        }

        private void CraftSelectedRecipe()
        {
            // The highlighted row's price tag is resolved back to a recipe with
            // a binary search over the cost-sorted RecipeBook.
            int selectedCost = RecipeBook.SortedByCost[_traderSelection].Cost;
            CraftingRecipe? recipe = RecipeBook.FindByCost(selectedCost);
            if (recipe == null)
            {
                return;
            }

            if (Crafting.TryCraft(_inventory, _history, recipe))
            {
                ShowBanner($"CRAFTED {recipe.Name.ToUpperInvariant()}");
                _questLog.OnItemCrafted(recipe.Name);
            }
            else
            {
                ShowBanner("NEED MORE MATERIALS");
            }
        }

        // U: pops the most recent crafting transaction off the stack, removes
        // the gear it made and refunds the materials it spent.
        private void TryUndoLastAction()
        {
            if (_gameState != GameState.Day)
            {
                return;
            }

            PlayerAction? undone = Crafting.UndoLast(_inventory, _history);
            ShowBanner(undone == null ? "NOTHING TO UNDO" : $"UNDID {undone.Description.ToUpperInvariant()}");
        }

        private void DrawTrader(GameTime gameTime)
        {
            Point tile = VillageCollisionLayout.TraderPosition;
            int frame = (int)(gameTime.TotalGameTime.TotalSeconds * TraderFramesPerSecond) % TraderFrameCount;
            var source = new Rectangle(frame * TraderCellSize, 0, TraderCellSize, TraderCellSize);
            int size = (int)Math.Round(TraderCellSize * TraderScale);
            int feetY = (int)Math.Round(TraderFeetRow * TraderScale);
            var destination = new Rectangle(
                tile.X * TileSize + (TileSize - size) / 2,
                (tile.Y + 1) * TileSize - feetY,
                size,
                size);

            // The sheet faces right; turn her toward the player when they are on her left.
            SpriteEffects effects = _player.GridPosition.X < tile.X
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;
            _spriteBatch.Draw(_traderTexture, destination, source, Color.White, 0f, Vector2.Zero, effects, 0f);
        }

        private void DrawTraderMenu()
        {
            Viewport viewport = GraphicsDevice.Viewport;
            IReadOnlyList<CraftingRecipe> recipes = RecipeBook.SortedByCost;
            const int rowHeight = 36;
            const int panelWidth = 520;
            int panelHeight = 46 + recipes.Count * rowHeight + 28;
            int panelX = (viewport.Width - panelWidth) / 2;
            int panelY = (viewport.Height - panelHeight) / 2;

            CraftingRecipe? weapon = EquippedWeapon();
            var dimText = new Color(130, 122, 114);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawHudPanel(new Rectangle(panelX, panelY, panelWidth, panelHeight));
            BitmapFont.Draw(_spriteBatch, _pixelTexture, "TRADER", new Point(panelX + 12, panelY + 10), 2, Color.Wheat);
            BitmapFont.Draw(
                _spriteBatch, _pixelTexture,
                $"WEAPON {GearLabel(weapon)}",
                new Point(panelX + 12, panelY + 30), 1, Color.LightGray);

            for (int i = 0; i < recipes.Count; i++)
            {
                CraftingRecipe recipe = recipes[i];
                bool craftable = Crafting.CanCraft(_inventory, recipe);
                bool selected = i == _traderSelection;
                int rowY = panelY + 46 + i * rowHeight;
                var row = new Rectangle(panelX + 8, rowY, panelWidth - 16, rowHeight - 4);

                _spriteBatch.Draw(_pixelTexture, row, selected ? new Color(60, 46, 30, 245) : new Color(32, 25, 20, 235));
                DrawHudBorder(row, selected ? Color.Gold : new Color(96, 74, 46));

                Color nameColor = craftable ? Color.White : dimText;
                DrawInventoryItemIcon(recipe.Name, new Rectangle(row.X + 8, rowY + 4, 16, 16));
                BitmapFont.Draw(_spriteBatch, _pixelTexture, recipe.Name.ToUpperInvariant(), new Point(row.X + 30, rowY + 4), 2, nameColor);
                string stat = $"+{recipe.Power} DMG";
                BitmapFont.Draw(_spriteBatch, _pixelTexture, stat, new Point(row.X + 166, rowY + 4), 2, nameColor);

                if (recipe == weapon)
                {
                    BitmapFont.Draw(_spriteBatch, _pixelTexture, "EQUIPPED", new Point(row.X + 390, rowY + 8), 1, Color.LimeGreen);
                }

                int cursorX = row.X + 8;
                foreach (ItemStack ingredient in recipe.Ingredients)
                {
                    int have = _inventory.Count(ingredient.Type);
                    string text = $"{ingredient.Type.ToUpperInvariant()} {have}/{ingredient.Quantity}";
                    Color color = have >= ingredient.Quantity ? new Color(140, 220, 140) : new Color(225, 110, 100);
                    BitmapFont.Draw(_spriteBatch, _pixelTexture, text, new Point(cursorX, rowY + 21), 1, color);
                    cursorX += text.Length * 6 + 14;
                }
            }

            BitmapFont.Draw(
                _spriteBatch, _pixelTexture,
                "W/S SELECT   E CRAFT   U UNDO   ESC CLOSE",
                new Point(panelX + 12, panelY + panelHeight - 16), 1, Color.Wheat);
            _spriteBatch.End();
        }

        // ---- Food vendor: sells food for Gold (binary search on price) ------

        private bool IsPlayerBesideFoodVendor()
        {
            if (FoodVendorTile is not Point vendor || _map.MapType != WorldMapType.Village)
            {
                return false;
            }

            Point player = _player.GridPosition;
            return Math.Abs(player.X - vendor.X) + Math.Abs(player.Y - vendor.Y) <= 1;
        }

        private void UpdateFoodShopMenu(KeyboardState keyboard)
        {
            int count = FoodBook.SortedByPrice.Count;
            if (WasPressed(keyboard, Keys.Escape))
            {
                _foodShopOpen = false;
            }
            else if (WasPressed(keyboard, Keys.W) || WasPressed(keyboard, Keys.Up))
            {
                _foodSelection = (_foodSelection + count - 1) % count;
            }
            else if (WasPressed(keyboard, Keys.S) || WasPressed(keyboard, Keys.Down))
            {
                _foodSelection = (_foodSelection + 1) % count;
            }
            else if (WasPressed(keyboard, Keys.E) || WasPressed(keyboard, Keys.Enter))
            {
                BuySelectedFood();
            }
        }

        private void BuySelectedFood()
        {
            // The highlighted row's price tag is resolved back to a food item
            // with a binary search over the price-sorted FoodBook.
            int selectedPrice = FoodBook.SortedByPrice[_foodSelection].Price;
            FoodItem? food = FoodBook.FindByPrice(selectedPrice);
            if (food == null)
            {
                return;
            }

            ShowBanner(FoodShop.TryBuy(_inventory, food)
                ? $"BOUGHT {food.Name.ToUpperInvariant()}"
                : "NOT ENOUGH GOLD");
        }

        private void DrawFoodVendor(GameTime gameTime)
        {
            if (FoodVendorTile is not Point tile)
            {
                return;
            }

            int frame = (int)(gameTime.TotalGameTime.TotalSeconds * TraderFramesPerSecond) % TraderFrameCount;
            var source = new Rectangle(frame * TraderCellSize, 0, TraderCellSize, TraderCellSize);
            int size = (int)Math.Round(TraderCellSize * TraderScale);
            int feetY = (int)Math.Round(TraderFeetRow * TraderScale);
            var destination = new Rectangle(
                tile.X * TileSize + (TileSize - size) / 2,
                (tile.Y + 1) * TileSize - feetY,
                size,
                size);

            // The sheet faces right; turn her toward the player when they are on her left.
            SpriteEffects effects = _player.GridPosition.X < tile.X
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;
            _spriteBatch.Draw(_foodVendorTexture, destination, source, Color.White, 0f, Vector2.Zero, effects, 0f);
        }

        private void DrawFoodShopMenu()
        {
            Viewport viewport = GraphicsDevice.Viewport;
            IReadOnlyList<FoodItem> foods = FoodBook.SortedByPrice;
            const int rowHeight = 36;
            const int panelWidth = 520;
            int panelHeight = 46 + foods.Count * rowHeight + 28;
            int panelX = (viewport.Width - panelWidth) / 2;
            int panelY = (viewport.Height - panelHeight) / 2;
            var dimText = new Color(130, 122, 114);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawHudPanel(new Rectangle(panelX, panelY, panelWidth, panelHeight));
            BitmapFont.Draw(_spriteBatch, _pixelTexture, "FOOD VENDOR", new Point(panelX + 12, panelY + 10), 2, Color.Wheat);
            BitmapFont.Draw(
                _spriteBatch, _pixelTexture,
                $"ENERGY {_stats.Energy}/{PlayerStats.MaxEnergy}   GOLD {_inventory.Count(FoodBook.Currency)}",
                new Point(panelX + 12, panelY + 30), 1, Color.LightGray);

            for (int i = 0; i < foods.Count; i++)
            {
                FoodItem food = foods[i];
                bool affordable = FoodShop.CanAfford(_inventory, food);
                bool selected = i == _foodSelection;
                int rowY = panelY + 46 + i * rowHeight;
                var row = new Rectangle(panelX + 8, rowY, panelWidth - 16, rowHeight - 4);

                _spriteBatch.Draw(_pixelTexture, row, selected ? new Color(60, 46, 30, 245) : new Color(32, 25, 20, 235));
                DrawHudBorder(row, selected ? Color.Gold : new Color(96, 74, 46));

                Color nameColor = affordable ? Color.White : dimText;
                DrawInventoryItemIcon(food.Name, new Rectangle(row.X + 8, rowY + 4, 16, 16));
                BitmapFont.Draw(_spriteBatch, _pixelTexture, food.Name.ToUpperInvariant(), new Point(row.X + 30, rowY + 4), 2, nameColor);
                BitmapFont.Draw(_spriteBatch, _pixelTexture, $"+{food.Energy} ENERGY", new Point(row.X + 190, rowY + 4), 2, nameColor);

                int owned = _inventory.Count(food.Name);
                if (owned > 0)
                {
                    BitmapFont.Draw(_spriteBatch, _pixelTexture, $"OWNED {owned}", new Point(row.X + 390, rowY + 8), 1, Color.Wheat);
                }

                int have = _inventory.Count(FoodBook.Currency);
                Color priceColor = affordable ? new Color(140, 220, 140) : new Color(225, 110, 100);
                BitmapFont.Draw(
                    _spriteBatch, _pixelTexture,
                    $"{FoodBook.Currency.ToUpperInvariant()} {have}/{food.Price}",
                    new Point(row.X + 8, rowY + 21), 1, priceColor);
            }

            BitmapFont.Draw(
                _spriteBatch, _pixelTexture,
                "W/S SELECT   E BUY   ESC CLOSE   EAT IN BAG: I",
                new Point(panelX + 12, panelY + panelHeight - 16), 1, Color.Wheat);
            _spriteBatch.End();
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

        private void DrawVillageObjectLayer(Texture2D texture)
        {
            const float scale = TileSize / 16f;
            var destination = new Rectangle(
                0,
                0,
                _map.Width * TileSize,
                (int)(texture.Height * scale));
            _spriteBatch.Draw(texture, destination, Color.White);
        }

        private void DrawVillageActors(GameTime gameTime)
        {
            var actors = new List<(int BaseY, Action Draw)>();

            foreach (Enemy enemy in _enemies[_map.MapType])
            {
                actors.Add(((int)enemy.PixelPosition.Y + TileSize, () => DrawEnemy(enemy)));
            }

            actors.Add(((_hearth.Position.Y + _hearth.Size) * TileSize, () =>
            {
                DrawHearthStatue(_spriteBatch);
                if (_gameState != GameState.Night)
                {
                    DrawHearthHealthBar();   // at night the overlay draws it above the dimming
                }
            }));

            Point wizardPosition = VillageCollisionLayout.WizardPosition;
            actors.Add(((wizardPosition.Y + 1) * TileSize, DrawWizard));
            actors.Add(((int)_player.PixelPosition.Y + TileSize, () =>
            {
                DrawPlayerSprite();
                if (_player.IsAttacking)
                {
                    DrawSwingEffect();
                }
            }));

            foreach (var actor in actors.OrderBy(actor => actor.BaseY))
            {
                actor.Draw();
            }
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

            foreach (Point pickupTile in _map.Pickups.Keys)
            {
                DrawOutlinedBox(
                    new Rectangle(pickupTile.X * TileSize, pickupTile.Y * TileSize, TileSize, TileSize),
                    Color.Gold);
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

        // Both halves in one call, for DrawVillageStructure. The game itself draws
        // them separately: the platform is floor (under every actor), the statue
        // is depth-sorted with the actors so people can walk behind it.
        private void DrawVillageHearth(VillageStructure hearth, SpriteBatch spriteBatch, GameTime gameTime)
        {
            DrawHearthPlatform(spriteBatch);
            DrawHearthStatue(spriteBatch);
        }

        private int HearthFootprintCenterX()
        {
            return _hearth.Position.X * TileSize + _hearth.Size * TileSize / 2;
        }

        // The statue stands with its base on the bottom edge of the footprint.
        private Rectangle HearthStatueDestination()
        {
            int width = HearthStatueSource.Width * HearthArtScale;
            int height = HearthStatueSource.Height * HearthArtScale;
            int footprintBottom = (_hearth.Position.Y + _hearth.Size) * TileSize;
            return new Rectangle(HearthFootprintCenterX() - width / 2, footprintBottom - height, width, height);
        }

        // Centred under the statue and placed so its feet land on the lower
        // part of the platform's centre slab.
        private Rectangle HearthPlatformDestination()
        {
            int width = HearthPlatformSource.Width * HearthArtScale;
            int height = HearthPlatformSource.Height * HearthArtScale;
            int footprintBottom = (_hearth.Position.Y + _hearth.Size) * TileSize;
            int top = footprintBottom - HearthPlatformSlabBottom * HearthArtScale;
            return new Rectangle(HearthFootprintCenterX() - width / 2, top, width, height);
        }

        private void DrawHearthPlatform(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_txPropsTexture, HearthPlatformDestination(), HearthPlatformSource, Color.White);
        }

        private void DrawHearthStatue(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_txPropsTexture, HearthStatueDestination(), HearthStatueSource, Color.White);
        }

        private void DrawActors(GameTime gameTime)
        {
            var actors = new List<(int BaseY, Action Draw)>(_enemies[_map.MapType].Count + 1);
            foreach (Enemy enemy in _enemies[_map.MapType])
            {
                actors.Add(((int)enemy.PixelPosition.Y + TileSize, () => DrawEnemy(enemy)));
            }

            actors.Add(((int)_player.PixelPosition.Y + TileSize, () =>
            {
                DrawPlayerSprite();
                if (_player.IsAttacking)
                {
                    DrawSwingEffect();
                }
            }));

            foreach (var actor in actors.OrderBy(actor => actor.BaseY))
            {
                actor.Draw();
            }
        }

        private void DrawDepthSortedObjects(GameTime gameTime)
        {
            foreach (IDepthSorted item in _depthSorted)
            {
                item.Draw(_spriteBatch, gameTime);
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

        // Draws one hand cell so its centre lands on `center` (source pixels in the
        // body frame), using the same scale and tint as the body. When the body
        // is flipped to face left the hand's centre and sprite are mirrored too.
        private void DrawEnemyHand(int column, Point center, int frame, int bodyX, int bodyY, float scale, Color tint, bool flip, int offsetY = 0, int handRow = GoblinHandRow, int cellSize = EnemyCellSize, bool applyIdleBob = true)
        {
            var source = new Rectangle(column * HandCellSize, handRow * HandCellSize, HandCellSize, HandCellSize);
            int centerX = flip ? cellSize - center.X : center.X;
            int handX = bodyX + (int)Math.Round((centerX - HandCellSize / 2) * scale);
            int bobY = applyIdleBob ? GoblinBodyBobY[frame % GoblinBodyBobY.Length] : 0;
            int handY = bodyY + (int)Math.Round((center.Y - HandCellSize / 2 + bobY) * scale) + offsetY;
            int handSize = (int)Math.Round(HandCellSize * scale);
            SpriteEffects effects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            _spriteBatch.Draw(
                _enemyHandsTexture,
                new Rectangle(handX, handY, handSize, handSize),
                source,
                tint,
                0f,
                Vector2.Zero,
                effects,
                0f);
        }

        private void DrawSkeletonWeapon(Point handCenter, int cellSize, int bodyX, int bodyY, float bodyScale, Color tint, bool flip)
        {
            float weaponScale = bodyScale * SkeletonWeaponScale;
            int width = (int)Math.Round(SkeletonWeaponSource.Width * weaponScale);
            int height = (int)Math.Round(SkeletonWeaponSource.Height * weaponScale);
            int handX = flip ? cellSize - handCenter.X : handCenter.X;
            int anchorX = bodyX + (int)Math.Round(handX * bodyScale);
            int anchorY = bodyY + (int)Math.Round(handCenter.Y * bodyScale);
            int gripOffset = (int)Math.Round(36f * weaponScale);
            var destination = new Rectangle(anchorX - width / 2, anchorY - gripOffset, width, height);
            SpriteEffects effects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            _spriteBatch.Draw(
                _skeletonWeaponTexture,
                destination,
                SkeletonWeaponSource,
                tint,
                0f,
                Vector2.Zero,
                effects,
                0f);
        }

        private static float SmoothStep01(float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        // Draw-only attack pose from Enemy.AttackProgress. Wind-up: ease back from
        // the target, hands up, tint toward red. Impact: snap forward, hands down.
        // Recovery: ease back to the resting position. Not attacking: no change.
        private static void GetEnemyAttackPose(Enemy enemy, out Vector2 offset, out int handLift, out Color tint)
        {
            offset = Vector2.Zero;
            handLift = 0;
            tint = Color.White;
            if (!enemy.IsAttacking || !enemy.IsAlive)
            {
                return;
            }

            float progress = enemy.AttackProgress;
            Vector2 direction = enemy.AttackDirection;
            float impact = Enemy.AttackImpactProgress;

            if (progress < impact)
            {
                float t = SmoothStep01(progress / impact);
                offset = -direction * (EnemyWindupBackPixels * t);
                handLift = -(int)Math.Round(EnemyWindupHandLiftPixels * t);
                tint = Color.Lerp(Color.White, EnemyWindupTint, t);
            }
            else if (progress < EnemyLungeEndProgress)
            {
                // Snap forward over the first quarter of the impact phase, then hold.
                float t = (progress - impact) / (EnemyLungeEndProgress - impact);
                float snap = SmoothStep01(Math.Min(1f, t * 4f));
                offset = direction * MathHelper.Lerp(-EnemyWindupBackPixels, EnemyLungePixels, snap);
                handLift = -(int)Math.Round(EnemyWindupHandLiftPixels * (1f - snap));
                tint = Color.Lerp(EnemyWindupTint, Color.White, t);
            }
            else
            {
                float t = SmoothStep01((progress - EnemyLungeEndProgress) / (1f - EnemyLungeEndProgress));
                offset = direction * (EnemyLungePixels * (1f - t));
            }
        }

        // A short slash arc on the target side, drawn from small pixel rectangles
        // (same style as DrawSwingEffect). Visible for EnemySlashSeconds from impact.
        private void DrawEnemySlash(Enemy enemy)
        {
            if (!enemy.IsAttacking || !enemy.IsAlive)
            {
                return;
            }

            float slashProgress = (enemy.AttackProgress - Enemy.AttackImpactProgress) * Enemy.AttackSwingSeconds / EnemySlashSeconds;
            if (slashProgress < 0f || slashProgress >= 1f)
            {
                return;
            }

            Vector2 center = enemy.PixelPosition + new Vector2(TileSize / 2f, TileSize / 2f);
            Vector2 direction = enemy.AttackDirection;
            const int segments = 9;
            const float arcHalfWidth = 0.9f;   // radians either side of the attack direction
            float radius = TileSize * 0.65f;
            float reveal = Math.Min(1f, slashProgress * 2.5f);   // the arc sweeps across, then fades
            float fade = 1f - slashProgress;
            Color slashColor = Color.Lerp(Color.White, new Color(255, 150, 130), 0.5f);

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                if (t > reveal)
                {
                    break;
                }

                float angle = MathHelper.Lerp(-arcHalfWidth, arcHalfWidth, t);
                float cos = (float)Math.Cos(angle);
                float sin = (float)Math.Sin(angle);
                Vector2 rotated = new(direction.X * cos - direction.Y * sin, direction.X * sin + direction.Y * cos);
                Vector2 point = center + rotated * radius;
                int size = i == segments / 2 ? 4 : 3;
                var box = new Rectangle((int)Math.Round(point.X) - size / 2, (int)Math.Round(point.Y) - size / 2, size, size);
                _spriteBatch.Draw(_pixelTexture, box, slashColor * (0.85f * fade));
            }
        }

        private void DrawEnemy(Enemy enemy)
        {
            float scale = TileSize / (float)EnemySpriteHeight;
            bool flip = enemy.FacingLeft;
            SpriteEffects effects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Attack pose (draw-only): body offset along the attack direction,
            // goblin hand lift, and a wind-up tint. The hit flash wins over the tint.
            GetEnemyAttackPose(enemy, out Vector2 attackOffset, out int handLift, out Color attackTint);
            int offsetX = (int)Math.Round(attackOffset.X);
            int offsetY = (int)Math.Round(attackOffset.Y);

            Color tint = enemy.HitFlashRemaining > 0f ? new Color(255, 110, 110) : attackTint;
            tint *= enemy.DeathFade;

            // Skeletons and goblins share the same sheet layout and body size.
            // Skeletons carry a bone spear; goblin hands are separate sprites.
            bool skeleton = enemy.Kind == EnemyKind.Skeleton;
            Texture2D idleTexture = skeleton ? _skeletonIdleTextures[(int)enemy.Variant] : _enemyTexture;
            Texture2D runTexture = skeleton ? _skeletonRunTextures[(int)enemy.Variant] : _enemyRunTexture;

            if (enemy.IsRunning && !enemy.IsAttacking)
            {
                // Marching enemies use the run cycle. Its cells are 64x64 with
                // the body low and centred, so it is scaled like the idle sprite
                // (same size) and bottom-centred on the tile.
                int runFrame = (int)(enemy.AnimationTime * enemy.MarchTilesPerSecond * EnemyRunFramesPerTile)
                               % EnemyRunFrameCount;
                var runSource = new Rectangle(runFrame * EnemyRunCellSize, 0, EnemyRunCellSize, EnemyRunCellSize);
                int runSize = (int)Math.Round(EnemyRunCellSize * scale);
                int runX = (int)enemy.PixelPosition.X + (TileSize - runSize) / 2;
                int runY = (int)enemy.PixelPosition.Y + TileSize - runSize;
                _spriteBatch.Draw(
                    runTexture,
                    new Rectangle(runX, runY, runSize, runSize),
                    runSource,
                    tint,
                    0f,
                    Vector2.Zero,
                    effects,
                    0f);

                if (skeleton)
                {
                    DrawEnemyHand(0, SkeletonRunBackHandCenter, runFrame, runX, runY, scale, tint, flip, 0, SkeletonHandRow, EnemyRunCellSize, false);
                    DrawEnemyHand(1, SkeletonRunFrontHandCenter, runFrame, runX, runY, scale, tint, flip, 0, SkeletonHandRow, EnemyRunCellSize, false);
                    DrawSkeletonWeapon(
                        SkeletonRunWeaponHand, EnemyRunCellSize, runX, runY, scale, tint, flip);
                }
            }
            else
            {
                int frame = (int)(enemy.AnimationTime * EnemyFramesPerSecond) % EnemyFrameCount;
                var source = new Rectangle(frame * EnemyCellSize, 0, EnemyCellSize, EnemyCellSize);
                int size = (int)Math.Round(EnemyCellSize * scale);
                int x = (int)enemy.PixelPosition.X + (TileSize - size) / 2 + offsetX;
                int y = (int)enemy.PixelPosition.Y + TileSize - size + offsetY;

                // The back hand is drawn first so the body covers part of it; the
                // front hand goes on top. Facing left swaps which side each is on.
                if (!skeleton)
                {
                    DrawEnemyHand(0, GoblinBackHandCenter, frame, x, y, scale, tint, flip, handLift);
                }
                else
                {
                    DrawEnemyHand(0, SkeletonIdleBackHandCenter, frame, x, y, scale, tint, flip, handLift, SkeletonHandRow);
                }

                _spriteBatch.Draw(
                    idleTexture,
                    new Rectangle(x, y, size, size),
                    source,
                    tint,
                    0f,
                    Vector2.Zero,
                    effects,
                    0f);

                if (!skeleton)
                {
                    DrawEnemyHand(1, GoblinFrontHandCenter, frame, x, y, scale, tint, flip, handLift);
                }
                else
                {
                    DrawEnemyHand(1, SkeletonIdleFrontHandCenter, frame, x, y, scale, tint, flip, handLift, SkeletonHandRow);
                    DrawSkeletonWeapon(SkeletonIdleWeaponHand, EnemyCellSize, x, y, scale, tint, flip);
                }
            }

            DrawEnemySlash(enemy);

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

            Color tint = _stats.HurtFlashRemaining > 0f ? new Color(255, 120, 120) : Color.White;
            _spriteBatch.Draw(_playerTexture, destination, source, tint, 0f, Vector2.Zero, effects, 0f);
        }
    }
}
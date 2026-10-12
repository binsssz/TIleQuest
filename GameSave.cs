using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TileQuest
{
    internal static class GameSaveStore
    {
        private const int CurrentVersion = 1;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static string SavePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TileQuest",
            "savegame.json");

        public static bool Exists => File.Exists(SavePath);

        public static void Save(GameSaveData save)
        {
            string path = SavePath;
            string directory = Path.GetDirectoryName(path)
                ?? throw new InvalidOperationException("The save-game directory could not be determined.");
            Directory.CreateDirectory(directory);

            string temporaryPath = path + ".tmp";
            string json = JsonSerializer.Serialize(save, JsonOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, path, overwrite: true);
        }

        public static GameSaveData Load()
        {
            GameSaveData save = JsonSerializer.Deserialize<GameSaveData>(
                File.ReadAllText(SavePath), JsonOptions)
                ?? throw new InvalidDataException("The save file is empty.");
            if (save.Version != CurrentVersion)
            {
                throw new InvalidDataException(
                    $"Unsupported save version {save.Version}; expected {CurrentVersion}.");
            }

            return save;
        }
    }

    internal sealed class GameSaveData
    {
        public GameSaveData() { }

        public int Version { get; set; } = 1;
        public int ForestSeed { get; set; }
        public WorldMapType Map { get; set; }
        public SavedPoint PlayerPosition { get; set; } = new();
        public FacingDirection PlayerFacing { get; set; }
        public int Health { get; set; }
        public float Energy { get; set; }
        public int HearthHealth { get; set; }
        public GameState GameState { get; set; }
        public int DayCount { get; set; }
        public int NightNumber { get; set; }
        public int Kills { get; set; }
        public bool AllNightsSurvived { get; set; }
        public bool BonusQuestInserted { get; set; }
        public bool HasTalkedToWizard { get; set; }
        public string? EquippedWeaponName { get; set; }
        public int QuestIndex { get; set; }
        public int QuestProgress { get; set; }
        public List<SavedItem> Inventory { get; set; } = new();
        public List<SavedMap> Maps { get; set; } = new();
        public List<SavedChest> Chests { get; set; } = new();
        public List<SavedEnemy> Enemies { get; set; } = new();
        public List<SavedSpawn> PendingSpawns { get; set; } = new();
        public SavedSpawn? PendingWaveSpawn { get; set; }
        public float SpawnTimer { get; set; }
    }

    internal sealed class SavedPoint
    {
        public SavedPoint() { }

        public int X { get; set; }
        public int Y { get; set; }
    }

    internal sealed class SavedItem
    {
        public SavedItem() { }

        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    internal sealed class SavedMap
    {
        public SavedMap() { }

        public WorldMapType Map { get; set; }
        public List<SavedResourceNode> ResourceNodes { get; set; } = new();
        public List<SavedPickup> Pickups { get; set; } = new();
    }

    internal sealed class SavedResourceNode
    {
        public SavedResourceNode() { }

        public SavedPoint Tile { get; set; } = new();
        public int HitsRemaining { get; set; }
    }

    internal sealed class SavedPickup
    {
        public SavedPickup() { }

        public SavedPoint Tile { get; set; } = new();
        public ResourceType Type { get; set; }
    }

    internal sealed class SavedChest
    {
        public SavedChest() { }

        public SavedPoint Tile { get; set; } = new();
        public List<SavedItem> Items { get; set; } = new();
    }

    internal sealed class SavedEnemy
    {
        public SavedEnemy() { }

        public WorldMapType Map { get; set; }
        public SavedPoint Tile { get; set; } = new();
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public bool IsWaveEnemy { get; set; }
        public bool IsHostile { get; set; }
        public EnemyKind Kind { get; set; }
        public SkeletonVariant Variant { get; set; }
        public int HearthDamage { get; set; }
        public float MarchTilesPerSecond { get; set; }
    }

    internal sealed class SavedSpawn
    {
        public SavedSpawn() { }

        public EnemyKind Kind { get; set; }
        public SkeletonVariant Variant { get; set; }
        public float SpawnDelaySeconds { get; set; }
        public int Health { get; set; }
        public float Speed { get; set; }
        public int HearthDamage { get; set; }
    }
}

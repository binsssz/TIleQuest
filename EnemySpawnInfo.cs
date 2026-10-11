namespace TileQuest
{
    // A single entry in a night's spawn queue: what to spawn, how long to
    // wait after the previous spawn before it appears, and its stats for
    // that night (scaled up as nights progress).
    public class EnemySpawnInfo
    {
        public EnemyKind Kind { get; }
        public SkeletonVariant Variant { get; }
        public float SpawnDelaySeconds { get; }
        public int Health { get; }
        public float Speed { get; }
        public int HearthDamage { get; }

        // Readable name for logs and the startup self-check.
        public string EnemyType => Kind == EnemyKind.Skeleton ? $"Skeleton ({Variant})" : "Goblin";

        public EnemySpawnInfo(
            EnemyKind kind,
            SkeletonVariant variant,
            float spawnDelaySeconds,
            int health,
            float speed,
            int hearthDamage)
        {
            Kind = kind;
            Variant = variant;
            SpawnDelaySeconds = spawnDelaySeconds;
            Health = health;
            Speed = speed;
            HearthDamage = hearthDamage;
        }
    }
}

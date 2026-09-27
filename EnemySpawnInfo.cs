namespace TileQuest
{
    // A single entry in a night's spawn queue: what to spawn, how long to
    // wait after the previous spawn before it appears, and its stats for
    // that night (scaled up as nights progress).
    public class EnemySpawnInfo
    {
        public string EnemyType { get; }
        public float SpawnDelaySeconds { get; }
        public int Health { get; }
        public float Speed { get; }

        public EnemySpawnInfo(string enemyType, float spawnDelaySeconds, int health, float speed)
        {
            EnemyType = enemyType;
            SpawnDelaySeconds = spawnDelaySeconds;
            Health = health;
            Speed = speed;
        }
    }
}

using System.Collections.Generic;

namespace TileQuest
{
    // DSA note: Queue<EnemySpawnInfo> piece. A queue is exactly the right
    // structure for a wave spawner — enemies must appear in the order the
    // wave was authored (FIFO), one at a time, respecting each entry's own
    // delay, rather than all at once or in arbitrary order.
    public class WaveSpawner
    {
        private readonly Queue<EnemySpawnInfo> _pendingSpawns = new();
        private float _timeUntilNextSpawn;

        public int RemainingInWave => _pendingSpawns.Count;
        public bool IsWaveComplete => _pendingSpawns.Count == 0;

        // Builds and enqueues the night's wave. Enemy count, health, and
        // speed all scale with nightNumber so later nights are harder.
        public void QueueWave(int nightNumber)
        {
            _pendingSpawns.Clear();

            int enemyCount = 3 + nightNumber * 2;
            int health = 10 + nightNumber * 5;
            float speed = 1.0f + nightNumber * 0.1f;

            for (int i = 0; i < enemyCount; i++)
            {
                _pendingSpawns.Enqueue(new EnemySpawnInfo("Goblin", spawnDelaySeconds: 1.5f, health, speed));
            }

            _timeUntilNextSpawn = 0f;
        }

        // Call every frame during NightPhase. Returns the next enemy to
        // spawn once its delay has elapsed, or null if it's not time yet
        // (or the wave is already empty).
        public EnemySpawnInfo? Update(float deltaSeconds)
        {
            if (_pendingSpawns.Count == 0)
            {
                return null;
            }

            _timeUntilNextSpawn -= deltaSeconds;
            if (_timeUntilNextSpawn > 0f)
            {
                return null;
            }

            var next = _pendingSpawns.Dequeue();
            _timeUntilNextSpawn = next.SpawnDelaySeconds;
            return next;
        }
    }
}

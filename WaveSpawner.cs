using System;
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
        public float TimeUntilNextSpawn => _timeUntilNextSpawn;

        private const float SpawnDelaySeconds = 1.5f;

        // Builds and enqueues the night's wave: a mix of goblins and skeletons,
        // interleaved so both kinds arrive throughout the night. Enemy count,
        // health, and speed all scale with nightNumber so later nights are
        // harder, and skeletons make up a growing share of the wave.
        public void QueueWave(int nightNumber)
        {
            _pendingSpawns.Clear();

            int enemyCount = 3 + nightNumber * 2;
            int skeletonCount = SkeletonCountFor(nightNumber, enemyCount);
            SkeletonVariant[] variants = VariantsForNight(nightNumber);

            int skeletonsQueued = 0;
            for (int i = 0; i < enemyCount; i++)
            {
                // Spreads the skeletons evenly through the queue. The first
                // enemy of every night is a goblin.
                bool nextIsSkeleton = (i + 1) * skeletonCount / enemyCount > skeletonsQueued;
                if (nextIsSkeleton)
                {
                    SkeletonVariant variant = variants[skeletonsQueued % variants.Length];
                    skeletonsQueued++;
                    _pendingSpawns.Enqueue(CreateSkeleton(nightNumber, variant));
                }
                else
                {
                    _pendingSpawns.Enqueue(CreateGoblin(nightNumber));
                }
            }

            _timeUntilNextSpawn = 0f;
        }

        // Skeleton share of the wave: 30% on night 1, +10% each night, capped at
        // 70%. Always at least one skeleton and at least one goblin.
        private static int SkeletonCountFor(int nightNumber, int enemyCount)
        {
            int sharePercent = Math.Min(30 + 10 * (nightNumber - 1), 70);
            int count = (enemyCount * sharePercent + 50) / 100;   // rounded to nearest
            return Math.Clamp(count, 1, enemyCount - 1);
        }

        // New skeleton types join the wave as the nights go on.
        private static SkeletonVariant[] VariantsForNight(int nightNumber)
        {
            return nightNumber switch
            {
                <= 1 => new[] { SkeletonVariant.Base },
                2 => new[] { SkeletonVariant.Base, SkeletonVariant.Warrior },
                3 => new[] { SkeletonVariant.Base, SkeletonVariant.Warrior, SkeletonVariant.Rogue },
                _ => new[] { SkeletonVariant.Base, SkeletonVariant.Warrior, SkeletonVariant.Rogue, SkeletonVariant.Mage },
            };
        }

        private static EnemySpawnInfo CreateGoblin(int nightNumber)
        {
            int health = 10 + nightNumber * 5;
            float speed = 1.0f + nightNumber * 0.1f;
            return new EnemySpawnInfo(
                EnemyKind.Goblin, SkeletonVariant.Base, SpawnDelaySeconds, health, speed, hearthDamage: 3);
        }

        // Skeletons start a little tougher and slower than goblins; the variant
        // then nudges both (see SkeletonStats).
        private static EnemySpawnInfo CreateSkeleton(int nightNumber, SkeletonVariant variant)
        {
            int health = (int)Math.Round((12 + nightNumber * 5) * SkeletonStats.HealthScale(variant));
            float speed = (0.9f + nightNumber * 0.1f) * SkeletonStats.SpeedScale(variant);
            return new EnemySpawnInfo(
                EnemyKind.Skeleton, variant, SpawnDelaySeconds, health, speed, SkeletonStats.HearthDamage(variant));
        }

        // Drops whatever is still queued (a night that ended early).
        public void ClearWave()
        {
            _pendingSpawns.Clear();
            _timeUntilNextSpawn = 0f;
        }

        public IReadOnlyList<EnemySpawnInfo> GetPendingSpawns() => _pendingSpawns.ToArray();

        public void RestoreState(IEnumerable<EnemySpawnInfo> pendingSpawns, float timeUntilNextSpawn)
        {
            if (float.IsNaN(timeUntilNextSpawn) || float.IsInfinity(timeUntilNextSpawn) ||
                timeUntilNextSpawn < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeUntilNextSpawn), timeUntilNextSpawn, "The saved spawn timer is invalid.");
            }

            _pendingSpawns.Clear();
            foreach (EnemySpawnInfo spawn in pendingSpawns)
            {
                _pendingSpawns.Enqueue(spawn);
            }
            _timeUntilNextSpawn = _pendingSpawns.Count == 0 ? 0f : timeUntilNextSpawn;
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

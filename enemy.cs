using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    // A target the player can hit. Grid-locked like the player: it always
    // occupies exactly one tile, and is only moved by being knocked back, which
    // slides it smoothly to the new tile. It does not move or attack on its own
    // yet.
    public class Enemy
    {
        private const float SlideTilesPerSecond = 14f;
        private const float HitFlashSeconds = 0.15f;
        private const float DeathFadeSeconds = 0.35f;

        private readonly int _tileSize;
        private Vector2 _targetPixelPosition;
        private float _deathRemainingSeconds;

        public Point GridPosition { get; private set; }
        public Vector2 PixelPosition { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; }
        public float AnimationTime { get; private set; }

        // Counts down from HitFlashSeconds after each hit; the renderer tints
        // the sprite while it is above zero.
        public float HitFlashRemaining { get; private set; }

        public bool IsAlive => Health > 0;

        // 1 -> 0 while a dead enemy fades out.
        public float DeathFade => IsAlive ? 1f : Math.Clamp(_deathRemainingSeconds / DeathFadeSeconds, 0f, 1f);

        // True once the death fade has finished and the enemy can be dropped.
        public bool ShouldRemove => !IsAlive && _deathRemainingSeconds <= 0f;

        public Enemy(Point gridPosition, int tileSize, int maxHealth)
        {
            GridPosition = gridPosition;
            _tileSize = tileSize;
            PixelPosition = new Vector2(gridPosition.X * tileSize, gridPosition.Y * tileSize);
            _targetPixelPosition = PixelPosition;
            MaxHealth = maxHealth;
            Health = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (!IsAlive)
            {
                return;
            }

            Health = Math.Max(0, Health - amount);
            HitFlashRemaining = HitFlashSeconds;
            if (!IsAlive)
            {
                _deathRemainingSeconds = DeathFadeSeconds;
            }
        }

        public IEnumerable<Item> DropLoot()
        {
            if (Health > 0)
            {
                yield break;
            }

            // Fresh rolls on every kill (a position-based seed made the same
            // tile give the same loot every time).
            Random random = Random.Shared;

            int goblinEars = random.Next(1, 3);
            for (int i = 0; i < goblinEars; i++)
            {
                yield return new Item("Goblin Ear", "Loot", 1, 4);
            }

            if (random.NextDouble() < 0.65)
            {
                yield return new Item("Bone", "Loot", 1, 3);
            }

            if (random.NextDouble() < 0.35)
            {
                yield return new Item("Gold", "Resource", 1, 8);
            }
        }

        // The caller decides whether the destination tile is free; this just
        // moves the enemy there (the pixel position slides over in Update).
        public void KnockBackTo(Point tile)
        {
            GridPosition = tile;
            _targetPixelPosition = new Vector2(tile.X * _tileSize, tile.Y * _tileSize);
        }

        public void Update(float deltaSeconds)
        {
            AnimationTime += deltaSeconds;
            HitFlashRemaining = Math.Max(0f, HitFlashRemaining - deltaSeconds);

            if (!IsAlive)
            {
                _deathRemainingSeconds = Math.Max(0f, _deathRemainingSeconds - deltaSeconds);
            }

            Vector2 toTarget = _targetPixelPosition - PixelPosition;
            float distance = toTarget.Length();
            float step = SlideTilesPerSecond * _tileSize * deltaSeconds;
            if (distance <= 0f)
            {
                return;
            }

            if (step >= distance)
            {
                PixelPosition = _targetPixelPosition;
            }
            else
            {
                toTarget.Normalize();
                PixelPosition += toTarget * step;
            }
        }
    }
}
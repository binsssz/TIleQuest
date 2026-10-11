using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    // A target the player can hit. Grid-locked like the player: it always
    // occupies exactly one tile and slides smoothly between tiles. Training
    // enemies only move when knocked back; wave enemies (IsWaveEnemy) are also
    // marched toward the hearth by the game via MarchTo and strike it with
    // TryStrike.
    public class Enemy
    {
        private const float KnockbackTilesPerSecond = 14f;
        private const float StunSeconds = 0.35f;        // after a hit, so knockback is not walked straight back
        private const float FirstStrikeDelaySeconds = 0.5f;
        private const float HitFlashSeconds = 0.15f;
        private const float DeathFadeSeconds = 0.35f;

        private readonly int _tileSize;
        private Vector2 _targetPixelPosition;
        private float _deathRemainingSeconds;
        private float _slideTilesPerSecond = KnockbackTilesPerSecond;
        private float _stunRemaining;
        private float _attackCooldownRemaining = FirstStrikeDelaySeconds;

        public Point GridPosition { get; private set; }
        public Vector2 PixelPosition { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; }
        public float AnimationTime { get; private set; }

        // Counts down from HitFlashSeconds after each hit; the renderer tints
        // the sprite while it is above zero.
        public float HitFlashRemaining { get; private set; }

        public bool IsAlive => Health > 0;

        // True for goblins spawned by the night wave (not training dummies).
        public bool IsWaveEnemy { get; init; }

        // How fast a wave enemy walks, in tiles per second.
        public float MarchTilesPerSecond { get; init; } = 2f;

        // True while the sprite is still sliding toward its tile.
        public bool IsSliding => PixelPosition != _targetPixelPosition;

        public bool IsStunned => _stunRemaining > 0f;

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
            _stunRemaining = StunSeconds;
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
            _slideTilesPerSecond = KnockbackTilesPerSecond;
        }

        // One step of the march: same grid-locked slide as a knockback, but at
        // the enemy's own walking speed. The caller picks a free neighbour tile.
        public void MarchTo(Point tile)
        {
            GridPosition = tile;
            _targetPixelPosition = new Vector2(tile.X * _tileSize, tile.Y * _tileSize);
            _slideTilesPerSecond = MarchTilesPerSecond;
        }

        // Starts a strike if the cooldown has run out; returns whether it did.
        public bool TryStrike(float cooldownSeconds)
        {
            if (!IsAlive || _attackCooldownRemaining > 0f)
            {
                return false;
            }

            _attackCooldownRemaining = cooldownSeconds;
            return true;
        }

        public void Update(float deltaSeconds)
        {
            AnimationTime += deltaSeconds;
            HitFlashRemaining = Math.Max(0f, HitFlashRemaining - deltaSeconds);
            _stunRemaining = Math.Max(0f, _stunRemaining - deltaSeconds);
            _attackCooldownRemaining = Math.Max(0f, _attackCooldownRemaining - deltaSeconds);

            if (!IsAlive)
            {
                _deathRemainingSeconds = Math.Max(0f, _deathRemainingSeconds - deltaSeconds);
            }

            Vector2 toTarget = _targetPixelPosition - PixelPosition;
            float distance = toTarget.Length();
            float step = _slideTilesPerSecond * _tileSize * deltaSeconds;
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
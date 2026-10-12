using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    // A target the player can hit. Grid-locked like the player: it always
    // occupies exactly one tile and slides smoothly between tiles. Wave enemies
    // (IsWaveEnemy) are marched toward the hearth by the game via MarchTo and
    // attack with a wind-up / impact / recovery swing (TryBeginAttack,
    // ConsumeImpact).
    public class Enemy
    {
        private const float KnockbackTilesPerSecond = 14f;
        private const float StunSeconds = 0.35f;        // after a hit, so knockback is not walked straight back
        private const float FirstStrikeDelaySeconds = 0.5f;

        // One attack swing: AttackProgress runs 0 -> 1 over AttackSwingSeconds.
        // The damage lands once, when progress passes AttackImpactProgress
        // (wind-up before it, recovery after it). The time between swings is
        // the cycle length the game passes to TryBeginAttack.
        public const float AttackSwingSeconds = 0.45f;
        public const float AttackImpactProgress = 0.6f;
        private const float HitFlashSeconds = 0.15f;
        private const float DeathFadeSeconds = 0.35f;
        private const float RunLingerSeconds = 0.12f;   // bridges the one-frame gap between two march steps

        private readonly int _tileSize;
        private Vector2 _targetPixelPosition;
        private float _deathRemainingSeconds;
        private float _slideTilesPerSecond = KnockbackTilesPerSecond;
        private float _stunRemaining;
        private float _attackCooldownRemaining = FirstStrikeDelaySeconds;
        private float _attackElapsed;                   // seconds into the current swing
        private bool _impactPending;                    // impact reached, not yet consumed
        private bool _marching;                         // the last slide was a march step, not a knockback
        private float _runLingerRemaining;

        public Point GridPosition { get; private set; }
        public Vector2 PixelPosition { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; }
        public float AnimationTime { get; private set; }

        // Counts down from HitFlashSeconds after each hit; the renderer tints
        // the sprite while it is above zero.
        public float HitFlashRemaining { get; private set; }

        public bool IsAlive => Health > 0;

        // True for enemies spawned by the night wave.
        public bool IsWaveEnemy { get; init; }

        // True for forest and dungeon enemies that chase and attack the player
        // on their own. Wave enemies attack too.
        public bool IsHostile { get; init; }

        // Which monster this is; the renderer picks sprites from it.
        public EnemyKind Kind { get; init; } = EnemyKind.Goblin;

        // Only meaningful for skeletons: which of the four sprite sets to use.
        public SkeletonVariant Variant { get; init; } = SkeletonVariant.Base;

        // Damage dealt to the hearth by each strike.
        public int HearthDamage { get; init; } = 3;

        // How fast a wave enemy walks, in tiles per second.
        public float MarchTilesPerSecond { get; init; } = 2f;

        // True while the sprite is facing left (the sheets are drawn facing right).
        public bool FacingLeft { get; private set; }

        // True while a wave enemy is walking: sliding between tiles on a march
        // step (not a knockback), with a short linger so the run animation does
        // not flicker to idle between steps. The renderer swaps to the run sheet.
        public bool IsRunning => IsAlive && _marching && (IsSliding || _runLingerRemaining > 0f);

        // True while the sprite is still sliding toward its tile.
        public bool IsSliding => PixelPosition != _targetPixelPosition;

        public bool IsStunned => _stunRemaining > 0f;

        // True from the start of a swing until it ends (or is cancelled).
        public bool IsAttacking { get; private set; }

        // 0 -> 1 across the swing; 0 when not attacking.
        public float AttackProgress =>
            IsAttacking ? Math.Clamp(_attackElapsed / AttackSwingSeconds, 0f, 1f) : 0f;

        // Unit vector toward the target, set when the swing begins (for drawing).
        public Vector2 AttackDirection { get; private set; } = Vector2.UnitX;

        // True if the current (or last) swing was aimed at the player, false for the hearth.
        public bool AttackTargetsPlayer { get; private set; }

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

            // Getting hit interrupts a swing but does not reset the cooldown,
            // so the enemy cannot instantly attack again.
            CancelAttack();
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

            // Skeletons are mostly bone: they almost always drop one and
            // carry a little less gold than a goblin.
            bool skeleton = Kind == EnemyKind.Skeleton;
            double boneChance = skeleton ? 0.9 : 0.65;
            double goldChance = skeleton ? 0.25 : 0.35;

            if (random.NextDouble() < boneChance)
            {
                yield return new Item("Bone", "Loot", 1, 3);
            }

            if (random.NextDouble() < goldChance)
            {
                yield return new Item("Gold", "Resource", 1, 8);
            }
        }

        // The caller decides whether the destination tile is free; this just
        // moves the enemy there (the pixel position slides over in Update).
        public void KnockBackTo(Point tile)
        {
            _marching = false;
            _runLingerRemaining = 0f;
            GridPosition = tile;
            _targetPixelPosition = new Vector2(tile.X * _tileSize, tile.Y * _tileSize);
            _slideTilesPerSecond = KnockbackTilesPerSecond;
        }

        // One step of the march: same grid-locked slide as a knockback, but at
        // the enemy's own walking speed. The caller picks a free neighbour tile.
        public void MarchTo(Point tile)
        {
            FaceToward(tile.X);
            _marching = true;
            _runLingerRemaining = RunLingerSeconds;
            GridPosition = tile;
            _targetPixelPosition = new Vector2(tile.X * _tileSize, tile.Y * _tileSize);
            _slideTilesPerSecond = MarchTilesPerSecond;
        }

        // Turns toward a tile column (may be fractional, e.g. the middle of a
        // 2-wide hearth). Same column: keeps whichever way it was facing.
        public void FaceToward(float tileX)
        {
            if (tileX < GridPosition.X)
            {
                FacingLeft = true;
            }
            else if (tileX > GridPosition.X)
            {
                FacingLeft = false;
            }
        }

        // Starts a swing toward `direction` if this is a living, unstunned wave or
        // hostile enemy that is not already swinging and whose cooldown has run out.
        // The cooldown restarts at the START of the swing, so the time from one
        // swing to the next is exactly cycleSeconds. Returns whether it began.
        public bool TryBeginAttack(float cycleSeconds, Vector2 direction, bool targetsPlayer)
        {
            if ((!IsWaveEnemy && !IsHostile) || !IsAlive || IsStunned || IsAttacking || _attackCooldownRemaining > 0f)
            {
                return false;
            }

            IsAttacking = true;
            _attackElapsed = 0f;
            _impactPending = false;
            AttackTargetsPlayer = targetsPlayer;
            AttackDirection = direction.LengthSquared() > 0f
                ? Vector2.Normalize(direction)
                : Vector2.UnitX;
            _attackCooldownRemaining = cycleSeconds;
            return true;
        }

        // True exactly once per swing: on the first update where AttackProgress
        // passes AttackImpactProgress. The game applies the damage (or misses)
        // when this returns true.
        public bool ConsumeImpact()
        {
            if (!_impactPending)
            {
                return false;
            }

            _impactPending = false;
            return true;
        }

        // Ends the swing without touching the cooldown and drops any impact
        // that has not been consumed yet.
        private void CancelAttack()
        {
            IsAttacking = false;
            _attackElapsed = 0f;
            _impactPending = false;
        }

        public void Update(float deltaSeconds)
        {
            AnimationTime += deltaSeconds;
            HitFlashRemaining = Math.Max(0f, HitFlashRemaining - deltaSeconds);
            _stunRemaining = Math.Max(0f, _stunRemaining - deltaSeconds);
            _attackCooldownRemaining = Math.Max(0f, _attackCooldownRemaining - deltaSeconds);
            _runLingerRemaining = _marching && IsSliding
                ? RunLingerSeconds
                : Math.Max(0f, _runLingerRemaining - deltaSeconds);

            if (!IsAlive)
            {
                _deathRemainingSeconds = Math.Max(0f, _deathRemainingSeconds - deltaSeconds);
                CancelAttack();
            }

            if (IsAttacking)
            {
                float before = _attackElapsed;
                _attackElapsed += deltaSeconds;
                float impactAt = AttackImpactProgress * AttackSwingSeconds;
                if (before < impactAt && _attackElapsed >= impactAt)
                {
                    _impactPending = true;
                }

                if (_attackElapsed >= AttackSwingSeconds)
                {
                    // Swing over. A pending impact stays until the game consumes it.
                    IsAttacking = false;
                    _attackElapsed = 0f;
                }
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
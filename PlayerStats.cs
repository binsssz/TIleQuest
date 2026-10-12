using System;

namespace TileQuest
{
    // The player's health and energy. Free of MonoGame code so DsaDemo can
    // exercise it at startup.
    //
    // Energy works like a hunger bar. It drains slowly as time passes, is
    // never spent on swings, and its only job is to fuel healing: while
    // energy is left and health is missing, health regenerates and burns a
    // little energy for every point restored. At zero energy there is no
    // healing until the player eats (RestoreEnergy) or respawns.
    public class PlayerStats
    {
        public const int MaxHealth = 100;
        public const int MaxEnergy = 100;

        private const float EnergyDrainPerSecond = 0.2f;      // full to empty in about 8 minutes
        private const float HealthRegenPerSecond = 2f;
        private const float EnergyPerHealthPoint = 0.75f;     // healing 1 HP costs this much energy
        private const float HealDelayAfterHitSeconds = 3f;    // no healing right after being hit
        private const float HurtInvulnerableSeconds = 0.6f;   // grace period so goblins cannot stack hits
        private const float HurtFlashSeconds = 0.25f;

        private float _energy = MaxEnergy;
        private float _healthRegenProgress;                   // fractional HP carried between frames
        private float _healDelayRemaining;
        private float _invulnerableRemaining;

        public int Health { get; private set; } = MaxHealth;
        public int Energy => (int)MathF.Ceiling(_energy);
        public float EnergyAmount => _energy;

        public bool IsDead => Health <= 0;
        public bool IsInvulnerable => _invulnerableRemaining > 0f;

        public float HealthFraction => Health / (float)MaxHealth;
        public float EnergyFraction => _energy / MaxEnergy;

        // Counts down after the player is hit; the HUD and sprite use it to flash.
        public float HurtFlashRemaining { get; private set; }

        // Returns true if the hit landed (false while dead or in the grace
        // period after the previous hit).
        public bool TakeDamage(int amount)
        {
            if (IsDead || IsInvulnerable || amount <= 0)
            {
                return false;
            }

            Health = Math.Max(0, Health - amount);
            _invulnerableRemaining = HurtInvulnerableSeconds;
            _healDelayRemaining = HealDelayAfterHitSeconds;
            HurtFlashRemaining = HurtFlashSeconds;
            _healthRegenProgress = 0f;
            return true;
        }

        // Food refills go through here.
        public void RestoreEnergy(int amount)
        {
            if (amount > 0)
            {
                _energy = Math.Min(MaxEnergy, _energy + amount);
            }
        }

        public void RestoreFully()
        {
            Health = MaxHealth;
            _energy = MaxEnergy;
            _healthRegenProgress = 0f;
            _healDelayRemaining = 0f;
            _invulnerableRemaining = 0f;
            HurtFlashRemaining = 0f;
        }

        public void RestoreState(int health, float energy)
        {
            if (health < 0 || health > MaxHealth ||
                float.IsNaN(energy) || float.IsInfinity(energy) ||
                energy < 0f || energy > MaxEnergy)
            {
                throw new ArgumentOutOfRangeException(nameof(health), "Saved health or energy is outside its valid range.");
            }

            Health = health;
            _energy = energy;
            _healthRegenProgress = 0f;
            _healDelayRemaining = 0f;
            _invulnerableRemaining = 0f;
            HurtFlashRemaining = 0f;
        }

        public void Update(float deltaSeconds)
        {
            _invulnerableRemaining = Math.Max(0f, _invulnerableRemaining - deltaSeconds);
            HurtFlashRemaining = Math.Max(0f, HurtFlashRemaining - deltaSeconds);
            _healDelayRemaining = Math.Max(0f, _healDelayRemaining - deltaSeconds);

            if (IsDead)
            {
                return;
            }

            // Hunger: energy slowly runs down on its own.
            _energy = Math.Max(0f, _energy - EnergyDrainPerSecond * deltaSeconds);

            // Healing needs missing health, energy to burn, and a calm moment
            // since the last hit.
            if (Health >= MaxHealth || _energy <= 0f || _healDelayRemaining > 0f)
            {
                _healthRegenProgress = 0f;
                return;
            }

            // Never heal more than the remaining energy can pay for.
            float healed = Math.Min(HealthRegenPerSecond * deltaSeconds, _energy / EnergyPerHealthPoint);
            _energy -= healed * EnergyPerHealthPoint;
            _healthRegenProgress += healed;

            int whole = (int)_healthRegenProgress;
            if (whole > 0)
            {
                Health = Math.Min(MaxHealth, Health + whole);
                _healthRegenProgress -= whole;
            }
        }
    }
}
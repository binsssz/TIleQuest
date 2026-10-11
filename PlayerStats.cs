using System;

namespace TileQuest
{
    // The player's health and energy. Free of MonoGame code so DsaDemo can
    // exercise it at startup.
    //
    // Energy is spent on melee swings and refills on its own after a short
    // pause. Health only regenerates while energy is completely full, so
    // swinging constantly keeps you from healing: you have to stop and let
    // the energy bar refill first.
    public class PlayerStats
    {
        public const int MaxHealth = 100;
        public const int MaxEnergy = 100;
        public const int SwingEnergyCost = 10;

        private const float EnergyRegenDelaySeconds = 1.0f;   // pause after spending before energy refills
        private const float EnergyRegenPerSecond = 15f;
        private const float HealthRegenPerSecond = 3f;        // only while energy is full
        private const float HurtInvulnerableSeconds = 0.6f;   // grace period so goblins cannot stack hits
        private const float HurtFlashSeconds = 0.25f;
        private const float EnergyDeniedFlashSeconds = 0.35f;

        private float _energy = MaxEnergy;
        private float _energyRegenDelayRemaining;
        private float _healthRegenProgress;                   // fractional HP carried between frames
        private float _invulnerableRemaining;

        public int Health { get; private set; } = MaxHealth;
        public int Energy => (int)MathF.Floor(_energy);

        public bool IsDead => Health <= 0;
        public bool IsEnergyFull => _energy >= MaxEnergy;
        public bool IsInvulnerable => _invulnerableRemaining > 0f;

        public float HealthFraction => Health / (float)MaxHealth;
        public float EnergyFraction => _energy / MaxEnergy;

        // Counts down after the player is hit / tries to swing without energy;
        // the HUD uses them to flash.
        public float HurtFlashRemaining { get; private set; }
        public float EnergyDeniedFlashRemaining { get; private set; }

        public bool HasEnergyFor(int cost) => _energy >= cost;

        // Spends energy if there is enough; returns false (changing nothing)
        // otherwise.
        public bool TrySpendEnergy(int cost)
        {
            if (!HasEnergyFor(cost))
            {
                return false;
            }

            _energy -= cost;
            _energyRegenDelayRemaining = EnergyRegenDelaySeconds;
            return true;
        }

        public void FlashEnergyDenied()
        {
            EnergyDeniedFlashRemaining = EnergyDeniedFlashSeconds;
        }

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
            HurtFlashRemaining = HurtFlashSeconds;
            _healthRegenProgress = 0f;
            return true;
        }

        public void RestoreFully()
        {
            Health = MaxHealth;
            _energy = MaxEnergy;
            _energyRegenDelayRemaining = 0f;
            _healthRegenProgress = 0f;
            _invulnerableRemaining = 0f;
            HurtFlashRemaining = 0f;
            EnergyDeniedFlashRemaining = 0f;
        }

        public void Update(float deltaSeconds)
        {
            _invulnerableRemaining = Math.Max(0f, _invulnerableRemaining - deltaSeconds);
            HurtFlashRemaining = Math.Max(0f, HurtFlashRemaining - deltaSeconds);
            EnergyDeniedFlashRemaining = Math.Max(0f, EnergyDeniedFlashRemaining - deltaSeconds);

            // Judged before this frame's energy refill, so health never
            // starts healing on the same frame energy reaches full.
            bool energyWasFull = IsEnergyFull;

            if (_energyRegenDelayRemaining > 0f)
            {
                _energyRegenDelayRemaining = Math.Max(0f, _energyRegenDelayRemaining - deltaSeconds);
            }
            else if (_energy < MaxEnergy)
            {
                _energy = Math.Min(MaxEnergy, _energy + EnergyRegenPerSecond * deltaSeconds);
            }

            if (!IsDead && Health < MaxHealth && energyWasFull)
            {
                _healthRegenProgress += HealthRegenPerSecond * deltaSeconds;
                int whole = (int)_healthRegenProgress;
                if (whole > 0)
                {
                    Health = Math.Min(MaxHealth, Health + whole);
                    _healthRegenProgress -= whole;
                }
            }
        }
    }
}

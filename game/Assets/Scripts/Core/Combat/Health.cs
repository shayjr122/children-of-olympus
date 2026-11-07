using System;

namespace GMR.Core.Combat
{
    /// <summary>
    /// HP of one damageable (AD-16). Readable by anyone; mutated only by DamageSystem (internal API), so no component
    /// can write HP directly. Hero Swap (Epic 8) will copy <see cref="Fraction"/>.
    /// </summary>
    public sealed class Health
    {
        public Health(float max)
        {
            if (!(max > 0f)) throw new ArgumentOutOfRangeException(nameof(max), "Max HP must be > 0.");
            Max = max;
            Current = max;
        }

        public float Current { get; private set; }
        public float Max { get; private set; }
        public float Fraction => Current / Max;
        public bool IsDepleted => Current <= 0f;

        /// <summary>Removes up to <paramref name="amount"/> HP and returns what was actually removed.</summary>
        internal float TakeDamage(float amount)
        {
            if (!(amount > 0f) || Current <= 0f) return 0f;
            float dealt = Math.Min(amount, Current);
            Current -= dealt;
            return dealt;
        }

        /// <summary>Full HP again, optionally with a new maximum (respawn).</summary>
        internal void Restore(float max)
        {
            if (max > 0f) Max = max;
            Current = Max;
        }
    }
}

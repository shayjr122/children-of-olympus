using GMR.Core.Status;
using UnityEngine;

namespace GMR.Core.Combat
{
    /// <summary>
    /// Anything DamageSystem can hit (AD-9): Heroes, enemies, training dummies. HP lives in <see cref="Health"/> and
    /// only DamageSystem changes it; invulnerability is read from the target's InvulnerabilityTracker (AD-16).
    /// </summary>
    public interface IDamageable
    {
        /// <summary>Who this target is (a Player's Hero or an enemy).</summary>
        SourceId Id { get; }

        bool IsAlive { get; }
        Vector2 Position { get; }
        Health Health { get; }

        /// <summary>0 = full knockback, 1 = immovable.</summary>
        float KnockbackResistance { get; }

        /// <summary>Statuses on this target (FR-18), or null if it cannot carry any. Owned by the target, ticked by StatusTicker.</summary>
        StatusContainer Statuses { get; }

        /// <summary>True while an invulnerability window covers scaled time <paramref name="now"/>.</summary>
        bool IsInvulnerable(float now);

        /// <summary>Applies the resolved result (knockback impulse, post-hit windows). Called only by DamageSystem.</summary>
        void ApplyResolved(in DamageResult result);
    }
}

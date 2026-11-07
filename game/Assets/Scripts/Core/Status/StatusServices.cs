using System;
using GMR.Core.Combat;
using GMR.Core.Enemies;
using GMR.Core.Rng;

namespace GMR.Core.Status
{
    /// <summary>
    /// What status behaviours may use (AD-5: passed by hand, one per Encounter): events, the damage pipeline, the enemy
    /// registry (chain targets), the seeded Status RNG stream (tie-breaks, AD-2) and the interaction rule set.
    /// </summary>
    public sealed class StatusServices
    {
        public StatusServices(CombatEvents events, DamageSystem damage, EnemyRegistry enemies, IRng rng)
        {
            Events = events ?? throw new ArgumentNullException(nameof(events));
            Damage = damage ?? throw new ArgumentNullException(nameof(damage));
            Enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            Rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        public CombatEvents Events { get; }
        public DamageSystem Damage { get; }
        public EnemyRegistry Enemies { get; }

        /// <summary>The RunRng "status" stream.</summary>
        public IRng Rng { get; }

        /// <summary>Interaction rules (Rule_WetConductsShock, ...). Set by EncounterBootstrap from its asset; null = none.</summary>
        public StatusRuleSet Rules { get; set; }
    }
}

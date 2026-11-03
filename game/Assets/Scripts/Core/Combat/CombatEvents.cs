using System;
using GMR.Core.Abilities;

namespace GMR.Core.Combat
{
    /// <summary>
    /// Typed, per-Encounter combat events (AD-11). Instance events only (no static events). Each event has a single
    /// owner that raises it (AD-16); feedback, HUD and telemetry only subscribe, and unsubscribe in OnDisable.
    /// Raisers owned by Core systems (DamageSystem, AbilityRunner) are internal, so no other assembly can raise them.
    /// </summary>
    public sealed class CombatEvents
    {
        public event Action<DashStarted> OnDashStarted;
        public event Action<PerfectDodged> OnPerfectDodged;
        public event Action<HitLanded> OnHitLanded;
        public event Action<HitAvoided> OnHitAvoided;
        public event Action<PlayerDamaged> OnPlayerDamaged;
        public event Action<EnemyDied> OnEnemyDied;
        public event Action<AbilityUsed> OnAbilityUsed;

        /// <summary>Owner: DashController.</summary>
        public void RaiseDashStarted(in DashStarted e) => OnDashStarted?.Invoke(e);

        /// <summary>Owner: DashController.</summary>
        public void RaisePerfectDodged(in PerfectDodged e) => OnPerfectDodged?.Invoke(e);

        /// <summary>Owner: DamageSystem.</summary>
        internal void RaiseHitLanded(in HitLanded e) => OnHitLanded?.Invoke(e);

        /// <summary>Owner: DamageSystem.</summary>
        internal void RaiseHitAvoided(in HitAvoided e) => OnHitAvoided?.Invoke(e);

        /// <summary>Owner: DamageSystem.</summary>
        internal void RaisePlayerDamaged(in PlayerDamaged e) => OnPlayerDamaged?.Invoke(e);

        /// <summary>Owner: DamageSystem.</summary>
        internal void RaiseEnemyDied(in EnemyDied e) => OnEnemyDied?.Invoke(e);

        /// <summary>Owner: AbilityRunner.</summary>
        internal void RaiseAbilityUsed(in AbilityUsed e) => OnAbilityUsed?.Invoke(e);
    }
}

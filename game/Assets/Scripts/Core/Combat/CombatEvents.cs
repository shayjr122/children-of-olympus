using System;
using GMR.Core.Abilities;
using GMR.Core.Combo;
using GMR.Core.Rage;
using GMR.Core.Status;

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
        public event Action<UltimateUsed> OnUltimateUsed;
        public event Action<UltimateNotReady> OnUltimateNotReady;
        public event Action<ComboChanged> OnComboChanged;
        public event Action<ComboReset> OnComboReset;
        public event Action<RageChanged> OnRageChanged;
        public event Action<RageFull> OnRageFull;
        public event Action<StatusApplied> OnStatusApplied;
        public event Action<StatusExpired> OnStatusExpired;
        public event Action<ConductedShock> OnConductedShock;
        public event Action<ChainJumped> OnChainJumped;

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

        /// <summary>Owner: AbilityRunner (Ultimate slot).</summary>
        internal void RaiseUltimateUsed(in UltimateUsed e) => OnUltimateUsed?.Invoke(e);

        /// <summary>Owner: UltimateGate.</summary>
        internal void RaiseUltimateNotReady(in UltimateNotReady e) => OnUltimateNotReady?.Invoke(e);

        /// <summary>Owner: ComboCounter.</summary>
        internal void RaiseComboChanged(in ComboChanged e) => OnComboChanged?.Invoke(e);

        /// <summary>Owner: ComboCounter.</summary>
        internal void RaiseComboReset(in ComboReset e) => OnComboReset?.Invoke(e);

        /// <summary>Owner: RageMeter.</summary>
        internal void RaiseRageChanged(in RageChanged e) => OnRageChanged?.Invoke(e);

        /// <summary>Owner: RageMeter.</summary>
        internal void RaiseRageFull(in RageFull e) => OnRageFull?.Invoke(e);

        /// <summary>Owner: StatusContainer.</summary>
        internal void RaiseStatusApplied(in StatusApplied e) => OnStatusApplied?.Invoke(e);

        /// <summary>Owner: StatusContainer.</summary>
        internal void RaiseStatusExpired(in StatusExpired e) => OnStatusExpired?.Invoke(e);

        /// <summary>Owner: StatusContainer (interaction rule fold).</summary>
        internal void RaiseConductedShock(in ConductedShock e) => OnConductedShock?.Invoke(e);

        /// <summary>Owner: ChainRun (ChainLightningBehaviour).</summary>
        internal void RaiseChainJumped(in ChainJumped e) => OnChainJumped?.Invoke(e);
    }
}

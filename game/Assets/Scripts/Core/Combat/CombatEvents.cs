using System;

namespace GMR.Core.Combat
{
    /// <summary>
    /// Typed, per-Encounter combat events (AD-11). Instance events only (no static events). Each event has a single
    /// owner that raises it (AD-16); feedback, HUD and telemetry only subscribe, and unsubscribe in OnDisable.
    /// More events (HitLanded, ComboChanged, ...) arrive with their owners in later epics.
    /// </summary>
    public sealed class CombatEvents
    {
        public event Action<DashStarted> OnDashStarted;
        public event Action<PerfectDodged> OnPerfectDodged;

        /// <summary>Owner: DashController.</summary>
        public void RaiseDashStarted(in DashStarted e) => OnDashStarted?.Invoke(e);

        /// <summary>Owner: DashController.</summary>
        public void RaisePerfectDodged(in PerfectDodged e) => OnPerfectDodged?.Invoke(e);
    }
}

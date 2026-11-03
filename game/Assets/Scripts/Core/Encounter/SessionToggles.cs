namespace GMR.Core.Encounter
{
    /// <summary>
    /// Pause/debug-menu overrides for feature toggles, kept for the session (FR-29). Config assets stay read-only at
    /// runtime (AD-5): a null override means "use the Config default", so Inspector edits still apply until the
    /// menu overrides them.
    /// </summary>
    public sealed class SessionToggles
    {
        public bool? AimAssist;
        public bool? DirectionLock;
        public bool? InputBuffering;
        public bool? PerfectDodgeFlash;
        public bool? PerfectDodgeTimeSlow;
        public bool? DamageNumbers;
        public bool? Hitstop;
        public bool? ScreenShake;

        /// <summary>Effective value: the override if set, else the config default.</summary>
        public static bool Resolve(bool? overrideValue, bool configDefault) => overrideValue ?? configDefault;

        public void ClearOverrides()
        {
            AimAssist = null;
            DirectionLock = null;
            InputBuffering = null;
            PerfectDodgeFlash = null;
            PerfectDodgeTimeSlow = null;
            DamageNumbers = null;
            Hitstop = null;
            ScreenShake = null;
        }
    }
}

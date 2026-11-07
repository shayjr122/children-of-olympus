namespace GMR.Core.Abilities
{
    /// <summary>What an Ultimate press did (FR-17).</summary>
    public enum UltimateGateResult
    {
        /// <summary>RAGE was full and the Ultimate started; RAGE consumed.</summary>
        Fired = 0,

        /// <summary>Hero free but RAGE below 100%: nothing fired, UltimateNotReady raised. The press is spent.</summary>
        NotReady = 1,

        /// <summary>
        /// The Hero is busy (ability running, DASH) or the Ultimate is cooling down: nothing happened. AbilityController
        /// keeps the press queued while the Hero is busy and re-checks RAGE once it is free.
        /// </summary>
        Waiting = 2,

        /// <summary>The Hero has no Ultimate in its slot.</summary>
        NoUltimate = 3,
    }
}

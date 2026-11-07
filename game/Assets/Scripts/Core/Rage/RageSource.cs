namespace GMR.Core.Rage
{
    /// <summary>Where a RAGE gain came from (FR-15). Amounts per source live in RageConfig.</summary>
    public enum RageSource
    {
        /// <summary>A direct hit by the Player's abilities.</summary>
        Hit = 0,

        /// <summary>Damage tagged StatusTick or ChainJump (lower rate).</summary>
        StatusTick = 1,

        /// <summary>The Combo reached a milestone (every N).</summary>
        ComboMilestone = 2,

        /// <summary>A Perfect Dodge (FR-7). HitAvoided grants nothing.</summary>
        PerfectDodge = 3,

        /// <summary>A hit of the Player's Ultimate (default 0, so an Ultimate cannot refill itself).</summary>
        UltimateHit = 4,

        /// <summary>Debug cheat (DevTools "Fill RAGE").</summary>
        Debug = 5,
    }
}

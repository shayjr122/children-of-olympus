using System;

namespace GMR.Core.Combat
{
    /// <summary>What kind of damage a request carries (AD-9). Statuses and modifiers key off these tags (Epic 5).</summary>
    [Flags]
    public enum DamageTags
    {
        None = 0,
        Physical = 1,
        Shock = 2,
        Water = 4,
        StatusTick = 8,
        Ultimate = 16,
        ChainJump = 32,
    }
}

using GMR.Core.Players;

namespace GMR.Core.Combo
{
    /// <summary>A Player's Combo went back to 0 (AD-11). Raised only by that Player's ComboCounter (AD-16).</summary>
    public readonly struct ComboReset
    {
        public readonly PlayerId Player;

        /// <summary>The Combo count just before the reset (telemetry: Combo values at each reset).</summary>
        public readonly int FinalCount;

        public readonly ComboResetReason Reason;

        public ComboReset(PlayerId player, int finalCount, ComboResetReason reason)
        {
            Player = player;
            FinalCount = finalCount;
            Reason = reason;
        }
    }
}

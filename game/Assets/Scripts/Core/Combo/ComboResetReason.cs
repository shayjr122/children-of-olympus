namespace GMR.Core.Combo
{
    /// <summary>Why a Combo went back to 0 (FR-13). Telemetry compares the two (SM-5).</summary>
    public enum ComboResetReason
    {
        /// <summary>No hit landed within the Combo Window.</summary>
        Timeout = 0,

        /// <summary>The Player took damage (avoided hits never reset).</summary>
        Damage = 1,
    }
}

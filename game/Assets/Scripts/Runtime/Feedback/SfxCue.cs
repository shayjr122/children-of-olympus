namespace GMR.Runtime.Feedback
{
    /// <summary>Placeholder SFX hook points (FR-27). Wet/Shock application and chain jumps fire too (M5b).</summary>
    public enum SfxCue
    {
        Hit = 0,
        HeavyHit = 1,
        PlayerHurt = 2,
        Avoided = 3,
        EnemyDied = 4,
        Dash = 5,
        PerfectDodge = 6,
        AbilityUsed = 7,
        UltimateUsed = 8,
        UltimateNotReady = 9,
        RageFull = 10,
        ComboLost = 11,
        Conducted = 12,
        ChainJump = 13,
        StatusApplied = 14,
    }
}

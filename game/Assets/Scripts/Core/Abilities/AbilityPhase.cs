namespace GMR.Core.Abilities
{
    /// <summary>One ability's cycle (FR-9): Ready -> Startup -> Active (hits) -> Recovery -> Ready.</summary>
    public enum AbilityPhase
    {
        Ready = 0,
        Startup = 1,
        Active = 2,
        Recovery = 3,
    }
}

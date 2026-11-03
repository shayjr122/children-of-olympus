using System;

namespace GMR.Core.Abilities
{
    /// <summary>Phase transitions reported by one <see cref="AbilityRunner.Tick"/> (several can happen in one tick).</summary>
    [Flags]
    public enum AbilityStep
    {
        None = 0,
        EnteredActive = 1,
        ExitedActive = 2,
        Finished = 4,
    }
}

using UnityEngine;

namespace GMR.Core.Players
{
    /// <summary>
    /// What Core systems (enemy attacks, targeting, pending hits) may know about a registered Player (AD-4).
    /// Implemented by the Runtime PlayerRoot, which delegates to its current, replaceable HeroRuntime (AD-6).
    /// </summary>
    public interface IPlayerTarget
    {
        PlayerId Id { get; }
        bool IsAlive { get; }
        Vector2 Position { get; }

        /// <summary>Body radius in units, for area overlap tests.</summary>
        float Radius { get; }

        /// <summary>True while any invulnerability window is active at scaled time <paramref name="now"/>.</summary>
        bool IsInvulnerable(float now);
    }
}

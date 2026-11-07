using UnityEngine;

namespace GMR.Core.Combat
{
    /// <summary>
    /// A hit met an invulnerable target (i-frames, post-hit, spawn) and did nothing (AD-11). Raised only by
    /// DamageSystem (AD-16). Grants no RAGE; only PerfectDodged does.
    /// </summary>
    public readonly struct HitAvoided
    {
        public readonly IDamageable Target;
        public readonly SourceId TargetId;
        public readonly SourceId Source;
        public readonly Vector2 Position;
        public readonly int HitId;
        public readonly float At;

        public HitAvoided(IDamageable target, SourceId targetId, SourceId source, Vector2 position, int hitId, float at)
        {
            Target = target;
            TargetId = targetId;
            Source = source;
            Position = position;
            HitId = hitId;
            At = at;
        }
    }
}

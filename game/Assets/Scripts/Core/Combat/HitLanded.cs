using UnityEngine;

namespace GMR.Core.Combat
{
    /// <summary>A hit damaged a non-Player target (AD-11). Raised only by DamageSystem (AD-16).</summary>
    public readonly struct HitLanded
    {
        public readonly IDamageable Target;
        public readonly SourceId TargetId;

        /// <summary>Target position when the hit resolved.</summary>
        public readonly Vector2 Position;

        public readonly DamageResult Result;

        public HitLanded(IDamageable target, SourceId targetId, Vector2 position, in DamageResult result)
        {
            Target = target;
            TargetId = targetId;
            Position = position;
            Result = result;
        }

        public SourceId Source => Result.Source;
    }
}

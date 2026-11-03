using UnityEngine;

namespace GMR.Core.Combat
{
    /// <summary>An enemy (or training dummy) reached 0 HP (AD-11). Raised only by DamageSystem (AD-16).</summary>
    public readonly struct EnemyDied
    {
        public readonly IDamageable Target;
        public readonly SourceId Enemy;

        /// <summary>Who dealt the killing hit.</summary>
        public readonly SourceId Killer;

        public readonly Vector2 Position;
        public readonly float At;

        public EnemyDied(IDamageable target, SourceId enemy, SourceId killer, Vector2 position, float at)
        {
            Target = target;
            Enemy = enemy;
            Killer = killer;
            Position = position;
            At = at;
        }
    }
}

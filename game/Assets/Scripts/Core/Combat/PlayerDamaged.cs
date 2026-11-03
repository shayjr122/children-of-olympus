using GMR.Core.Players;
using UnityEngine;

namespace GMR.Core.Combat
{
    /// <summary>A Player's Hero took damage (AD-11). Raised only by DamageSystem (AD-16).</summary>
    public readonly struct PlayerDamaged
    {
        public readonly PlayerId Player;
        public readonly IDamageable Target;
        public readonly Vector2 Position;
        public readonly DamageResult Result;

        public PlayerDamaged(PlayerId player, IDamageable target, Vector2 position, in DamageResult result)
        {
            Player = player;
            Target = target;
            Position = position;
            Result = result;
        }
    }
}

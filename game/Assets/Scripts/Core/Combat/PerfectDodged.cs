using GMR.Core.Players;
using UnityEngine;

namespace GMR.Core.Combat
{
    /// <summary>A Player's DASH perfectly dodged an attack (FR-7, AD-11). Raised only by DashController (AD-16).</summary>
    public readonly struct PerfectDodged
    {
        public readonly PlayerId Player;
        public readonly int AttackId;
        public readonly Vector2 Position;
        public readonly float At;

        public PerfectDodged(PlayerId player, int attackId, Vector2 position, float at)
        {
            Player = player;
            AttackId = attackId;
            Position = position;
            At = at;
        }
    }
}

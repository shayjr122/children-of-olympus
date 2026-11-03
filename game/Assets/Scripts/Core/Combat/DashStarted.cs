using GMR.Core.Players;
using UnityEngine;

namespace GMR.Core.Combat
{
    /// <summary>A Player's Hero started a DASH at scaled time At (AD-11). Raised only by DashController (AD-16).</summary>
    public readonly struct DashStarted
    {
        public readonly PlayerId Player;
        public readonly Vector2 Direction;
        public readonly Vector2 Position;
        public readonly float At;

        public DashStarted(PlayerId player, Vector2 direction, Vector2 position, float at)
        {
            Player = player;
            Direction = direction;
            Position = position;
            At = at;
        }
    }
}

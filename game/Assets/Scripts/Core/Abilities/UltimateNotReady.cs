using GMR.Core.Players;
using UnityEngine;

namespace GMR.Core.Abilities
{
    /// <summary>
    /// Ultimate pressed with RAGE below 100% (FR-17): nothing fires; feedback plays the "not ready" cue. Raised only by
    /// UltimateGate (AD-16).
    /// </summary>
    public readonly struct UltimateNotReady
    {
        public readonly PlayerId Player;
        public readonly float Rage01;
        public readonly Vector2 Position;
        public readonly float At;

        public UltimateNotReady(PlayerId player, float rage01, Vector2 position, float at)
        {
            Player = player;
            Rage01 = rage01;
            Position = position;
            At = at;
        }
    }
}

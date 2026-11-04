using GMR.Core.Players;
using UnityEngine;

namespace GMR.Core.Abilities
{
    /// <summary>A Player fired their Ultimate (FR-17, AD-11). Raised only by AbilityRunner (AD-16), with AbilityUsed.</summary>
    public readonly struct UltimateUsed
    {
        public readonly PlayerId Player;
        public readonly AbilityDefinition Ability;
        public readonly Vector2 Position;
        public readonly float At;

        public UltimateUsed(PlayerId player, AbilityDefinition ability, Vector2 position, float at)
        {
            Player = player;
            Ability = ability;
            Position = position;
            At = at;
        }
    }
}

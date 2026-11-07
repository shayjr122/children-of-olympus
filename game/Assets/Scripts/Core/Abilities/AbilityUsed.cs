using GMR.Core.Players;
using UnityEngine;

namespace GMR.Core.Abilities
{
    /// <summary>A Player started an ability (AD-11). Raised only by AbilityRunner (AD-16).</summary>
    public readonly struct AbilityUsed
    {
        public readonly PlayerId Player;
        public readonly AbilitySlot Slot;
        public readonly AbilityDefinition Ability;
        public readonly AttackType AttackType;
        public readonly Vector2 Position;
        public readonly Vector2 Direction;
        public readonly float At;

        public AbilityUsed(PlayerId player, AbilitySlot slot, AbilityDefinition ability, Vector2 position, Vector2 direction, float at)
        {
            Player = player;
            Slot = slot;
            Ability = ability;
            AttackType = ability != null ? ability.AttackType : default;
            Position = position;
            Direction = direction;
            At = at;
        }
    }
}

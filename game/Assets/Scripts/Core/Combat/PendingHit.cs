using UnityEngine;

namespace GMR.Core.Combat
{
    /// <summary>An enemy hit announced by its telegraph (AD-12): when it lands and the circular area it covers.</summary>
    public readonly struct PendingHit
    {
        public readonly int AttackId;
        public readonly float LandTime;
        public readonly Vector2 Center;
        public readonly float Radius;

        public PendingHit(int attackId, float landTime, Vector2 center, float radius)
        {
            AttackId = attackId;
            LandTime = landTime;
            Center = center;
            Radius = radius;
        }

        /// <summary>True if a circle at <paramref name="position"/> with <paramref name="radius"/> touches the area.</summary>
        public bool Overlaps(Vector2 position, float radius)
        {
            float reach = Radius + radius;
            return (position - Center).sqrMagnitude <= reach * reach;
        }
    }
}

using UnityEngine;

namespace GMR.Runtime.Abilities
{
    /// <summary>Where the caster is this frame: center and body radius.</summary>
    public readonly struct HitOrigin
    {
        public readonly Vector2 Position;
        public readonly float Radius;

        public HitOrigin(Vector2 position, float radius)
        {
            Position = position;
            Radius = radius;
        }
    }
}

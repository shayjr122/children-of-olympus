using System.Collections.Generic;
using UnityEngine;

namespace GMR.Runtime.Abilities
{
    /// <summary>
    /// Thrust (FR-9): a box in front of the Hero along the fire direction, starting ForwardOffset from the Hero's
    /// center, Range long and Width wide. Physics2D.OverlapBox with the target layer mask.
    /// </summary>
    public sealed class BoxHitResolver : IHitResolver
    {
        readonly List<Collider2D> _results = new List<Collider2D>(16);
        ContactFilter2D _filter;

        public BoxHitResolver(LayerMask targets)
        {
            _filter = new ContactFilter2D { useTriggers = true };
            _filter.SetLayerMask(targets);
        }

        public void Begin(AbilityCast cast, HitOrigin origin, float now) => Tick(cast, origin, now);

        public void Tick(AbilityCast cast, HitOrigin origin, float now)
        {
            GetBox(cast, origin, out Vector2 center, out Vector2 size, out float angle);
            Physics2D.OverlapBox(center, size, angle, _filter, _results);
            for (int i = 0; i < _results.Count; i++)
                cast.TryHit(DamageableLookup.Find(_results[i]), cast.Direction, now);
        }

        /// <summary>The box for <paramref name="cast"/> at <paramref name="origin"/> (also used by the placeholder visual).</summary>
        public static void GetBox(AbilityCast cast, HitOrigin origin, out Vector2 center, out Vector2 size, out float angleDegrees)
        {
            var def = cast.Definition;
            Vector2 dir = cast.Direction;
            float length = Mathf.Max(0.01f, def.Range);
            center = origin.Position + dir * (def.ForwardOffset + length * 0.5f);
            size = new Vector2(length, Mathf.Max(0.01f, def.Width));
            angleDegrees = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }
    }
}

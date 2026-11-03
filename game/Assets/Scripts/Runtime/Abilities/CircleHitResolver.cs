using System.Collections.Generic;
using UnityEngine;

namespace GMR.Runtime.Abilities
{
    /// <summary>
    /// Spin (FR-9): every target within Range of the Hero's center. Knockback pushes outward from the Hero.
    /// Physics2D.OverlapCircle with the target layer mask.
    /// </summary>
    public sealed class CircleHitResolver : IHitResolver
    {
        readonly List<Collider2D> _results = new List<Collider2D>(16);
        ContactFilter2D _filter;

        public CircleHitResolver(LayerMask targets)
        {
            _filter = new ContactFilter2D { useTriggers = true };
            _filter.SetLayerMask(targets);
        }

        public void Begin(AbilityCast cast, HitOrigin origin, float now) => Tick(cast, origin, now);

        public void Tick(AbilityCast cast, HitOrigin origin, float now)
        {
            float radius = Mathf.Max(0.01f, cast.Definition.Range);
            Physics2D.OverlapCircle(origin.Position, radius, _filter, _results);
            for (int i = 0; i < _results.Count; i++)
            {
                var target = DamageableLookup.Find(_results[i]);
                if (target == null) continue;
                Vector2 outward = target.Position - origin.Position;
                cast.TryHit(target, outward.sqrMagnitude > 1e-6f ? outward : cast.Direction, now);
            }
        }
    }
}

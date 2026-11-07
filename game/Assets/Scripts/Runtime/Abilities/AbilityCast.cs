using System.Collections.Generic;
using GMR.Core.Abilities;
using GMR.Core.Combat;
using UnityEngine;

namespace GMR.Runtime.Abilities
{
    /// <summary>
    /// One activation of one ability: its hit id, direction and the targets it already hit (each target at most once
    /// per activation). Hit resolvers turn overlaps into DamageRequests through <see cref="TryHit"/>.
    /// </summary>
    public sealed class AbilityCast
    {
        readonly HashSet<IDamageable> _hit = new HashSet<IDamageable>();
        readonly DamageSystem _damage;

        public AbilityCast(AbilityRunner runner, SourceId source, DamageSystem damage)
        {
            Runner = runner;
            Source = source;
            _damage = damage;
        }

        public AbilityRunner Runner { get; }
        public AbilityDefinition Definition => Runner.Definition;
        public SourceId Source { get; }
        public DamageSystem Damage => _damage;
        public int HitId { get; private set; }
        public Vector2 Direction { get; private set; } = Vector2.right;
        public int HitCount => _hit.Count;

        /// <summary>Starts a new activation (Active phase entered).</summary>
        public void Begin(int hitId)
        {
            HitId = hitId;
            Direction = Runner.Direction;
            _hit.Clear();
        }

        /// <summary>
        /// Damages <paramref name="target"/> unless it is null, dead or already hit by this activation.
        /// Knockback = <paramref name="knockbackDirection"/> x the ability's knockback.
        /// </summary>
        public bool TryHit(IDamageable target, Vector2 knockbackDirection, float now)
        {
            if (target == null || !target.IsAlive || !_hit.Add(target)) return false;
            var def = Definition;
            Vector2 knockback = knockbackDirection.sqrMagnitude > 1e-8f ? knockbackDirection.normalized * def.Knockback : Vector2.zero;
            _damage.Apply(target, new DamageRequest(Source, def.Damage, def.DamageTags, knockback, def.StatusPayload, HitId, def.HitWeight), now);
            return true;
        }
    }
}

using UnityEngine;

namespace GMR.Core.Combat
{
    /// <summary>
    /// What DamageSystem resolved for one request (AD-9, AD-16). The knockback impulse is final (after resistance);
    /// only the target's ApplyResolved applies it.
    /// </summary>
    public readonly struct DamageResult
    {
        public readonly float Dealt;
        public readonly bool Killed;
        public readonly bool Avoided;
        public readonly Vector2 KnockbackImpulse;
        public readonly DamageTags Tags;
        public readonly SourceId Source;
        public readonly HitWeight Weight;
        public readonly int HitId;

        /// <summary>Scaled time the request resolved at.</summary>
        public readonly float At;

        public DamageResult(float dealt, bool killed, bool avoided, Vector2 knockbackImpulse, DamageTags tags,
            SourceId source, HitWeight weight, int hitId, float at)
        {
            Dealt = dealt;
            Killed = killed;
            Avoided = avoided;
            KnockbackImpulse = knockbackImpulse;
            Tags = tags;
            Source = source;
            Weight = weight;
            HitId = hitId;
            At = at;
        }
    }
}

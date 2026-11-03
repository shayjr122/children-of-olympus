using UnityEngine;

namespace GMR.Core.Combat
{
    /// <summary>One request to damage one target (AD-9). Built by hit resolvers, projectiles and enemy attacks.</summary>
    public readonly struct DamageRequest
    {
        public readonly SourceId Source;
        public readonly float Amount;
        public readonly DamageTags Tags;

        /// <summary>Knockback before the target's resistance (direction x strength, applied as an impulse).</summary>
        public readonly Vector2 Knockback;

        public readonly StatusPayload Status;

        /// <summary>Id of the attack instance (one ability activation, one projectile, one slam).</summary>
        public readonly int HitId;

        public readonly HitWeight Weight;

        public DamageRequest(SourceId source, float amount, DamageTags tags, Vector2 knockback, StatusPayload status,
            int hitId, HitWeight weight = HitWeight.Light)
        {
            Source = source;
            Amount = amount;
            Tags = tags;
            Knockback = knockback;
            Status = status;
            HitId = hitId;
            Weight = weight;
        }
    }
}

using GMR.Core.Combat;
using UnityEngine;

namespace GMR.Runtime.Abilities
{
    /// <summary>Finds the IDamageable a collider belongs to (its Rigidbody2D's GameObject, else a parent).</summary>
    public static class DamageableLookup
    {
        public static IDamageable Find(Collider2D collider)
        {
            if (collider == null) return null;
            var body = collider.attachedRigidbody;
            if (body != null && body.TryGetComponent(out IDamageable onBody)) return onBody;
            var inParent = collider.GetComponentInParent<IDamageable>();
            return inParent is Object unityObject && unityObject == null ? null : inParent;
        }
    }
}

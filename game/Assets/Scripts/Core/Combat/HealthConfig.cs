using UnityEngine;

namespace GMR.Core.Combat
{
    /// <summary>HP tuning of a damageable (FR-27, NFR-4): Heroes and training dummies each reference one.</summary>
    [CreateAssetMenu(menuName = "GMR/Config/Health Config", fileName = "Config_Health_")]
    public sealed class HealthConfig : ScriptableObject
    {
        [SerializeField, Min(1f), Tooltip("Maximum HP. Applied when the damageable is (re)built or respawns.")]
        float _maxHp = 100f;

        [SerializeField, Range(0f, 1f), Tooltip("Knockback resistance: impulse = knockback x (1 - resistance). Read live.")]
        float _knockbackResistance;

        [SerializeField, Min(0f), Tooltip("Invulnerability after taking a hit, seconds (FR-27; Player default 0.5). Read live.")]
        float _postHitInvulnerability;

        public float MaxHp { get => _maxHp; internal set => _maxHp = value; }
        public float KnockbackResistance { get => _knockbackResistance; internal set => _knockbackResistance = value; }
        public float PostHitInvulnerability { get => _postHitInvulnerability; internal set => _postHitInvulnerability = value; }
    }
}

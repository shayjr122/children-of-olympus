using UnityEngine;

namespace GMR.DevTools
{
    /// <summary>Tuning for the DummyAttacker (Story 2.4). Debug-only content, read live.</summary>
    [CreateAssetMenu(menuName = "GMR/DevTools/Dummy Attacker Config", fileName = "Config_DummyAttacker")]
    public sealed class DummyAttackerConfig : ScriptableObject
    {
        [SerializeField, Min(0.1f), Tooltip("Seconds between the end of one attack and the next telegraph.")]
        float _restTime = 1.6f;

        [SerializeField, Min(0.05f), Tooltip("Telegraph (wind-up) length; the hit lands when it ends. GDD min telegraph 0.4 s.")]
        float _telegraphTime = 0.8f;

        [SerializeField, Min(0.1f), Tooltip("Radius of the circular hit area in units.")]
        float _radius = 1.6f;

        [SerializeField, Min(0f), Tooltip("How long the area flashes after the hit lands.")]
        float _impactFlashTime = 0.15f;

        [SerializeField, Min(0f), Tooltip("Damage dealt to a Player hit by the slam (through DamageSystem).")]
        float _damage = 10f;

        [SerializeField, Min(0f), Tooltip("Knockback strength pushing the Player out of the slam.")]
        float _knockback = 4f;

        public float RestTime => _restTime;
        public float Damage => _damage;
        public float Knockback => _knockback;
        public float TelegraphTime => _telegraphTime;
        public float Radius => _radius;
        public float ImpactFlashTime => _impactFlashTime;
    }
}

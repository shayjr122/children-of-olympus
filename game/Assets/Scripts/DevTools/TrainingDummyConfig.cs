using UnityEngine;

namespace GMR.DevTools
{
    /// <summary>Tuning for training dummies (Story 3.1). Debug-only content, read live. HP lives in the HealthConfig.</summary>
    [CreateAssetMenu(menuName = "GMR/DevTools/Training Dummy Config", fileName = "Config_TrainingDummy")]
    public sealed class TrainingDummyConfig : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip("Seconds a broken dummy stays down before it respawns at full HP.")]
        float _respawnDelay = 1.5f;

        [SerializeField, Min(0f), Tooltip("Seconds after the last hit before the dummy walks back to its spot.")]
        float _returnDelay = 1f;

        [SerializeField, Min(0f), Tooltip("Walk-back speed in units/s (0 = stays where knocked).")]
        float _returnSpeed = 2f;

        public float RespawnDelay { get => _respawnDelay; internal set => _respawnDelay = value; }
        public float ReturnDelay { get => _returnDelay; internal set => _returnDelay = value; }
        public float ReturnSpeed { get => _returnSpeed; internal set => _returnSpeed = value; }
    }
}

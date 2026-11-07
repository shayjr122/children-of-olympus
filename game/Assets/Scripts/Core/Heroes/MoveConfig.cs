using UnityEngine;

namespace GMR.Core.Heroes
{
    /// <summary>
    /// Per-Hero movement tuning (FR-1, NFR-4). Read live every physics step, so Play Mode edits apply immediately.
    /// </summary>
    [CreateAssetMenu(menuName = "GMR/Config/Move Config", fileName = "Config_Move_")]
    public sealed class MoveConfig : ScriptableObject
    {
        [SerializeField, Min(0.01f), Tooltip("Max speed in units/s at full stick (1 unit = 32 px).")]
        float _maxSpeed = 5.5f;

        [SerializeField, Min(0f), Tooltip("Seconds from rest to max speed at full deflection. 0 = instant.")]
        float _accelerationTime = 0.06f;

        [SerializeField, Min(0f), Tooltip("Seconds from max speed to rest after releasing the stick. 0 = instant.")]
        float _decelerationTime = 0.06f;

        [SerializeField, Range(0f, 0.95f), Tooltip("Radial stick dead-zone. Deflection at or below it produces no movement.")]
        float _deadZone = 0.2f;

        public float MaxSpeed { get => _maxSpeed; internal set => _maxSpeed = value; }
        public float AccelerationTime { get => _accelerationTime; internal set => _accelerationTime = value; }
        public float DecelerationTime { get => _decelerationTime; internal set => _decelerationTime = value; }
        public float DeadZone { get => _deadZone; internal set => _deadZone = value; }
    }
}

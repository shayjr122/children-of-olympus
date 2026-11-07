using UnityEngine;

namespace GMR.Core.Controls
{
    /// <summary>Aim tuning (FR-2..FR-5, AD-7). Toggles here are defaults; the pause menu can override them per session.</summary>
    [CreateAssetMenu(menuName = "GMR/Config/Aim Config", fileName = "Config_Aim")]
    public sealed class AimConfig : ScriptableObject
    {
        [SerializeField, Tooltip("Only MovementAim is implemented (RightStick = FR-5 hook, not built).")]
        AimMode _mode = AimMode.MovementAim;

        [SerializeField, Tooltip("Aim Assist default: snap to the nearest living enemy in the cone when firing.")]
        bool _assistEnabled = true;

        [SerializeField, Range(0f, 90f), Tooltip("Aim Assist cone half-angle in degrees around the Aim Direction.")]
        float _assistHalfAngle = 25f;

        [SerializeField, Min(0f), Tooltip("Aim Assist range in units.")]
        float _assistRange = 8f;

        [SerializeField, Tooltip("Direction Lock default: holding the button freezes the Aim Direction.")]
        bool _directionLockEnabled = true;

        public AimMode Mode { get => _mode; internal set => _mode = value; }
        public bool AssistEnabled { get => _assistEnabled; internal set => _assistEnabled = value; }
        public float AssistHalfAngle { get => _assistHalfAngle; internal set => _assistHalfAngle = value; }
        public float AssistRange { get => _assistRange; internal set => _assistRange = value; }
        public bool DirectionLockEnabled { get => _directionLockEnabled; internal set => _directionLockEnabled = value; }
    }
}

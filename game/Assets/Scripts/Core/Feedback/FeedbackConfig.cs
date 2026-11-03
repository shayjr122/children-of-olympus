using UnityEngine;

namespace GMR.Core.Feedback
{
    /// <summary>
    /// Feedback tuning (FR-7 Perfect Dodge feedback; grows into the full FR-27 FeedbackConfig in Story 3.4). Every
    /// effect has an on/off default that the pause menu can override per session.
    /// </summary>
    [CreateAssetMenu(menuName = "GMR/Config/Feedback Config", fileName = "Config_Feedback")]
    public sealed class FeedbackConfig : ScriptableObject
    {
        [Header("Perfect Dodge flash")]
        [SerializeField] bool _perfectDodgeFlash = true;

        [SerializeField, Min(0f), Tooltip("Screen flash fade time in real seconds.")]
        float _flashDuration = 0.15f;

        [SerializeField, Range(0f, 1f)] float _flashMaxAlpha = 0.45f;

        [Header("Perfect Dodge time-slow")]
        [SerializeField] bool _perfectDodgeTimeSlow = true;

        [SerializeField, Range(0f, 1f), Tooltip("Time scale while slowed (0.3 = 30% speed).")]
        float _timeSlowScale = 0.3f;

        [SerializeField, Min(0f), Tooltip("Slow-mo length in real seconds.")]
        float _timeSlowDuration = 0.15f;

        [Header("Popups (PERFECT / AVOIDED / HIT)")]
        [SerializeField, Min(0.05f), Tooltip("Popup lifetime in real seconds.")]
        float _popupDuration = 0.7f;

        [SerializeField, Min(0f), Tooltip("How far a popup rises over its lifetime, in units.")]
        float _popupRise = 0.8f;

        public bool PerfectDodgeFlash { get => _perfectDodgeFlash; internal set => _perfectDodgeFlash = value; }
        public float FlashDuration { get => _flashDuration; internal set => _flashDuration = value; }
        public float FlashMaxAlpha { get => _flashMaxAlpha; internal set => _flashMaxAlpha = value; }
        public bool PerfectDodgeTimeSlow { get => _perfectDodgeTimeSlow; internal set => _perfectDodgeTimeSlow = value; }
        public float TimeSlowScale { get => _timeSlowScale; internal set => _timeSlowScale = value; }
        public float TimeSlowDuration { get => _timeSlowDuration; internal set => _timeSlowDuration = value; }
        public float PopupDuration { get => _popupDuration; internal set => _popupDuration = value; }
        public float PopupRise { get => _popupRise; internal set => _popupRise = value; }
    }
}

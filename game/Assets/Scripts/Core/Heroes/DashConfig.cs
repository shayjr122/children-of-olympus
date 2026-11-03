using UnityEngine;

namespace GMR.Core.Heroes
{
    /// <summary>
    /// Per-Hero DASH tuning (FR-6, FR-7, NFR-4). Durations in scaled seconds. Values are snapshotted when a DASH
    /// starts, so edits apply from the next DASH.
    /// </summary>
    [CreateAssetMenu(menuName = "GMR/Config/Dash Config", fileName = "Config_Dash_")]
    public sealed class DashConfig : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip("DASH travel distance in units (stops early at walls).")]
        float _distance = 3f;

        [SerializeField, Min(0.01f), Tooltip("DASH travel time in seconds.")]
        float _duration = 0.15f;

        [SerializeField, Min(0f), Tooltip("Invulnerability from DASH start, seconds. Only the start of the DASH is safe.")]
        float _iFrameDuration = 0.10f;

        [SerializeField, Min(0f), Tooltip("Dash Recovery after the DASH ends: no i-frames, no new DASH.")]
        float _recoveryDuration = 0.12f;

        [SerializeField, Min(0f), Tooltip("Perfect Dodge Window: a DASH starting at most this long before a hit lands.")]
        float _perfectWindow = 0.08f;

        public float Distance { get => _distance; internal set => _distance = value; }
        public float Duration { get => _duration; internal set => _duration = value; }
        public float IFrameDuration { get => _iFrameDuration; internal set => _iFrameDuration = value; }
        public float RecoveryDuration { get => _recoveryDuration; internal set => _recoveryDuration = value; }
        public float PerfectWindow { get => _perfectWindow; internal set => _perfectWindow = value; }

        void OnValidate()
        {
            if (_perfectWindow > _iFrameDuration)
                UnityEngine.Debug.LogWarning($"[GMR/Heroes] {name}: Perfect Dodge Window ({_perfectWindow}s) is longer than the " +
                                             $"i-frames ({_iFrameDuration}s); an early Perfect Dodge would still be hit.", this);
        }

        /// <summary>DASH speed in units/s.</summary>
        public float Speed => _duration > 0f ? _distance / _duration : 0f;
    }
}

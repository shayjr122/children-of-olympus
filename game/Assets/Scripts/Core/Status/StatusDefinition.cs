using UnityEngine;

namespace GMR.Core.Status
{
    /// <summary>
    /// A status as data (FR-18, AD-10): id, duration, max stacks, refresh rule, tint/icon and behaviours. A new status with
    /// existing behaviours is a new asset only; a new behaviour type is one new <see cref="StatusBehaviour"/> subclass.
    /// Interactions between statuses live in <see cref="StatusInteractionRule"/> assets, never in behaviour code.
    /// Read live; never written at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "GMR/Definition/Status Definition", fileName = "Status_")]
    public sealed class StatusDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Stable id used in telemetry and RNG Derive.")]
        string _id = "status";

        [SerializeField] string _displayName = "Status";

        [SerializeField, Min(0f), Tooltip("Seconds an application lasts (scaled time). 0 = expires on the next tick.")]
        float _duration = 3f;

        [SerializeField, Min(1)] int _maxStacks = 1;

        [SerializeField, Tooltip("What re-applying does while the status is active.")]
        RefreshRule _refreshRule = RefreshRule.RefreshDuration;

        [Header("Visuals (placeholder)")]
        [SerializeField, Tooltip("Tint blended onto affected targets, and the icon color.")]
        Color _tint = Color.white;

        [SerializeField, Tooltip("Icon shown above affected targets.")]
        Sprite _icon;

        [SerializeField, Tooltip("Higher wins the body tint when several statuses are active.")]
        int _tintPriority;

        [Header("Behaviours (run in list order)")]
        [SerializeField] StatusBehaviour[] _behaviours = System.Array.Empty<StatusBehaviour>();

        public string Id { get => _id; internal set => _id = value; }
        public string DisplayName { get => _displayName; internal set => _displayName = value; }
        public float Duration { get => _duration; internal set => _duration = value; }
        public int MaxStacks { get => Mathf.Max(1, _maxStacks); internal set => _maxStacks = value; }
        public RefreshRule RefreshRule { get => _refreshRule; internal set => _refreshRule = value; }
        public Color Tint { get => _tint; internal set => _tint = value; }
        public Sprite Icon { get => _icon; internal set => _icon = value; }
        public int TintPriority { get => _tintPriority; internal set => _tintPriority = value; }
        public StatusBehaviour[] Behaviours { get => _behaviours ?? System.Array.Empty<StatusBehaviour>(); internal set => _behaviours = value; }
    }
}

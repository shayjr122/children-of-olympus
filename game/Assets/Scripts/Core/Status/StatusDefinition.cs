using UnityEngine;

namespace GMR.Core.Status
{
    /// <summary>
    /// Content identity of a status (AD-10). STUB for Epic 3: abilities can already name the status they apply
    /// (Thunderbolt and Storm Whirl -> Status_Shock), but duration, stacks, behaviours and rules arrive with the
    /// Status System in Epic 5. Until then the payload goes to a no-op IStatusApplier.
    /// </summary>
    [CreateAssetMenu(menuName = "GMR/Definition/Status Definition", fileName = "Status_")]
    public sealed class StatusDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Stable id used in telemetry and RNG Derive.")]
        string _id = "status";

        [SerializeField] string _displayName = "Status";

        [SerializeField, Tooltip("Placeholder tint shown on affected targets (Epic 5).")]
        Color _tint = Color.white;

        public string Id { get => _id; internal set => _id = value; }
        public string DisplayName { get => _displayName; internal set => _displayName = value; }
        public Color Tint { get => _tint; internal set => _tint = value; }
    }
}

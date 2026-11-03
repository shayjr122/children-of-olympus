using UnityEngine;

namespace GMR.Core.Heroes
{
    /// <summary>
    /// Content identity of a Hero (AD-6, AD-8). A HeroRuntime is built from it; movement and DASH tuning come from
    /// the referenced Config assets. Ability slots arrive in Epic 3.
    /// </summary>
    [CreateAssetMenu(menuName = "GMR/Definition/Hero Definition", fileName = "Hero_")]
    public sealed class HeroDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Stable id used in telemetry and RNG Derive.")]
        string _id = "hero";

        [SerializeField] string _displayName = "Hero";

        [SerializeField, Tooltip("Placeholder body color (NFR-6).")]
        Color _placeholderColor = new Color(1f, 0.6f, 0.2f, 1f);

        [SerializeField] MoveConfig _move;
        [SerializeField] DashConfig _dash;

        public string Id { get => _id; internal set => _id = value; }
        public string DisplayName { get => _displayName; internal set => _displayName = value; }
        public Color PlaceholderColor { get => _placeholderColor; internal set => _placeholderColor = value; }
        public MoveConfig Move { get => _move; internal set => _move = value; }
        public DashConfig Dash { get => _dash; internal set => _dash = value; }
    }
}

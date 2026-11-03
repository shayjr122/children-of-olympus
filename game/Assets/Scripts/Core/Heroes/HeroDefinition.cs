using GMR.Core.Abilities;
using GMR.Core.Combat;
using GMR.Core.Status;
using UnityEngine;

namespace GMR.Core.Heroes
{
    /// <summary>
    /// Content identity of a Hero (AD-6, AD-8). A HeroRuntime is built from it: movement, DASH and health tuning come
    /// from the referenced Config assets, and the kit is whatever AbilityDefinitions sit in the slot fields (any asset
    /// in any slot; re-enter Play Mode after changing them). RAGE config and chains arrive with Epics 4 and 8.
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
        [SerializeField, Tooltip("HP, knockback resistance and post-hit invulnerability.")] HealthConfig _health;

        [Header("Ability slots (slot-agnostic; Basic ignores cooldown)")]
        [SerializeField] AbilityDefinition _basic;
        [SerializeField] AbilityDefinition _special;
        [SerializeField] AbilityDefinition _signature;
        [SerializeField, Tooltip("Gated by RAGE in Epic 4; not usable yet.")] AbilityDefinition _ultimate;

        [SerializeField, Tooltip("The Hero's signature status (Heracles: Shock). Used by the Status System, Epic 5.")]
        StatusDefinition _primaryStatus;

        public string Id { get => _id; internal set => _id = value; }
        public string DisplayName { get => _displayName; internal set => _displayName = value; }
        public Color PlaceholderColor { get => _placeholderColor; internal set => _placeholderColor = value; }
        public MoveConfig Move { get => _move; internal set => _move = value; }
        public DashConfig Dash { get => _dash; internal set => _dash = value; }
        public HealthConfig Health { get => _health; internal set => _health = value; }
        public AbilityDefinition Basic { get => _basic; internal set => _basic = value; }
        public AbilityDefinition Special { get => _special; internal set => _special = value; }
        public AbilityDefinition Signature { get => _signature; internal set => _signature = value; }
        public AbilityDefinition Ultimate { get => _ultimate; internal set => _ultimate = value; }
        public StatusDefinition PrimaryStatus { get => _primaryStatus; internal set => _primaryStatus = value; }

        public AbilityDefinition Get(AbilitySlot slot) => slot switch
        {
            AbilitySlot.Basic => _basic,
            AbilitySlot.Special => _special,
            AbilitySlot.Signature => _signature,
            AbilitySlot.Ultimate => _ultimate,
            _ => throw new System.ArgumentOutOfRangeException(nameof(slot)),
        };

        void OnValidate()
        {
            // The Basic slot forces cooldown 0 (FR-9). The shared asset is left untouched so it keeps its cooldown
            // when moved back to Special/Signature; AbilityRunner applies 0 in the Basic slot.
            if (_basic != null && _basic.Cooldown > 0f)
                UnityEngine.Debug.LogWarning($"[GMR/Abilities] {name}: '{_basic.name}' is in the Basic slot; its " +
                                             $"{_basic.Cooldown}s cooldown is forced to 0 there.", this);
        }
    }
}

using GMR.Core.Combat;
using UnityEngine;

namespace GMR.Core.Status
{
    /// <summary>
    /// "When X lands on a target carrying Y, modify it" as data (FR-18, FR-21, AD-10). A rule matches an incoming hit when
    /// the target carries <see cref="TargetHas"/> and the hit either applies <see cref="WhenApplied"/> or carries any of
    /// <see cref="WhenDamageTagged"/>. On a match the chain leaving the target gets the bonus jumps/radius/falloff; the
    /// damage multiplier applies only to tag-matched damage (Wet doubles Shock damage, not a physical hit that merely
    /// applies Shock). Optionally consumes the target's status and raises ConductedShock with <see cref="ConductedEventTag"/>.
    /// Evaluated only by StatusContainer's fold; Shock code never knows about Wet.
    /// </summary>
    [CreateAssetMenu(menuName = "GMR/Status/Interaction Rule", fileName = "Rule_")]
    public sealed class StatusInteractionRule : ScriptableObject
    {
        [Header("When")]
        [SerializeField, Tooltip("Matches hits that apply this status (optional).")]
        StatusDefinition _whenApplied;

        [SerializeField, Tooltip("Matches hits carrying any of these damage tags (optional).")]
        DamageTags _whenDamageTagged = DamageTags.None;

        [SerializeField, Tooltip("...lands on a target that carries this status (required).")]
        StatusDefinition _targetHas;

        [Header("Then")]
        [SerializeField, Min(0f), Tooltip("Damage multiplier for tag-matched damage.")]
        float _damageMultiplier = 1f;

        [SerializeField, Tooltip("Extra chain jumps for a chain leaving this target.")]
        int _bonusJumps;

        [SerializeField, Tooltip("Extra chain radius (units) for the jump leaving this target.")]
        float _bonusRadius;

        [SerializeField, Tooltip("Added to the chain falloff factor for jumps after this target (0 = unchanged).")]
        float _bonusFalloff;

        [SerializeField, Tooltip("Remove the target's status when the rule fires (Wet consumed by Shock). Default off.")]
        bool _consumeTargetStatus;

        [SerializeField, Tooltip("Non-empty: StatusContainer raises ConductedShock with this tag when the rule fires.")]
        string _conductedEventTag = "";

        public StatusDefinition WhenApplied { get => _whenApplied; internal set => _whenApplied = value; }
        public DamageTags WhenDamageTagged { get => _whenDamageTagged; internal set => _whenDamageTagged = value; }
        public StatusDefinition TargetHas { get => _targetHas; internal set => _targetHas = value; }
        public float DamageMultiplier { get => Mathf.Max(0f, _damageMultiplier); internal set => _damageMultiplier = value; }
        public int BonusJumps { get => _bonusJumps; internal set => _bonusJumps = value; }
        public float BonusRadius { get => _bonusRadius; internal set => _bonusRadius = value; }
        public float BonusFalloff { get => _bonusFalloff; internal set => _bonusFalloff = value; }
        public bool ConsumeTargetStatus { get => _consumeTargetStatus; internal set => _consumeTargetStatus = value; }
        public string ConductedEventTag { get => _conductedEventTag ?? ""; internal set => _conductedEventTag = value; }

        /// <summary>True if <paramref name="request"/> triggers this rule (target check excluded); <paramref name="tagMatched"/> = by damage tag.</summary>
        public bool Matches(in DamageRequest request, out bool tagMatched)
        {
            tagMatched = _whenDamageTagged != DamageTags.None && (request.Tags & _whenDamageTagged) != 0;
            bool applied = _whenApplied != null && request.Status.Def == _whenApplied;
            return tagMatched || applied;
        }
    }
}

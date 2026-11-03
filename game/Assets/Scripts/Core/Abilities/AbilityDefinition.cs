using System.Collections.Generic;
using GMR.Core.Combat;
using GMR.Core.Status;
using UnityEngine;

namespace GMR.Core.Abilities
{
    /// <summary>
    /// One ability as data (FR-9, AD-8): attack-type tag, hit shape and geometry, timings, cooldown, damage, tags,
    /// knockback, rootsCaster, an optional status payload and an optional ProjectileConfig. Slot-agnostic: the same
    /// asset works in Basic, Special or Signature (the Basic slot ignores the cooldown, see AbilityRunner). Read live
    /// at use; timings are snapshotted when the ability starts.
    /// </summary>
    [CreateAssetMenu(menuName = "GMR/Definition/Ability Definition", fileName = "Ability_")]
    public sealed class AbilityDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Stable id used in telemetry and RNG Derive.")]
        string _id = "ability";

        [SerializeField] string _displayName = "Ability";

        [Header("Type and shape")]
        [SerializeField, Tooltip("Design tag only (Thrust / Shot / Spin).")]
        AttackType _attackType = AttackType.Thrust;

        [SerializeField, Tooltip("How targets are found: Box (in front, along aim), Circle (around the Hero), Projectile.")]
        HitShape _hitShape = HitShape.Box;

        [SerializeField, Min(0f), Tooltip("Box: length along the aim. Circle: radius around the Hero. Units.")]
        float _range = 1.3f;

        [SerializeField, Min(0f), Tooltip("Box: width across the aim, units.")]
        float _width = 0.9f;

        [SerializeField, Tooltip("Box: gap between the Hero's center and the start of the box, units.")]
        float _forwardOffset = 0.2f;

        [SerializeField, Tooltip("Shot: projectile tuning (required when the hit shape is Projectile).")]
        ProjectileConfig _projectile;

        [Header("Timings (seconds)")]
        [SerializeField, Min(0f)] float _startup = 0.08f;
        [SerializeField, Min(0f), Tooltip("Hits are checked during Active. Projectiles spawn when Active starts.")] float _active = 0.08f;
        [SerializeField, Min(0f), Tooltip("DASH cancels the recovery.")] float _recovery = 0.2f;
        [SerializeField, Min(0f), Tooltip("Starts when the ability is used. Ignored (0) in the Basic slot.")] float _cooldown;

        [Header("Hit")]
        [SerializeField, Min(0f)] float _damage = 10f;
        [SerializeField] DamageTags _damageTags = DamageTags.Physical;
        [SerializeField, Tooltip("Heavy hits get the heavy hitstop and shake the camera.")] HitWeight _hitWeight = HitWeight.Light;
        [SerializeField, Min(0f), Tooltip("Knockback strength (impulse) before the target's resistance.")] float _knockback = 4f;

        [SerializeField, Tooltip("If set, the Hero cannot move from Startup until the end of Recovery.")]
        bool _rootsCaster;

        [Header("Status (applied by the Status System, Epic 5)")]
        [SerializeField] StatusDefinition _status;
        [SerializeField, Min(1)] int _statusStacks = 1;

        [Header("Placeholder")]
        [SerializeField, Tooltip("Color of the hit-area flash shown during Active.")]
        Color _color = new Color(1f, 0.85f, 0.4f, 0.55f);

        public string Id { get => _id; internal set => _id = value; }
        public string DisplayName { get => _displayName; internal set => _displayName = value; }
        public AttackType AttackType { get => _attackType; internal set => _attackType = value; }
        public HitShape HitShape { get => _hitShape; internal set => _hitShape = value; }
        public float Range { get => _range; internal set => _range = value; }
        public float Width { get => _width; internal set => _width = value; }
        public float ForwardOffset { get => _forwardOffset; internal set => _forwardOffset = value; }
        public ProjectileConfig Projectile { get => _projectile; internal set => _projectile = value; }
        public float Startup { get => _startup; internal set => _startup = value; }
        public float Active { get => _active; internal set => _active = value; }
        public float Recovery { get => _recovery; internal set => _recovery = value; }
        public float Cooldown { get => _cooldown; internal set => _cooldown = value; }
        public float Damage { get => _damage; internal set => _damage = value; }
        public DamageTags DamageTags { get => _damageTags; internal set => _damageTags = value; }
        public HitWeight HitWeight { get => _hitWeight; internal set => _hitWeight = value; }
        public float Knockback { get => _knockback; internal set => _knockback = value; }
        public bool RootsCaster { get => _rootsCaster; internal set => _rootsCaster = value; }
        public StatusDefinition Status { get => _status; internal set => _status = value; }
        public int StatusStacks { get => _statusStacks; internal set => _statusStacks = value; }
        public Color Color { get => _color; internal set => _color = value; }

        /// <summary>The status payload hits of this ability carry (none if no status is set).</summary>
        public StatusPayload StatusPayload => new StatusPayload(_status, _statusStacks);

        /// <summary>Appends one message per problem to <paramref name="errors"/>; returns true if valid.</summary>
        public bool Validate(List<string> errors)
        {
            int before = errors.Count;
            if (_hitShape == HitShape.Projectile)
            {
                if (_projectile == null) errors.Add($"{name}: hit shape Projectile needs a ProjectileConfig.");
                else _projectile.Validate(errors);
            }
            if ((_hitShape == HitShape.Box || _hitShape == HitShape.Circle) && !(_range > 0f))
                errors.Add($"{name}: range must be > 0 for {_hitShape}.");
            if (_hitShape == HitShape.Box && !(_width > 0f)) errors.Add($"{name}: width must be > 0 for Box.");
            if (_hitShape == HitShape.AllEnemies || _hitShape == HitShape.ExpandingWave)
                errors.Add($"{name}: hit shape {_hitShape} has no resolver yet (Ultimates, Epics 4/8).");
            return errors.Count == before;
        }

        void OnValidate()
        {
            var errors = new List<string>();
            if (!Validate(errors))
                UnityEngine.Debug.LogWarning("[GMR/Abilities] " + string.Join(" ", errors), this);
        }
    }
}

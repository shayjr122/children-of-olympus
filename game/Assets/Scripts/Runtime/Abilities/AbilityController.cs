using System.Collections.Generic;
using GMR.Core.Abilities;
using GMR.Core.Combat;
using GMR.Runtime.Heroes;
using UnityEngine;

namespace GMR.Runtime.Abilities
{
    /// <summary>
    /// Runs the Hero's ability slots (FR-9, AD-8): ticks each AbilityRunner with scaled time, starts the first
    /// buffered legal ability (Basic / Special / Signature, FR-8) along the fire direction (Aim Assist applied), and
    /// hands the Active phase to the resolver for the ability's HitShape (Box, Circle, Projectile). Shows the hit area
    /// as a placeholder shape during Active. Runs before DashController so DASH sees fresh ability phases.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(HeroRuntime))]
    public sealed class AbilityController : MonoBehaviour
    {
        [SerializeField, Tooltip("Layers abilities can hit (Enemy).")] LayerMask _targetLayers;
        [SerializeField, Tooltip("Layers that stop projectiles (Wall).")] LayerMask _wallLayers;
        [SerializeField, Tooltip("Spawned by Shot (Projectile hit shape) abilities.")] Projectile _projectilePrefab;
        [SerializeField, Tooltip("Placeholder for Box hit areas (1x1 unit square sprite).")] SpriteRenderer _boxShape;
        [SerializeField, Tooltip("Placeholder for Circle hit areas (1 unit wide circle sprite).")] SpriteRenderer _circleShape;

        readonly Dictionary<AbilityRunner, AbilityCast> _casts = new Dictionary<AbilityRunner, AbilityCast>(4);
        HeroRuntime _hero;
        IHitResolver _box;
        IHitResolver _circle;
        IHitResolver _projectile;
        bool _warnedShape;

        void Awake()
        {
            _hero = GetComponent<HeroRuntime>();
            _box = new BoxHitResolver(_targetLayers);
            _circle = new CircleHitResolver(_targetLayers);
            _projectile = new ProjectileHitResolver(_projectilePrefab, _targetLayers, _wallLayers);
            SetShape(_boxShape, false);
            SetShape(_circleShape, false);
        }

        void Update()
        {
            if (!_hero.IsInitialized || _hero.Owner == null || !_hero.Owner.IsInitialized || _hero.Abilities == null) return;
            float now = Time.time;
            var loadout = _hero.Abilities;
            var runners = loadout.Runners;
            for (int i = 0; i < runners.Count; i++) TickRunner(runners[i], now);

            if (Time.timeScale > 0f)
            {
                _hero.Dash.Tick(now);
                if (!_hero.Dash.IsDashing)
                {
                    var owner = _hero.Owner;
                    var started = loadout.TryStartBuffered(owner.Buffer, now, _hero.Position, owner.ResolveFireDirection());
                    if (started != null) TickRunner(started, now); // zero startup goes Active this frame
                }
            }
            UpdateShapes();
        }

        void TickRunner(AbilityRunner runner, float now)
        {
            var step = runner.Tick(now);
            bool entered = (step & AbilityStep.EnteredActive) != 0;
            bool active = runner.Phase == AbilityPhase.Active || (step & AbilityStep.ExitedActive) != 0;
            if (!entered && !active) return;

            var cast = GetCast(runner);
            var resolver = ResolverFor(runner.Definition);
            if (resolver == null) return;
            var origin = new HitOrigin(_hero.Position, _hero.Radius);
            if (entered)
            {
                cast.Begin(_hero.Owner.Context.Damage.NextHitId());
                resolver.Begin(cast, origin, now);
            }
            else
            {
                resolver.Tick(cast, origin, now);
            }
        }

        AbilityCast GetCast(AbilityRunner runner)
        {
            if (!_casts.TryGetValue(runner, out var cast))
            {
                cast = new AbilityCast(runner, SourceId.ForPlayer(_hero.Owner.Id), _hero.Owner.Context.Damage);
                _casts.Add(runner, cast);
            }
            return cast;
        }

        IHitResolver ResolverFor(AbilityDefinition definition)
        {
            switch (definition.HitShape)
            {
                case HitShape.Box: return _box;
                case HitShape.Circle: return _circle;
                case HitShape.Projectile: return _projectile;
                default:
                    if (!_warnedShape)
                    {
                        _warnedShape = true;
                        UnityEngine.Debug.LogError($"[GMR/Abilities] No hit resolver for {definition.HitShape} ('{definition.name}').", definition);
                    }
                    return null;
            }
        }

        void UpdateShapes()
        {
            var current = _hero.Abilities.Current;
            bool showBox = false, showCircle = false;
            if (current != null && current.Phase == AbilityPhase.Active && _casts.TryGetValue(current, out var cast))
            {
                var def = current.Definition;
                var origin = new HitOrigin(_hero.Position, _hero.Radius);
                if (def.HitShape == HitShape.Box && _boxShape != null)
                {
                    BoxHitResolver.GetBox(cast, origin, out Vector2 center, out Vector2 size, out float angle);
                    var t = _boxShape.transform;
                    t.SetPositionAndRotation(center, Quaternion.Euler(0f, 0f, angle));
                    t.localScale = new Vector3(size.x, size.y, 1f);
                    _boxShape.color = def.Color;
                    showBox = true;
                }
                else if (def.HitShape == HitShape.Circle && _circleShape != null)
                {
                    var t = _circleShape.transform;
                    t.position = origin.Position;
                    float d = Mathf.Max(0.01f, def.Range) * 2f;
                    t.localScale = new Vector3(d, d, 1f);
                    _circleShape.color = def.Color;
                    showCircle = true;
                }
            }
            SetShape(_boxShape, showBox);
            SetShape(_circleShape, showCircle);
        }

        static void SetShape(SpriteRenderer shape, bool visible)
        {
            if (shape != null && shape.enabled != visible) shape.enabled = visible;
        }
    }
}

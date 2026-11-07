using GMR.Core.Combat;
using GMR.Core.Enemies;
using GMR.Core.Status;
using GMR.Runtime.Encounter;
using UnityEngine;

namespace GMR.DevTools
{
    /// <summary>
    /// Training dummy (Story 3.1): an IDamageable enemy on the Enemy layer with HP from a HealthConfig, knocked back by
    /// the impulse DamageSystem resolves (ApplyResolved), walking back to its spot after a while. At 0 HP DamageSystem
    /// raises EnemyDied; the dummy hides and after the configured delay respawns at its spot with full HP (restored
    /// through DamageSystem, the only HP writer). Also an Aim Assist candidate and a chain-lightning target while alive.
    /// Carries a StatusContainer (registered for StatusTicker while enabled); statuses end when it breaks.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class TrainingDummy : MonoBehaviour, IDamageable, IEnemyTarget
    {
        [SerializeField] EncounterBootstrap _bootstrap;
        [SerializeField] HealthConfig _health;
        [SerializeField] TrainingDummyConfig _config;
        [SerializeField, Tooltip("Hidden while broken.")] GameObject _visualRoot;

        readonly InvulnerabilityTracker _invulnerability = new InvulnerabilityTracker();
        Rigidbody2D _body;
        Collider2D[] _colliders;
        Vector2 _home;
        float _lastHitTime = float.NegativeInfinity;
        float _respawnAt = float.PositiveInfinity;
        bool _registered;
        bool _broken;

        public SourceId Id { get; private set; }
        public bool IsAlive => _registered && !_broken;
        public Vector2 Position => _body != null ? _body.position : (Vector2)transform.position;
        public Health Health { get; private set; }
        public StatusContainer Statuses { get; private set; }
        public float KnockbackResistance => _health != null ? _health.KnockbackResistance : 0f;
        public bool IsInvulnerable(float now) => _invulnerability.IsInvulnerable(now);

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _colliders = GetComponentsInChildren<Collider2D>(true);
            _home = transform.position;
        }

        void OnEnable()
        {
            if (_bootstrap == null || _health == null || _config == null)
            {
                UnityEngine.Debug.LogError("[GMR/DevTools] TrainingDummy needs a bootstrap, a HealthConfig and a TrainingDummyConfig.", this);
                enabled = false;
                return;
            }
            var context = _bootstrap.Context;
            if (Health == null)
            {
                Id = SourceId.ForEnemy(context.Enemies.NextEnemyId());
                Health = new Health(_health.MaxHp);
                Statuses = new StatusContainer(this, context.StatusServices);
            }
            context.Enemies.Register(this);
            context.Statuses.Register(Statuses);
            context.Events.OnEnemyDied += OnEnemyDied;
            _registered = true;
        }

        void OnDisable()
        {
            if (!_registered) return;
            var context = _bootstrap.Context;
            context.Events.OnEnemyDied -= OnEnemyDied;
            context.Enemies.Unregister(this);
            context.Statuses.Unregister(Statuses);
            Statuses.Clear(Time.time);
            _registered = false;
        }

        public void ApplyResolved(in DamageResult result)
        {
            if (result.Avoided) return;
            _lastHitTime = result.At;
            if (result.KnockbackImpulse.sqrMagnitude > 0f) _body.AddForce(result.KnockbackImpulse, ForceMode2D.Impulse);
            _invulnerability.Register(result.At, _health.PostHitInvulnerability, InvulnerabilitySource.PostHit);
        }

        void OnEnemyDied(EnemyDied e)
        {
            if (e.Enemy != Id) return;
            _broken = true;
            _respawnAt = Time.time + _config.RespawnDelay;
            Statuses.Clear(Time.time);
            SetVisible(false);
        }

        void Update()
        {
            if (_broken && Time.time >= _respawnAt) Respawn();
        }

        void FixedUpdate()
        {
            if (_broken || _config.ReturnSpeed <= 0f) return;
            if (Time.time - _lastHitTime < _config.ReturnDelay) return;
            Vector2 toHome = _home - _body.position;
            float distance = toHome.magnitude;
            if (distance < 0.02f)
            {
                _body.linearVelocity = Vector2.zero; // arrived: do not drift past home
                return;
            }
            float step = Mathf.Min(distance, _config.ReturnSpeed * Time.fixedDeltaTime);
            _body.linearVelocity = toHome / distance * (step / Time.fixedDeltaTime);
        }

        void Respawn()
        {
            _broken = false;
            _respawnAt = float.PositiveInfinity;
            _invulnerability.Clear();
            _body.position = _home;
            transform.position = _home;
            _body.linearVelocity = Vector2.zero;
            _bootstrap.Context.Damage.Restore(this, _health.MaxHp);
            SetVisible(true);
        }

        void SetVisible(bool visible)
        {
            if (_visualRoot != null) _visualRoot.SetActive(visible);
            for (int i = 0; i < _colliders.Length; i++) _colliders[i].enabled = visible;
            _body.simulated = visible;
        }
    }
}

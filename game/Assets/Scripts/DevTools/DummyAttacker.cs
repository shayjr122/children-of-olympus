using GMR.Core.Combat;
using GMR.Core.Enemies;
using GMR.Runtime.Encounter;
using UnityEngine;

namespace GMR.DevTools
{
    /// <summary>
    /// Stand-in attacker for Perfect Dodge practice (Story 2.4). Every few seconds it telegraphs a circular slam (the
    /// fill grows until it reaches the ring = the hit lands), registering a PendingHit with its land time when the
    /// telegraph starts. On landing it sends a DamageRequest for each overlapping Player through DamageSystem, using the
    /// land time as the clock (the same clock the Perfect Dodge used): i-frames -> HitAvoided, otherwise PlayerDamaged
    /// (red flash, number and post-hit invulnerability come from FeedbackRouter / the Hero). It is not damageable, but
    /// it is an Aim Assist candidate.
    /// </summary>
    public sealed class DummyAttacker : MonoBehaviour, IEnemyTarget
    {
        [SerializeField] EncounterBootstrap _bootstrap;
        [SerializeField] DummyAttackerConfig _config;
        [SerializeField, Tooltip("Faint full-size ring showing the hit area.")] SpriteRenderer _areaRing;
        [SerializeField, Tooltip("Fill that grows during the telegraph and flashes on impact.")] SpriteRenderer _areaFill;
        [SerializeField, Min(0f), Tooltip("Extra delay before the first attack, to desync several dummies.")] float _startDelay;
        [SerializeField] Color _telegraphColor = new Color(1f, 0.45f, 0.1f, 0.45f);
        [SerializeField] Color _impactColor = new Color(1f, 0.15f, 0.1f, 0.8f);

        float _nextTelegraph;
        float _landTime;
        float _impactUntil;
        int _attackId;
        bool _pending;
        bool _registered;
        SourceId _source;
        bool _hasId;

        public bool IsAlive => isActiveAndEnabled;
        public Vector2 Position => transform.position;

        void OnEnable()
        {
            if (_bootstrap == null || _config == null)
            {
                UnityEngine.Debug.LogError("[GMR/DevTools] DummyAttacker needs a bootstrap and a config.", this);
                enabled = false;
                return;
            }
            if (!_hasId)
            {
                _source = SourceId.ForEnemy(_bootstrap.Context.Enemies.NextEnemyId());
                _hasId = true;
            }
            _bootstrap.Context.Enemies.Register(this);
            _registered = true;
            _nextTelegraph = Time.time + _startDelay + _config.RestTime;
            SetFill(0f, _telegraphColor, false);
        }

        void OnDisable()
        {
            if (_bootstrap == null) return;
            if (_pending) _bootstrap.Context.PendingHits.Remove(_attackId);
            _pending = false;
            if (_registered) _bootstrap.Context.Enemies.Unregister(this);
            _registered = false;
        }

        void Update()
        {
            float now = Time.time;
            float radius = _config.Radius;
            if (_areaRing != null) _areaRing.transform.localScale = Vector3.one * (radius * 2f);

            if (!_pending && now >= _nextTelegraph) BeginTelegraph(now, radius);

            if (_pending)
            {
                float progress = Mathf.Clamp01(1f - (_landTime - now) / Mathf.Max(_config.TelegraphTime, 0.0001f));
                SetFill(radius * 2f * progress, _telegraphColor, true);
                if (now >= _landTime) Land(now, radius);
            }
            else if (now < _impactUntil)
            {
                SetFill(radius * 2f, _impactColor, true);
            }
            else
            {
                SetFill(0f, _telegraphColor, false);
            }
        }

        void BeginTelegraph(float now, float radius)
        {
            var hits = _bootstrap.Context.PendingHits;
            _attackId = hits.NextAttackId();
            _landTime = now + _config.TelegraphTime;
            hits.Register(new PendingHit(_attackId, _landTime, transform.position, radius));
            _pending = true;
        }

        void Land(float now, float radius)
        {
            var context = _bootstrap.Context;
            var area = new PendingHit(_attackId, _landTime, transform.position, radius);
            var players = context.Players.All;
            int hitId = context.Damage.NextHitId();
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                var target = player.Damageable;
                if (target == null || !player.IsAlive || !area.Overlaps(player.Position, player.Radius)) continue;
                Vector2 away = player.Position - (Vector2)transform.position;
                Vector2 knockback = away.sqrMagnitude > 1e-6f ? away.normalized * _config.Knockback : Vector2.zero;
                context.Damage.Apply(target, new DamageRequest(_source, _config.Damage, DamageTags.Physical, knockback,
                    default, hitId, HitWeight.Heavy), _landTime);
            }

            context.PendingHits.Remove(_attackId);
            _pending = false;
            _impactUntil = now + _config.ImpactFlashTime;
            _nextTelegraph = now + _config.RestTime;
        }

        void SetFill(float diameter, Color color, bool visible)
        {
            if (_areaFill == null) return;
            if (_areaFill.enabled != visible) _areaFill.enabled = visible;
            if (!visible) return;
            _areaFill.transform.localScale = Vector3.one * diameter;
            _areaFill.color = color;
        }
    }
}

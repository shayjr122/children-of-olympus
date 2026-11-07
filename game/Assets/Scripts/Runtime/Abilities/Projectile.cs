using System.Collections.Generic;
using GMR.Core.Abilities;
using GMR.Core.Combat;
using UnityEngine;

namespace GMR.Runtime.Abilities
{
    /// <summary>
    /// A Shot projectile (FR-9): a trigger collider on the Projectile layer that flies along its direction at the
    /// ProjectileConfig speed (read live), damages the first target it touches through DamageSystem (more with
    /// pierce), and disappears on walls or when its lifetime (scaled time) ends.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _renderer;

        readonly HashSet<IDamageable> _hit = new HashSet<IDamageable>();
        Rigidbody2D _body;
        DamageSystem _damage;
        AbilityDefinition _ability;
        ProjectileConfig _config;
        SourceId _source;
        Vector2 _direction;
        LayerMask _targets;
        LayerMask _walls;
        int _hitId;
        int _pierceLeft;
        float _expiresAt;
        bool _done;

        public bool IsLaunched => _ability != null;

        void Awake() => _body = GetComponent<Rigidbody2D>();

        public void Launch(DamageSystem damage, SourceId source, AbilityDefinition ability, Vector2 direction, int hitId,
            LayerMask targets, LayerMask walls, float now)
        {
            _damage = damage;
            _source = source;
            _ability = ability;
            _config = ability.Projectile;
            _direction = direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector2.right;
            _hitId = hitId;
            _targets = targets;
            _walls = walls;
            _pierceLeft = _config.Pierce;
            _expiresAt = now + _config.Lifetime;
            transform.localScale = Vector3.one * (_config.Radius * 2f); // prefab collider radius 0.5 = 1 unit across
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);
            if (_renderer != null) _renderer.color = _config.Color;
            _body.linearVelocity = _direction * _config.Speed;
            gameObject.name = $"Projectile_{ability.Id}";
        }

        void FixedUpdate()
        {
            if (!IsLaunched || _done) return;
            if (Time.time >= _expiresAt)
            {
                Despawn();
                return;
            }
            _body.linearVelocity = _direction * _config.Speed;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsLaunched || _done) return;
            int bit = 1 << other.gameObject.layer;
            if ((_walls.value & bit) != 0)
            {
                Despawn();
                return;
            }
            if ((_targets.value & bit) == 0) return;

            var target = DamageableLookup.Find(other);
            if (target == null || !target.IsAlive || !_hit.Add(target)) return;
            _damage.Apply(target, new DamageRequest(_source, _ability.Damage, _ability.DamageTags, _direction * _ability.Knockback,
                _ability.StatusPayload, _hitId, _ability.HitWeight), Time.time);
            if (_pierceLeft-- <= 0) Despawn();
        }

        void Despawn()
        {
            _done = true;
            Destroy(gameObject);
        }
    }
}

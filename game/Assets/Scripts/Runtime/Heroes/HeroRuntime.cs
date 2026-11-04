using GMR.Core.Abilities;
using GMR.Core.Combat;
using GMR.Core.Heroes;
using GMR.Runtime.Players;
using UnityEngine;

namespace GMR.Runtime.Heroes
{
    /// <summary>
    /// The replaceable Hero body under a PlayerRoot (AD-6): physics body, movement/DASH state, ability slots, health and
    /// visuals, all tuned by its HeroDefinition. It is the Player's IDamageable: DamageSystem changes its Health and
    /// calls ApplyResolved (knockback impulse + post-hit invulnerability). Hero Swap (Epic 8) destroys it and builds
    /// another; per-person state stays on PlayerRoot. Player death arrives with Epic 7 (HP stops at 0 for now).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class HeroRuntime : MonoBehaviour, IDamageable
    {
        [SerializeField] SpriteRenderer _body;

        Rigidbody2D _rigidbody;
        CircleCollider2D _collider;

        public PlayerRoot Owner { get; private set; }
        public HeroDefinition Definition { get; private set; }
        public Rigidbody2D Body => _rigidbody;
        public SpriteRenderer BodyRenderer => _body;

        /// <summary>Sole owner of this Hero's invulnerability windows (AD-16).</summary>
        public InvulnerabilityTracker Invulnerability { get; } = new InvulnerabilityTracker();

        /// <summary>DASH cycle state. Only DashController starts a DASH; HeroMover only advances its clock.</summary>
        public DashState Dash { get; } = new DashState();

        /// <summary>Ability slots built from the HeroDefinition (FR-9). Run by AbilityController.</summary>
        public AbilityLoadout Abilities { get; private set; }

        public Vector2 Position => _rigidbody != null ? _rigidbody.position : (Vector2)transform.position;

        public float Radius
        {
            get
            {
                if (_collider == null) return 0.5f;
                var s = transform.lossyScale;
                return _collider.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
            }
        }

        public bool IsInitialized => Definition != null;

        // IDamageable (AD-9). HP is only ever changed by DamageSystem.
        public SourceId Id => Owner != null ? SourceId.ForPlayer(Owner.Id) : default;
        // At 0 HP the Hero takes no more hits (no PlayerDamaged spam); Player death itself arrives with Epic 7.
        public bool IsAlive => IsInitialized && Health != null && !Health.IsDepleted;
        public Health Health { get; private set; }
        public float KnockbackResistance => Definition != null && Definition.Health != null ? Definition.Health.KnockbackResistance : 0f;
        public bool IsInvulnerable(float now) => Invulnerability.IsInvulnerable(now);

        void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CircleCollider2D>();
        }

        public void Init(PlayerRoot owner, HeroDefinition definition)
        {
            Owner = owner;
            Definition = definition;
            if (definition.Move == null || definition.Dash == null || definition.Health == null)
                UnityEngine.Debug.LogError($"[GMR/Heroes] HeroDefinition '{definition.Id}' is missing Move, Dash or Health config.", definition);
            Health = new Health(definition.Health != null ? definition.Health.MaxHp : 100f);
            Abilities = new AbilityLoadout(owner.Id, owner.Context.Events, definition.Basic, definition.Special, definition.Signature,
                definition.Ultimate);
            if (_body != null) _body.color = definition.PlaceholderColor;
            gameObject.name = $"Hero_{definition.Id}";
        }

        public void ApplyResolved(in DamageResult result)
        {
            if (result.Avoided) return;
            if (_rigidbody != null && result.KnockbackImpulse.sqrMagnitude > 0f)
                _rigidbody.AddForce(result.KnockbackImpulse, ForceMode2D.Impulse);
            float postHit = Definition != null && Definition.Health != null ? Definition.Health.PostHitInvulnerability : 0f;
            Invulnerability.Register(result.At, postHit, InvulnerabilitySource.PostHit);
        }
    }
}

using GMR.Core.Combat;
using GMR.Core.Heroes;
using GMR.Runtime.Players;
using UnityEngine;

namespace GMR.Runtime.Heroes
{
    /// <summary>
    /// The replaceable Hero body under a PlayerRoot (AD-6): physics body, movement/DASH state and visuals, all tuned by
    /// its HeroDefinition. Hero Swap (Epic 8) destroys it and builds another; per-person state stays on PlayerRoot.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class HeroRuntime : MonoBehaviour
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

        void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CircleCollider2D>();
        }

        public void Init(PlayerRoot owner, HeroDefinition definition)
        {
            Owner = owner;
            Definition = definition;
            if (definition.Move == null || definition.Dash == null)
                UnityEngine.Debug.LogError($"[GMR/Heroes] HeroDefinition '{definition.Id}' is missing Move or Dash config.", definition);
            if (_body != null) _body.color = definition.PlaceholderColor;
            gameObject.name = $"Hero_{definition.Id}";
        }
    }
}

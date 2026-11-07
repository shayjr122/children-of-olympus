using System.Collections.Generic;
using GMR.Core.Abilities;
using GMR.Core.Combat;
using GMR.Core.Feedback;
using GMR.Runtime.Encounter;
using UnityEngine;

namespace GMR.Runtime.Feedback
{
    /// <summary>
    /// Placeholder Ultimate visuals (FR-17, NFR-6): a charge ring shrinking onto the Hero during the Ultimate's startup,
    /// a lightning bolt (tall bar + impact disc) on every enemy an Ultimate hit strikes, and a full-screen flash. Shake,
    /// hitstop and SFX are FeedbackRouter's. Subscribes to CombatEvents only; animates on unscaled time.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class UltimateFeedback : MonoBehaviour
    {
        const float BoltHeight = 9f;

        [SerializeField] EncounterBootstrap _bootstrap;
        [SerializeField] FeedbackConfig _config;
        [SerializeField, Tooltip("Full-screen overlay image for the Ultimate flash (alpha animated).")] UnityEngine.UI.Image _flash;
        [SerializeField, Tooltip("1x1 unit square sprite (bolt).")] Sprite _square;
        [SerializeField, Tooltip("1 unit circle sprite (impact).")] Sprite _circle;
        [SerializeField, Tooltip("1 unit ring sprite (charge-up).")] Sprite _ring;

        struct Fx
        {
            public SpriteRenderer Renderer;
            public float Age;
            public float Duration;
            public Color Color;
            public Vector2 StartScale;
            public Vector2 EndScale;
        }

        readonly List<Fx> _active = new List<Fx>(16);
        CombatEvents _events;
        float _flashRemaining;

        void OnEnable()
        {
            if (_bootstrap == null || _config == null) return;
            _events = _bootstrap.Context.Events;
            _events.OnUltimateUsed += OnUltimateUsed;
            _events.OnHitLanded += OnHitLanded;
            SetFlashAlpha(0f);
        }

        void OnDisable()
        {
            if (_events != null)
            {
                _events.OnUltimateUsed -= OnUltimateUsed;
                _events.OnHitLanded -= OnHitLanded;
            }
            _events = null;
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Renderer != null) Destroy(_active[i].Renderer.gameObject);
            }
            _active.Clear();
            SetFlashAlpha(0f);
        }

        void OnUltimateUsed(UltimateUsed e)
        {
            float startup = e.Ability != null ? Mathf.Max(0.05f, e.Ability.Startup) : 0.3f;
            Spawn("UltimateCharge", _ring, e.Position, _config.UltimateChargeColor, startup, new Vector2(5f, 5f), new Vector2(1.2f, 1.2f), 30);
        }

        void OnHitLanded(HitLanded e)
        {
            if ((e.Result.Tags & DamageTags.Ultimate) == 0) return;
            float d = _config.UltimateBoltDuration;
            var c = _config.UltimateBoltColor;
            Spawn("UltimateBolt", _square, e.Position + new Vector2(0f, BoltHeight * 0.5f), c, d,
                new Vector2(0.45f, BoltHeight), new Vector2(0.05f, BoltHeight), 31);
            Spawn("UltimateImpact", _circle, e.Position, c, d, new Vector2(0.6f, 0.6f), new Vector2(2.2f, 2.2f), 32);
            if (_config.UltimateFlash) _flashRemaining = _config.UltimateFlashDuration;
        }

        void Spawn(string name, Sprite sprite, Vector2 position, Color color, float duration, Vector2 startScale,
            Vector2 endScale, int order)
        {
            if (sprite == null || duration <= 0f) return;
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(startScale.x, startScale.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            _active.Add(new Fx { Renderer = sr, Age = 0f, Duration = duration, Color = color, StartScale = startScale, EndScale = endScale });
        }

        void Update()
        {
            float dt = GMR.Runtime.Encounter.FrameClock.UnscaledDeltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var fx = _active[i];
                fx.Age += dt;
                if (fx.Renderer == null || fx.Age >= fx.Duration)
                {
                    if (fx.Renderer != null) Destroy(fx.Renderer.gameObject);
                    _active.RemoveAt(i);
                    continue;
                }
                float t = fx.Age / fx.Duration;
                var s = Vector2.Lerp(fx.StartScale, fx.EndScale, t);
                fx.Renderer.transform.localScale = new Vector3(s.x, s.y, 1f);
                var c = fx.Color;
                c.a = fx.Color.a * (1f - t * t);
                fx.Renderer.color = c;
                _active[i] = fx;
            }

            if (_flashRemaining > 0f)
            {
                _flashRemaining -= dt;
                float duration = Mathf.Max(_config.UltimateFlashDuration, 0.0001f);
                SetFlashAlpha(_config.UltimateFlashColor.a * Mathf.Clamp01(_flashRemaining / duration));
            }
        }

        void SetFlashAlpha(float alpha)
        {
            if (_flash == null) return;
            var c = _config != null ? _config.UltimateFlashColor : _flash.color;
            c.a = alpha;
            _flash.color = c;
        }
    }
}

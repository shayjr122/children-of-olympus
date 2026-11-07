using System.Collections.Generic;
using GMR.Core.Combat;
using GMR.Core.Feedback;
using GMR.Core.Rng;
using GMR.Core.Status;
using GMR.Runtime.Encounter;
using UnityEngine;

namespace GMR.Runtime.Feedback
{
    /// <summary>
    /// Placeholder chain lightning (FR-19, FR-21, NFR-6): every ChainJumped draws a zigzag line of stretched square
    /// sprites from the target the jump left to the one it hit, fading out; conducted jumps (Wet) are wider and blue-white.
    /// Zigzag jitter uses the cosmetic Feedback RNG stream (AD-2). Subscribes to CombatEvents only; unscaled FrameClock time.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class ChainLightningFeedback : MonoBehaviour
    {
        const int SortingOrder = 40;

        [SerializeField] EncounterBootstrap _bootstrap;
        [SerializeField] FeedbackConfig _config;
        [SerializeField, Tooltip("1x1 unit square sprite (line segments).")] Sprite _square;

        struct Segment
        {
            public SpriteRenderer Renderer;
            public float Age;
            public float Duration;
            public Color Color;
            public float Width;
        }

        readonly List<Segment> _active = new List<Segment>(32);
        readonly List<Vector2> _points = new List<Vector2>(8);
        CombatEvents _events;

        /// <summary>Lines drawn so far (tests): total, and how many were conducted.</summary>
        public int LinesDrawn { get; private set; }
        public int ConductedLinesDrawn { get; private set; }

        /// <summary>Segments currently visible.</summary>
        public int ActiveSegments => _active.Count;

        void OnEnable()
        {
            if (_bootstrap == null || _config == null) return;
            _events = _bootstrap.Context.Events;
            _events.OnChainJumped += OnChainJumped;
        }

        void OnDisable()
        {
            if (_events != null) _events.OnChainJumped -= OnChainJumped;
            _events = null;
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Renderer != null) Destroy(_active[i].Renderer.gameObject);
            }
            _active.Clear();
        }

        void OnChainJumped(ChainJumped e)
        {
            if (_square == null) return;
            LinesDrawn++;
            if (e.Conducted) ConductedLinesDrawn++;
            var color = e.Conducted ? _config.ConductedChainColor : _config.ChainColor;
            float width = e.Conducted ? _config.ConductedChainWidth : _config.ChainWidth;
            var rng = _bootstrap.Context.Rng.Stream(RngStreams.Feedback);

            Vector2 a = e.FromPosition, b = e.ToPosition;
            Vector2 along = b - a;
            Vector2 side = along.sqrMagnitude > 1e-6f ? new Vector2(-along.y, along.x).normalized : Vector2.up;
            int segments = _config.ChainSegments;
            _points.Clear();
            _points.Add(a);
            for (int i = 1; i < segments; i++)
            {
                float jitter = _config.ChainJitter > 0f ? rng.Range(-_config.ChainJitter, _config.ChainJitter) : 0f;
                _points.Add(a + along * (i / (float)segments) + side * jitter);
            }
            _points.Add(b);
            for (int i = 0; i < _points.Count - 1; i++) SpawnSegment(_points[i], _points[i + 1], color, width);
            if (e.Conducted) // a thin white core makes the conducted arc read "blue-white"
                for (int i = 0; i < _points.Count - 1; i++) SpawnSegment(_points[i], _points[i + 1], Color.white, width * 0.35f);
        }

        void SpawnSegment(Vector2 from, Vector2 to, Color color, float width)
        {
            Vector2 d = to - from;
            float length = d.magnitude;
            if (length < 1e-4f) return;
            var go = new GameObject("ChainSegment");
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation((from + to) * 0.5f, Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg));
            go.transform.localScale = new Vector3(length, width, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _square;
            sr.color = color;
            sr.sortingOrder = SortingOrder;
            _active.Add(new Segment { Renderer = sr, Age = 0f, Duration = Mathf.Max(0.01f, _config.ChainDuration), Color = color, Width = width });
        }

        void Update()
        {
            float dt = FrameClock.UnscaledDeltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var s = _active[i];
                s.Age += dt;
                if (s.Renderer == null || s.Age >= s.Duration)
                {
                    if (s.Renderer != null) Destroy(s.Renderer.gameObject);
                    _active.RemoveAt(i);
                    continue;
                }
                float t = s.Age / s.Duration;
                var c = s.Color;
                c.a = s.Color.a * (1f - t * t);
                s.Renderer.color = c;
                _active[i] = s;
            }
        }
    }
}

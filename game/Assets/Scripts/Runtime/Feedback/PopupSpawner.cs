using System.Collections.Generic;
using GMR.Core.Feedback;
using UnityEngine;

namespace GMR.Runtime.Feedback
{
    /// <summary>
    /// Floating text popups ("PERFECT!", "AVOIDED", ...) on a screen-space overlay canvas, anchored to world positions.
    /// Animates on unscaled time so popups stay readable during slow-mo and pause.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class PopupSpawner : MonoBehaviour
    {
        [SerializeField] Camera _camera;
        [SerializeField, Tooltip("Parent for spawned popups (under a Screen Space - Overlay canvas).")] RectTransform _container;
        [SerializeField, Tooltip("Inactive TextMeshPro template cloned for every popup.")] TMPro.TMP_Text _template;
        [SerializeField] FeedbackConfig _config;

        struct Popup
        {
            public TMPro.TMP_Text Text;
            public Vector2 World;
            public float Age;
            public Color Color;
        }

        readonly List<Popup> _active = new List<Popup>(8);

        public void Show(string message, Vector2 worldPosition, Color color)
        {
            if (_template == null || _container == null) return;
            var text = Instantiate(_template, _container);
            text.gameObject.SetActive(true);
            text.text = message;
            text.color = color;
            _active.Add(new Popup { Text = text, World = worldPosition, Age = 0f, Color = color });
            Place(_active[_active.Count - 1]);
        }

        void LateUpdate()
        {
            float duration = _config != null ? _config.PopupDuration : 0.7f;
            float dt = GMR.Runtime.Encounter.FrameClock.UnscaledDeltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var p = _active[i];
                p.Age += dt;
                if (p.Age >= duration || p.Text == null)
                {
                    if (p.Text != null) Destroy(p.Text.gameObject);
                    _active.RemoveAt(i);
                    continue;
                }
                float t = p.Age / duration;
                var c = p.Color;
                c.a = p.Color.a * (1f - t * t);
                p.Text.color = c;
                _active[i] = p;
                Place(p);
            }
        }

        void Place(in Popup p)
        {
            if (_camera == null || p.Text == null) return;
            float duration = _config != null ? _config.PopupDuration : 0.7f;
            float rise = _config != null ? _config.PopupRise : 0.8f;
            Vector2 world = p.World + new Vector2(0f, 0.6f + rise * Mathf.Clamp01(p.Age / duration));
            p.Text.rectTransform.position = _camera.WorldToScreenPoint(world);
        }

        void OnDisable()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Text != null) Destroy(_active[i].Text.gameObject);
            }
            _active.Clear();
        }
    }
}

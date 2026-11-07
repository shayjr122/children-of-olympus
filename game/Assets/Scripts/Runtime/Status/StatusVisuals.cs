using GMR.Core.Combat;
using GMR.Core.Status;
using UnityEngine;

namespace GMR.Runtime.Status
{
    /// <summary>
    /// Placeholder status readability (FR-18, NFR-6): tints the body toward the strongest active status (highest tint
    /// priority, then the latest applied) and shows one icon per active status in a row above the entity, colored with the
    /// status tint; an icon blinks in its last second (last 30% for short statuses). Read-only view of the IDamageable's StatusContainer. Runs before
    /// HitFlash (+110), which blends hit flashes over this tint.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class StatusVisuals : MonoBehaviour
    {
        [SerializeField, Tooltip("Body renderer to tint.")] SpriteRenderer _body;
        [SerializeField, Tooltip("Icon slots above the entity (shown left to right).")] SpriteRenderer[] _icons = System.Array.Empty<SpriteRenderer>();
        [SerializeField, Range(0f, 1f), Tooltip("How far the body color moves toward the status tint.")] float _tintStrength = 0.7f;
        [SerializeField, Min(0f), Tooltip("Distance between icon centers, units.")] float _iconSpacing = 0.42f;
        [SerializeField, Tooltip("Local position of the icon row's center.")] Vector2 _iconRowCenter = new Vector2(0f, 1.0f);

        IDamageable _target;
        Color _baseColor = Color.white;
        Sprite[] _defaultSprites;

        /// <summary>The body color this component wrote last frame (tests).</summary>
        public Color CurrentTint { get; private set; }

        /// <summary>Number of icons shown (tests).</summary>
        public int VisibleIcons { get; private set; }

        void Awake()
        {
            _target = GetComponent<IDamageable>();
            if (_body != null) _baseColor = _body.color;
            CurrentTint = _baseColor;
            _defaultSprites = new Sprite[_icons.Length];
            for (int i = 0; i < _icons.Length; i++)
            {
                if (_icons[i] == null) continue;
                _defaultSprites[i] = _icons[i].sprite;
                _icons[i].enabled = false;
            }
        }

        void LateUpdate()
        {
            var statuses = _target?.Statuses;
            int count = statuses != null && _target.IsAlive ? statuses.Count : 0;
            StatusInstance strongest = null;
            for (int i = 0; i < count; i++)
            {
                var s = statuses.Instances[i];
                if (strongest == null || s.Definition.TintPriority > strongest.Definition.TintPriority ||
                    (s.Definition.TintPriority == strongest.Definition.TintPriority && s.LastAppliedAt >= strongest.LastAppliedAt))
                    strongest = s;
            }

            Color color = _baseColor;
            if (strongest != null)
            {
                color = Color.Lerp(_baseColor, strongest.Definition.Tint, _tintStrength);
                color.a = _baseColor.a;
            }
            if (_body != null) _body.color = color;
            CurrentTint = color;

            int shown = Mathf.Min(count, _icons.Length);
            float start = -(shown - 1) * 0.5f * _iconSpacing;
            for (int i = 0; i < _icons.Length; i++)
            {
                var icon = _icons[i];
                if (icon == null) continue;
                bool show = i < shown;
                if (icon.enabled != show) icon.enabled = show;
                if (!show) continue;
                var instance = statuses.Instances[i];
                var def = instance.Definition;
                icon.sprite = def.Icon != null ? def.Icon : _defaultSprites[i];
                var c = def.Tint;
                float blinkFrom = Mathf.Min(1f, def.Duration * 0.3f); // last second, or last 30% of a short status
                if (instance.Remaining < blinkFrom && Mathf.Repeat(instance.Remaining * 8f, 1f) < 0.5f) c.a *= 0.35f;
                icon.color = c;
                icon.transform.localPosition = new Vector3(_iconRowCenter.x + start + i * _iconSpacing, _iconRowCenter.y, 0f);
            }
            VisibleIcons = shown;
        }

        void OnDisable()
        {
            if (_body != null) _body.color = _baseColor;
            for (int i = 0; i < _icons.Length; i++)
            {
                if (_icons[i] != null) _icons[i].enabled = false;
            }
            VisibleIcons = 0;
        }
    }
}

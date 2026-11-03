using GMR.Core.Combat;
using UnityEngine;

namespace GMR.Runtime.UI
{
    /// <summary>
    /// World-space placeholder HP bar (FR-27/FR-28 placeholder): scales a fill sprite by Health.Fraction of the
    /// IDamageable on <see cref="_target"/> and hides while the target is dead. Read-only view.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class HealthBarView : MonoBehaviour
    {
        [SerializeField, Tooltip("Component implementing IDamageable (e.g. TrainingDummy).")] Component _target;
        [SerializeField] SpriteRenderer _background;
        [SerializeField, Tooltip("Left-anchored fill; X scale = width x HP fraction.")] SpriteRenderer _fill;
        [SerializeField, Min(0.01f)] float _width = 1f;

        IDamageable _damageable;

        void Awake() => _damageable = _target as IDamageable;

        void LateUpdate()
        {
            if (_damageable == null || _fill == null) return;
            bool show = _damageable.IsAlive && _damageable.Health != null;
            if (_fill.enabled != show) _fill.enabled = show;
            if (_background != null && _background.enabled != show) _background.enabled = show;
            if (!show) return;

            float f = Mathf.Clamp01(_damageable.Health.Fraction);
            var t = _fill.transform;
            var s = t.localScale;
            s.x = _width * f;
            t.localScale = s;
            var p = t.localPosition;
            p.x = -_width * 0.5f * (1f - f); // keep the left edge fixed
            t.localPosition = p;
        }
    }
}

using GMR.Runtime.Heroes;
using UnityEngine;

namespace GMR.Runtime.Players
{
    /// <summary>
    /// Placeholder aim arrow (FR-2), always visible: points along the owner's Aim Direction, turns yellow while
    /// Direction Lock is active (FR-4), and a marker shows which enemy Aim Assist would snap to (FR-3).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class AimIndicator : MonoBehaviour
    {
        [SerializeField] HeroRuntime _hero;
        [SerializeField] SpriteRenderer _arrow;
        [SerializeField, Tooltip("Shown on the enemy Aim Assist would snap to.")] SpriteRenderer _assistMarker;
        [SerializeField, Min(0f)] float _arrowDistance = 0.85f;
        [SerializeField] Color _normalColor = Color.white;
        [SerializeField] Color _lockedColor = new Color(1f, 0.9f, 0.1f, 1f);

        void LateUpdate()
        {
            if (_hero == null || !_hero.IsInitialized || _hero.Owner == null || !_hero.Owner.IsInitialized) return;
            var owner = _hero.Owner;
            Vector2 direction = owner.Aim.Direction;

            if (_arrow != null)
            {
                Vector2 origin = _hero.Position;
                _arrow.transform.position = origin + direction * _arrowDistance;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                _arrow.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                _arrow.color = owner.Aim.IsLocked ? _lockedColor : _normalColor;
            }

            if (_assistMarker != null)
            {
                bool show = owner.TryGetAssistTarget(out Vector2 target);
                if (_assistMarker.enabled != show) _assistMarker.enabled = show;
                if (show) _assistMarker.transform.position = target;
            }
        }
    }
}

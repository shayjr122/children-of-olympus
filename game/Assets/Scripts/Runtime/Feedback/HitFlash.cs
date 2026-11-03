using UnityEngine;

namespace GMR.Runtime.Feedback
{
    /// <summary>
    /// Flashes a SpriteRenderer toward a color for a short real-time duration (FR-27). Runs after other visuals (+110)
    /// and blends over whatever color they set this frame, then hands the color back when the flash ends.
    /// Triggered only by FeedbackRouter.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public sealed class HitFlash : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _renderer;

        Color _flashColor;
        Color _unflashed;
        Color _lastWritten;
        float _duration;
        float _remaining;
        bool _wrote;

        public bool IsFlashing => _remaining > 0f;

        public void Flash(Color color, float duration)
        {
            if (_renderer == null || duration <= 0f) return;
            if (!_wrote) _unflashed = _renderer.color;
            _flashColor = color;
            _duration = duration;
            _remaining = duration;
        }

        void LateUpdate()
        {
            if (_renderer == null) return;
            // Someone else (HeroVisuals) wrote the color since our last write: that is the new base color.
            if (!_wrote || _renderer.color != _lastWritten) _unflashed = _renderer.color;

            if (_remaining <= 0f)
            {
                if (_wrote)
                {
                    _renderer.color = _unflashed;
                    _wrote = false;
                }
                return;
            }

            _remaining -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_remaining / Mathf.Max(_duration, 0.0001f));
            var c = Color.Lerp(_unflashed, _flashColor, k);
            c.a = _unflashed.a;
            _renderer.color = c;
            _lastWritten = c;
            _wrote = true;
        }

        void OnDisable()
        {
            if (_wrote && _renderer != null) _renderer.color = _unflashed;
            _wrote = false;
            _remaining = 0f;
        }
    }
}

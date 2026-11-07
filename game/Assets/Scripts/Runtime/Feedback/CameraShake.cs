using GMR.Core.Rng;
using UnityEngine;

namespace GMR.Runtime.Feedback
{
    /// <summary>
    /// Placeholder screen shake (FR-27): offsets the camera around its rest position for a short real-time duration,
    /// decaying linearly. Jitter comes from the seeded Feedback RNG stream (AD-2), never UnityEngine.Random.
    /// Triggered only by FeedbackRouter.
    /// </summary>
    [DefaultExecutionOrder(120)]
    public sealed class CameraShake : MonoBehaviour
    {
        Vector3 _rest;
        IRng _rng;
        float _amplitude;
        float _duration;
        float _remaining;

        public bool IsShaking => _remaining > 0f;

        void Awake() => _rest = transform.localPosition;

        /// <summary>Starts (or strengthens) a shake. <paramref name="rng"/> is the Feedback stream.</summary>
        public void Shake(float amplitude, float duration, IRng rng)
        {
            if (amplitude <= 0f || duration <= 0f || rng == null) return;
            _rng = rng;
            if (_remaining <= 0f || amplitude >= CurrentAmplitude)
            {
                _amplitude = amplitude;
                _duration = duration;
                _remaining = duration;
            }
        }

        float CurrentAmplitude => _remaining > 0f ? _amplitude * (_remaining / Mathf.Max(_duration, 0.0001f)) : 0f;

        void LateUpdate()
        {
            if (_remaining <= 0f) return;
            _remaining -= GMR.Runtime.Encounter.FrameClock.UnscaledDeltaTime;
            if (_remaining <= 0f)
            {
                transform.localPosition = _rest;
                return;
            }
            float a = CurrentAmplitude;
            transform.localPosition = _rest + new Vector3(_rng.Range(-a, a), _rng.Range(-a, a), 0f);
        }

        void OnDisable()
        {
            _remaining = 0f;
            transform.localPosition = _rest;
        }
    }
}

using GMR.Core.Combat;
using GMR.Core.Encounter;
using GMR.Core.Feedback;
using GMR.Runtime.Encounter;
using UnityEngine;

namespace GMR.Runtime.Feedback
{
    /// <summary>
    /// Perfect Dodge feedback (FR-7): screen flash, "PERFECT!" popup and a short time-slow requested through
    /// TimeScaleService. Subscribes to CombatEvents only; gameplay never calls it (feedback convention). Flash and
    /// slow are individually toggleable (FeedbackConfig default or pause-menu override).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class PerfectDodgeFeedback : MonoBehaviour
    {
        [SerializeField] EncounterBootstrap _bootstrap;
        [SerializeField] FeedbackConfig _config;
        [SerializeField, Tooltip("Full-screen overlay image used for the flash (alpha animated).")] UnityEngine.UI.Image _flash;
        [SerializeField] PopupSpawner _popups;
        [SerializeField] Color _popupColor = new Color(1f, 0.95f, 0.35f, 1f);

        CombatEvents _events;
        float _flashRemaining;

        void OnEnable()
        {
            if (_bootstrap == null || _config == null) return;
            _events = _bootstrap.Context.Events;
            _events.OnPerfectDodged += OnPerfectDodged;
            SetFlashAlpha(0f);
        }

        void OnDisable()
        {
            if (_events != null) _events.OnPerfectDodged -= OnPerfectDodged;
            _events = null;
            SetFlashAlpha(0f);
        }

        void OnPerfectDodged(PerfectDodged e)
        {
            var context = _bootstrap.Context;
            if (_popups != null) _popups.Show("PERFECT!", e.Position, _popupColor);

            if (SessionToggles.Resolve(context.Toggles.PerfectDodgeFlash, _config.PerfectDodgeFlash))
                _flashRemaining = _config.FlashDuration;

            if (SessionToggles.Resolve(context.Toggles.PerfectDodgeTimeSlow, _config.PerfectDodgeTimeSlow))
                context.TimeScale.Request(_config.TimeSlowScale, _config.TimeSlowDuration, context.TimeScale.UnscaledNow);
        }

        void Update()
        {
            if (_flashRemaining <= 0f) return;
            _flashRemaining -= FrameClock.UnscaledDeltaTime;
            float duration = Mathf.Max(_config.FlashDuration, 0.0001f);
            SetFlashAlpha(_config.FlashMaxAlpha * Mathf.Clamp01(_flashRemaining / duration));
        }

        void SetFlashAlpha(float alpha)
        {
            if (_flash == null) return;
            var c = _flash.color;
            c.a = alpha;
            _flash.color = c;
        }
    }
}

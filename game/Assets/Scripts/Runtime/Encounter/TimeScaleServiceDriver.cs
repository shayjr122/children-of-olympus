using UnityEngine;

namespace GMR.Runtime.Encounter
{
    /// <summary>
    /// Shell of TimeScaleService (AD-12): the ONLY code in the project that writes Time.timeScale. Every frame it
    /// advances the service's unscaled clock by <see cref="FrameClock.UnscaledDeltaTime"/> and copies the resolved scale
    /// (lowest request wins). Restores Time.timeScale to 1 when disabled.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class TimeScaleServiceDriver : MonoBehaviour
    {
        [SerializeField] EncounterBootstrap _bootstrap;

        void Update()
        {
            if (_bootstrap == null) return;
            Apply(_bootstrap.Context.TimeScale.Advance(FrameClock.UnscaledDeltaTime));
        }

        /// <summary>Resolves at the current clock and applies immediately (used when pausing so gameplay stops this frame).</summary>
        public void ApplyNow()
        {
            if (_bootstrap == null) return;
            var service = _bootstrap.Context.TimeScale;
            Apply(service.Resolve(service.UnscaledNow));
        }

        static void Apply(float scale)
        {
            if (!Mathf.Approximately(Time.timeScale, scale)) Time.timeScale = scale;
        }

        // Requests belong to their callers (AD-12); only the engine value is restored here.
        void OnDisable() => Time.timeScale = 1f;
    }
}

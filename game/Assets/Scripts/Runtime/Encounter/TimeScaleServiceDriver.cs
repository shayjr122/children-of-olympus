using UnityEngine;

namespace GMR.Runtime.Encounter
{
    /// <summary>
    /// Shell of TimeScaleService (AD-12): the ONLY code in the project that writes Time.timeScale. Every frame it
    /// copies the service's resolved scale (lowest request wins). Restores Time.timeScale to 1 when disabled.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class TimeScaleServiceDriver : MonoBehaviour
    {
        [SerializeField] EncounterBootstrap _bootstrap;

        void Update() => ApplyNow();

        /// <summary>Resolves and applies immediately (used when pausing so gameplay stops this frame).</summary>
        public void ApplyNow()
        {
            if (_bootstrap == null) return;
            float scale = _bootstrap.Context.TimeScale.Resolve(Time.unscaledTime);
            if (!Mathf.Approximately(Time.timeScale, scale)) Time.timeScale = scale;
        }

        // Requests belong to their callers (AD-12); only the engine value is restored here.
        void OnDisable() => Time.timeScale = 1f;
    }
}

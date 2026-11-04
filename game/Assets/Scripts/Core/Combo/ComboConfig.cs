using UnityEngine;

namespace GMR.Core.Combo
{
    /// <summary>
    /// Combo tuning (FR-13, FR-14, NFR-4): fixed window, shrinking curve and its minimum. One asset shared by all
    /// Players; read live by every ComboCounter (the pause menu can override the mode per session).
    /// </summary>
    [CreateAssetMenu(menuName = "GMR/Config/Combo Config", fileName = "Config_Combo")]
    public sealed class ComboConfig : ScriptableObject
    {
        [SerializeField, Tooltip("Default mode. The pause-menu toggle 'Shrinking Combo window' overrides it per session.")]
        ComboMode _mode = ComboMode.Fixed;

        [SerializeField, Min(0.05f), Tooltip("Combo Window in Fixed mode, seconds (scaled time).")]
        float _fixedWindow = 2.0f;

        [SerializeField, Tooltip("Shrinking mode: window in seconds (Y) over the Combo count (X). Default 2.0 s at 0 to 1.0 s at 50.")]
        AnimationCurve _shrinkingCurve = AnimationCurve.Linear(0f, 2.0f, 50f, 1.0f);

        [SerializeField, Min(0.05f), Tooltip("Shrinking mode never goes below this window, seconds.")]
        float _minWindow = 0.8f;

        public ComboMode Mode { get => _mode; internal set => _mode = value; }
        public float FixedWindow { get => _fixedWindow; internal set => _fixedWindow = value; }
        public AnimationCurve ShrinkingCurve { get => _shrinkingCurve; internal set => _shrinkingCurve = value; }
        public float MinWindow { get => _minWindow; internal set => _minWindow = value; }

        /// <summary>The window granted by a hit that brought the Combo to <paramref name="count"/>, in seconds.</summary>
        public float WindowFor(int count, ComboMode mode)
        {
            if (mode == ComboMode.Fixed || _shrinkingCurve == null || _shrinkingCurve.length == 0)
                return Mathf.Max(0.05f, _fixedWindow);
            return Mathf.Max(_minWindow, _shrinkingCurve.Evaluate(count));
        }
    }
}

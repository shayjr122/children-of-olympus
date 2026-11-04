using GMR.Core.Combat;
using GMR.Core.Encounter;

namespace GMR.Core.Feedback
{
    /// <summary>
    /// When a landed hit asks TimeScaleService for hitstop (FR-27, AD-12): only if hitstop is enabled (config default
    /// or pause-menu override) and the duration for the hit's weight is above 0. Hitstop 0 = no request at all.
    /// </summary>
    public static class HitstopRule
    {
        /// <summary>Requests hitstop if the rule allows it; returns true if a request was made.</summary>
        public static bool TryRequest(TimeScaleService timeScale, FeedbackConfig config, bool enabled, HitWeight weight,
            float nowUnscaled) => TryRequest(timeScale, config, enabled, weight, DamageTags.None, nowUnscaled);

        /// <summary>As above; hits tagged Ultimate use the Ultimate hitstop.</summary>
        public static bool TryRequest(TimeScaleService timeScale, FeedbackConfig config, bool enabled, HitWeight weight,
            DamageTags tags, float nowUnscaled)
        {
            if (timeScale == null || config == null || !enabled) return false;
            float duration = config.HitstopDuration(weight, tags);
            if (!(duration > 0f)) return false;
            timeScale.Request(config.HitstopScale, duration, nowUnscaled);
            return true;
        }
    }
}

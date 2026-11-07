using UnityEngine;

namespace GMR.Runtime.Encounter
{
    /// <summary>
    /// Unscaled time for game feel (hitstop, slow-mo, popups, flashes, shake). Normally the real frame delta; when frames
    /// are captured at a fixed rate (Time.captureDeltaTime: video capture, deterministic PlayMode tests) every frame
    /// stands for exactly that much presented time, so unscaled timers advance in step with scaled time instead of the
    /// wall clock (which would make a 0.03 s hitstop span 2 frames on one machine and 100 on another).
    /// </summary>
    public static class FrameClock
    {
        /// <summary>This frame's unscaled delta in seconds (capture delta when capturing, else Time.unscaledDeltaTime).</summary>
        public static float UnscaledDeltaTime => Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
    }
}

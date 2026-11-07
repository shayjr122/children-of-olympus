using System;
using System.Collections.Generic;

namespace GMR.Core.Encounter
{
    /// <summary>
    /// The single owner of the game's time scale (AD-12). Callers push requests; the lowest active scale wins
    /// (pause = 0 beats a 0.3 slow-mo beats a 1.0 default). Timed requests expire on UNSCALED time, so a pause or
    /// slow-mo never freezes its own expiry. The Runtime TimeScaleServiceDriver is the only code that copies
    /// <see cref="Resolve"/> into UnityEngine.Time.timeScale, and the only code that advances <see cref="UnscaledNow"/>
    /// (once per frame, by the frame's presentation delta), so every requester in a frame stamps the same clock.
    /// </summary>
    public sealed class TimeScaleService
    {
        public const float DefaultScale = 1f;

        struct ScaleRequest
        {
            public int Handle;
            public float Scale;
            public float EndUnscaled; // +inf for held requests
        }

        readonly List<ScaleRequest> _requests = new List<ScaleRequest>(4);
        int _nextHandle = 1;

        /// <summary>
        /// The Encounter's unscaled clock in seconds, advanced once per frame by <see cref="Advance"/>. Requesters pass it
        /// as <c>nowUnscaled</c>. It counts presented frames, not the wall clock, when frames are captured at a fixed rate.
        /// </summary>
        public float UnscaledNow { get; private set; }

        /// <summary>Advances <see cref="UnscaledNow"/> by <paramref name="unscaledDelta"/> (negative/NaN ignored) and resolves.</summary>
        public float Advance(float unscaledDelta)
        {
            if (unscaledDelta > 0f) UnscaledNow += unscaledDelta;
            return Resolve(UnscaledNow);
        }

        /// <summary>Scale from the last <see cref="Resolve"/>.</summary>
        public float Current { get; private set; } = DefaultScale;

        public int ActiveCount => _requests.Count;

        /// <summary>Requests <paramref name="scale"/> for <paramref name="durationUnscaled"/> real seconds from <paramref name="nowUnscaled"/>.</summary>
        public int Request(float scale, float durationUnscaled, float nowUnscaled)
        {
            if (float.IsNaN(scale)) throw new ArgumentException("Scale must be a number.", nameof(scale));
            int handle = _nextHandle++;
            if (durationUnscaled <= 0f) return handle; // no-op request, nothing to hold
            _requests.Add(new ScaleRequest { Handle = handle, Scale = Math.Max(0f, scale), EndUnscaled = nowUnscaled + durationUnscaled });
            return handle;
        }

        /// <summary>Holds <paramref name="scale"/> until <see cref="Release"/> (e.g. pause = 0).</summary>
        public int Hold(float scale)
        {
            if (float.IsNaN(scale)) throw new ArgumentException("Scale must be a number.", nameof(scale));
            int handle = _nextHandle++;
            _requests.Add(new ScaleRequest { Handle = handle, Scale = Math.Max(0f, scale), EndUnscaled = float.PositiveInfinity });
            return handle;
        }

        public bool Release(int handle)
        {
            for (int i = 0; i < _requests.Count; i++)
            {
                if (_requests[i].Handle != handle) continue;
                _requests.RemoveAt(i);
                return true;
            }
            return false;
        }

        /// <summary>Drops expired requests and returns the lowest active scale (1 if none).</summary>
        public float Resolve(float nowUnscaled)
        {
            float lowest = DefaultScale;
            for (int i = _requests.Count - 1; i >= 0; i--)
            {
                if (nowUnscaled >= _requests[i].EndUnscaled)
                {
                    _requests.RemoveAt(i);
                    continue;
                }
                if (_requests[i].Scale < lowest) lowest = _requests[i].Scale;
            }
            Current = lowest;
            return lowest;
        }

        public void Clear()
        {
            _requests.Clear();
            Current = DefaultScale;
        }
    }
}

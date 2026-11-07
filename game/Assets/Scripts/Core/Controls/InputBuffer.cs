using System;

namespace GMR.Core.Controls
{
    /// <summary>
    /// One Player's press buffer (FR-8, AD-7). PlayerInputReader pushes presses; gameplay calls TryConsume when the
    /// action is legal. A press is usable for <see cref="Window"/> seconds after it happened and is consumed once.
    /// With a window of 0 a press is only usable in the frame it happened (buffering disabled).
    /// </summary>
    public sealed class InputBuffer
    {
        static readonly int ActionCount = Enum.GetValues(typeof(BufferedAction)).Length;

        readonly InputConfig _config;
        readonly bool[] _pending = new bool[ActionCount];
        readonly float[] _pressTime = new float[ActionCount];

        public InputBuffer(InputConfig config)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
        }

        /// <summary>Effective on/off for this session (config default or pause-menu override). Set by the owner.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Buffer window in seconds, read live from InputConfig; 0 when disabled.</summary>
        public float Window => Enabled ? Math.Max(0f, _config.BufferWindow) : 0f;

        /// <summary>Records a press at scaled time <paramref name="now"/> (a newer press replaces an older one).</summary>
        public void Press(BufferedAction action, float now)
        {
            int i = (int)action;
            _pending[i] = true;
            _pressTime[i] = now;
        }

        /// <summary>True if a press for <paramref name="action"/> is still usable at <paramref name="now"/>.</summary>
        public bool IsBuffered(BufferedAction action, float now)
        {
            int i = (int)action;
            return _pending[i] && now - _pressTime[i] <= Window;
        }

        /// <summary>
        /// Consumes the press if it is still inside the window. Expired presses are dropped. Returns true at most
        /// once per press.
        /// </summary>
        public bool TryConsume(BufferedAction action, float now)
        {
            int i = (int)action;
            if (!_pending[i]) return false;
            _pending[i] = false;
            return now - _pressTime[i] <= Window;
        }

        public void Clear()
        {
            for (int i = 0; i < ActionCount; i++) _pending[i] = false;
        }
    }
}

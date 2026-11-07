using System;
using UnityEngine;

namespace GMR.Core.Heroes
{
    /// <summary>
    /// One Hero's DASH cycle as pure state (FR-6): Ready -> Dashing (duration) -> Recovery (recovery) -> Ready.
    /// No cooldown; a new DASH is refused while Dashing or in Dash Recovery. Times are scaled seconds from the shell.
    /// </summary>
    public sealed class DashState
    {
        float _dashEnd;
        float _recoveryEnd;
        float _recoveryDuration;
        float _speed;

        public DashPhase Phase { get; private set; } = DashPhase.Ready;

        /// <summary>Normalized direction of the current or last DASH.</summary>
        public Vector2 Direction { get; private set; } = Vector2.right;

        /// <summary>Scaled time the current or last DASH started; NaN before the first DASH.</summary>
        public float StartTime { get; private set; } = float.NaN;

        public bool IsDashing => Phase == DashPhase.Dashing;

        /// <summary>Velocity while Dashing (direction x distance / duration), zero otherwise.</summary>
        public Vector2 Velocity => Phase == DashPhase.Dashing ? Direction * _speed : Vector2.zero;

        /// <summary>Advances phases to <paramref name="now"/>.</summary>
        public void Tick(float now)
        {
            if (Phase == DashPhase.Dashing && now >= _dashEnd) Phase = DashPhase.Recovery;
            if (Phase == DashPhase.Recovery && now >= _recoveryEnd) Phase = DashPhase.Ready;
        }

        /// <summary>True if a DASH may start at <paramref name="now"/> (ticks first).</summary>
        public bool CanStart(float now)
        {
            Tick(now);
            return Phase == DashPhase.Ready;
        }

        /// <summary>
        /// Starts a DASH along <paramref name="direction"/> if Ready. Config values are snapshotted here.
        /// A zero direction falls back to the last DASH direction.
        /// </summary>
        public bool TryStart(Vector2 direction, float now, DashConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (!CanStart(now)) return false;

            if (direction.sqrMagnitude > 1e-8f) Direction = direction.normalized;
            StartTime = now;
            _speed = config.Speed;
            _dashEnd = now + config.Duration;
            _recoveryDuration = config.RecoveryDuration;
            _recoveryEnd = _dashEnd + _recoveryDuration;
            Phase = DashPhase.Dashing;
            return true;
        }

        /// <summary>Remaining Dash Recovery as 1 (just started) .. 0 (over); 0 outside Recovery.</summary>
        public float RecoveryRemaining01(float now)
        {
            if (Phase != DashPhase.Recovery || _recoveryDuration <= 0f) return 0f;
            return Mathf.Clamp01((_recoveryEnd - now) / _recoveryDuration);
        }

        /// <summary>Cancels any DASH and Recovery (e.g. on Hero rebuild).</summary>
        public void Reset()
        {
            Phase = DashPhase.Ready;
            StartTime = float.NaN;
        }
    }
}

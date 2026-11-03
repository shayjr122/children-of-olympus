using System;
using System.Collections.Generic;
using UnityEngine;

namespace GMR.Core.Controls
{
    /// <summary>
    /// One Player's Aim Direction (FR-2..FR-4, AD-7). Aim follows the last non-zero movement direction; Direction
    /// Lock freezes it while held; Aim Assist bends only the direction resolved for firing.
    /// </summary>
    public sealed class AimResolver
    {
        const float MinSqr = 1e-8f;

        Vector2 _direction;

        public AimResolver() : this(Vector2.right) { }

        public AimResolver(Vector2 initialDirection)
        {
            _direction = initialDirection.sqrMagnitude > MinSqr ? initialDirection.normalized : Vector2.right;
        }

        /// <summary>Current Aim Direction (always normalized).</summary>
        public Vector2 Direction => _direction;

        /// <summary>True while Direction Lock is active (feature enabled and button held).</summary>
        public bool IsLocked { get; private set; }

        /// <summary>
        /// Per-frame update. <paramref name="move"/> is the dead-zoned movement input. While
        /// <paramref name="lockEnabled"/> and <paramref name="lockHeld"/>, the direction stays at its value from
        /// when the lock began; otherwise any non-zero move becomes the new direction. With the feature disabled
        /// the button does nothing.
        /// </summary>
        public void Update(Vector2 move, bool lockHeld, bool lockEnabled)
        {
            if (lockEnabled && lockHeld)
            {
                IsLocked = true;
                return;
            }

            IsLocked = false;
            if (move.sqrMagnitude > MinSqr) _direction = move.normalized;
        }

        /// <summary>
        /// Direction to fire along: with assist on, the direction to the nearest candidate inside the cone, else the
        /// current Aim Direction (FR-3).
        /// </summary>
        public Vector2 ResolveFire(Vector2 origin, IReadOnlyList<Vector2> candidates, AimConfig config, bool assistOn)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (!assistOn || candidates == null) return _direction;
            return TryFindAssistTarget(origin, _direction, candidates, config.AssistHalfAngle, config.AssistRange, out int index)
                ? (candidates[index] - origin).normalized
                : _direction;
        }

        /// <summary>
        /// Index of the nearest candidate within <paramref name="range"/> and at most
        /// <paramref name="halfAngleDeg"/> off <paramref name="aim"/>. Ties keep the lowest index (deterministic).
        /// </summary>
        public static bool TryFindAssistTarget(Vector2 origin, Vector2 aim, IReadOnlyList<Vector2> candidates,
            float halfAngleDeg, float range, out int index)
        {
            index = -1;
            if (candidates == null || aim.sqrMagnitude <= MinSqr) return false;

            Vector2 aimN = aim.normalized;
            float cosLimit = Mathf.Cos(Mathf.Clamp(halfAngleDeg, 0f, 180f) * Mathf.Deg2Rad);
            float bestSqr = range * range;
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector2 offset = candidates[i] - origin;
                float sqr = offset.sqrMagnitude;
                if (sqr <= MinSqr || sqr > bestSqr) continue;
                if (sqr == bestSqr && index >= 0) continue;
                float cos = Vector2.Dot(aimN, offset / Mathf.Sqrt(sqr));
                if (cos + 1e-6f < cosLimit) continue;
                bestSqr = sqr;
                index = i;
            }
            return index >= 0;
        }
    }
}

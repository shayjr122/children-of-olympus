using System;
using UnityEngine;

namespace GMR.Core.Heroes
{
    /// <summary>Pure movement math (FR-1): radial dead-zone and linear acceleration toward a target velocity.</summary>
    public static class MoveMath
    {
        /// <summary>
        /// Radial dead-zone with rescale: magnitude at or below <paramref name="deadZone"/> gives zero; above it,
        /// magnitude is remapped from (deadZone, 1] to (0, 1] so full deflection still means full speed.
        /// </summary>
        public static Vector2 ApplyDeadZone(Vector2 raw, float deadZone)
        {
            float magnitude = raw.magnitude;
            if (magnitude <= 0f || magnitude <= deadZone) return Vector2.zero;
            float clamped = Mathf.Min(magnitude, 1f);
            float scaled = deadZone >= 1f ? 1f : (clamped - deadZone) / (1f - deadZone);
            return raw / magnitude * scaled;
        }

        /// <summary>Target velocity for a raw stick value.</summary>
        public static Vector2 TargetVelocity(Vector2 rawStick, MoveConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            return ApplyDeadZone(rawStick, config.DeadZone) * config.MaxSpeed;
        }

        /// <summary>
        /// One step toward <paramref name="target"/>. Speeding up uses AccelerationTime (rest to max speed), slowing
        /// down uses DecelerationTime; a time of 0 snaps.
        /// </summary>
        public static Vector2 Step(Vector2 current, Vector2 target, MoveConfig config, float dt)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            bool speedingUp = target.sqrMagnitude >= current.sqrMagnitude;
            float rampTime = speedingUp ? config.AccelerationTime : config.DecelerationTime;
            return Step(current, target, config.MaxSpeed, rampTime, dt);
        }

        /// <summary>Moves velocity toward target at maxSpeed/rampTime units/s².</summary>
        public static Vector2 Step(Vector2 current, Vector2 target, float maxSpeed, float rampTime, float dt)
        {
            if (rampTime <= 0f) return target;
            float maxDelta = Mathf.Max(maxSpeed, 0.0001f) / rampTime * Mathf.Max(dt, 0f);
            return Vector2.MoveTowards(current, target, maxDelta);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace GMR.Core.Abilities
{
    /// <summary>
    /// Projectile tuning for Shot abilities (FR-9, AD-8). Read live when a projectile is spawned and while it flies.
    /// </summary>
    [CreateAssetMenu(menuName = "GMR/Config/Projectile Config", fileName = "Config_Projectile_")]
    public sealed class ProjectileConfig : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip("Travel speed in units/s.")]
        float _speed = 14f;

        [SerializeField, Min(0f), Tooltip("Seconds before the projectile disappears (range = speed x lifetime).")]
        float _lifetime = 0.8f;

        [SerializeField, Min(0f), Tooltip("Collision radius in units (also the placeholder size).")]
        float _radius = 0.2f;

        [SerializeField, Min(0), Tooltip("Extra targets it passes through; 0 = stops at the first target hit.")]
        int _pierce;

        [SerializeField, Tooltip("Placeholder color.")]
        Color _color = new Color(0.55f, 0.85f, 1f, 1f);

        public float Speed { get => _speed; internal set => _speed = value; }
        public float Lifetime { get => _lifetime; internal set => _lifetime = value; }
        public float Radius { get => _radius; internal set => _radius = value; }
        public int Pierce { get => _pierce; internal set => _pierce = value; }
        public Color Color { get => _color; internal set => _color = value; }

        /// <summary>Distance travelled before expiring, in units.</summary>
        public float MaxRange => _speed * _lifetime;

        /// <summary>Appends one message per invalid value to <paramref name="errors"/>; returns true if valid.</summary>
        public bool Validate(List<string> errors)
        {
            int before = errors.Count;
            if (!(_speed > 0f)) errors.Add($"{name}: speed must be > 0 (is {_speed}).");
            if (!(_lifetime > 0f)) errors.Add($"{name}: lifetime must be > 0 (is {_lifetime}).");
            if (!(_radius > 0f)) errors.Add($"{name}: radius must be > 0 (is {_radius}).");
            if (_pierce < 0) errors.Add($"{name}: pierce must be >= 0 (is {_pierce}).");
            return errors.Count == before;
        }

        void OnValidate()
        {
            var errors = new List<string>();
            if (!Validate(errors))
                UnityEngine.Debug.LogWarning("[GMR/Abilities] " + string.Join(" ", errors), this);
        }
    }
}

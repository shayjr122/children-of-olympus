using System.Collections.Generic;
using UnityEngine;

namespace GMR.Runtime.Abilities
{
    /// <summary>
    /// Shot (FR-9): when Active starts, spawns one projectile just outside the Hero along the fire direction. The
    /// projectile (trigger collider on the Projectile layer) does its own hits.
    /// </summary>
    public sealed class ProjectileHitResolver : IHitResolver
    {
        readonly Projectile _prefab;
        readonly LayerMask _targets;
        readonly LayerMask _walls;

        public ProjectileHitResolver(Projectile prefab, LayerMask targets, LayerMask walls)
        {
            _prefab = prefab;
            _targets = targets;
            _walls = walls;
        }

        public void Begin(AbilityCast cast, HitOrigin origin, float now)
        {
            var config = cast.Definition.Projectile;
            if (_prefab == null || config == null)
            {
                UnityEngine.Debug.LogError($"[GMR/Abilities] '{cast.Definition.name}' is a Projectile ability but the " +
                                           "projectile prefab or ProjectileConfig is missing.", cast.Definition);
                return;
            }
            var errors = new List<string>();
            if (!config.Validate(errors))
            {
                UnityEngine.Debug.LogError("[GMR/Abilities] Projectile not spawned: " + string.Join(" ", errors), config);
                return;
            }
            Vector2 spawn = origin.Position + cast.Direction * (origin.Radius + config.Radius + 0.05f);
            // Fired into an adjacent wall: the bolt fizzles instead of spawning beyond it.
            if (Physics2D.Linecast(origin.Position, spawn, _walls)) return;
            var projectile = Object.Instantiate(_prefab, spawn, Quaternion.identity);
            projectile.Launch(cast.Damage, cast.Source, cast.Definition, cast.Direction, cast.HitId, _targets, _walls, now);
        }

        public void Tick(AbilityCast cast, HitOrigin origin, float now) { }
    }
}

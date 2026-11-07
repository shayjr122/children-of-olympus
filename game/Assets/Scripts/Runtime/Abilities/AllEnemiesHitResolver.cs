using System.Collections.Generic;
using GMR.Core.Combat;
using GMR.Core.Enemies;

namespace GMR.Runtime.Abilities
{
    /// <summary>
    /// AllEnemies hit shape (FR-17, Wrath of Zeus): every living, damageable enemy registered in the Encounter, wherever
    /// it is. No physics query; knockback pushes away from the Hero. Each enemy is hit once per activation (AbilityCast).
    /// </summary>
    public sealed class AllEnemiesHitResolver : IHitResolver
    {
        readonly EnemyRegistry _enemies;
        readonly List<IDamageable> _targets = new List<IDamageable>(16);

        public AllEnemiesHitResolver(EnemyRegistry enemies)
        {
            _enemies = enemies;
        }

        public void Begin(AbilityCast cast, HitOrigin origin, float now) => Tick(cast, origin, now);

        public void Tick(AbilityCast cast, HitOrigin origin, float now)
        {
            if (_enemies == null) return;
            // Snapshot first: damage handlers (deaths, respawns) may change the registry while we iterate.
            _targets.Clear();
            var all = _enemies.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] is IDamageable damageable && damageable.IsAlive) _targets.Add(damageable);
            }
            for (int i = 0; i < _targets.Count; i++)
            {
                var target = _targets[i];
                var away = target.Position - origin.Position;
                cast.TryHit(target, away.sqrMagnitude > 1e-6f ? away : cast.Direction, now);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GMR.Core.Enemies
{
    /// <summary>Living enemies of the current Encounter, queried by Aim Assist and (later) targeting and chains.</summary>
    public sealed class EnemyRegistry
    {
        readonly List<IEnemyTarget> _enemies = new List<IEnemyTarget>(16);

        public int Count => _enemies.Count;
        public IReadOnlyList<IEnemyTarget> All => _enemies;

        public void Register(IEnemyTarget enemy)
        {
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));
            if (!_enemies.Contains(enemy)) _enemies.Add(enemy);
        }

        public bool Unregister(IEnemyTarget enemy) => _enemies.Remove(enemy);

        /// <summary>Clears <paramref name="into"/> and fills it with the positions of living enemies (no allocation).</summary>
        public void FillAlivePositions(List<Vector2> into)
        {
            if (into == null) throw new ArgumentNullException(nameof(into));
            into.Clear();
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].IsAlive) into.Add(_enemies[i].Position);
            }
        }
    }
}

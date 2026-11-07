using System;
using System.Collections.Generic;

namespace GMR.Core.Status
{
    /// <summary>
    /// The Encounter's status containers that StatusTicker ticks (AD-16: each exactly once per frame). Damageables
    /// register their container while enabled. A container is listed at most once.
    /// </summary>
    public sealed class StatusRegistry
    {
        readonly List<StatusContainer> _containers = new List<StatusContainer>(16);
        readonly List<StatusContainer> _snapshot = new List<StatusContainer>(16);

        public int Count => _containers.Count;
        public IReadOnlyList<StatusContainer> All => _containers;

        public void Register(StatusContainer container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (!_containers.Contains(container)) _containers.Add(container);
        }

        public bool Unregister(StatusContainer container) => _containers.Remove(container);

        /// <summary>Ticks every registered container once (on a snapshot, so (un)registering during a tick is safe).</summary>
        public void TickAll(float dt, float now)
        {
            _snapshot.Clear();
            _snapshot.AddRange(_containers);
            for (int i = 0; i < _snapshot.Count; i++) _snapshot[i].Tick(dt, now);
            _snapshot.Clear();
        }
    }
}

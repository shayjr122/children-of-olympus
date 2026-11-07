using System.Collections.Generic;
using GMR.Core.Combat;
using UnityEngine;

namespace GMR.Core.Status
{
    /// <summary>
    /// One chain of lightning (FR-19, AD-10, AD-16), created by <see cref="ChainLightningBehaviour"/> when Shock is applied.
    /// Holds the per-chain state: enemies visited (each hit at most once), jumps left, accumulated falloff. Each jump is a
    /// DamageRequest through DamageSystem (queued FIFO, never recursive) that carries this run in its status payload; when
    /// the jump lands, the target's fold has put its chain bonus (Wet) into the result, Shock is applied to it and
    /// <see cref="Arrived"/> continues from there. Ties between equally near candidates use the Status RNG stream.
    /// Sole raiser of ChainJumped.
    /// </summary>
    public sealed class ChainRun
    {
        struct Candidate
        {
            public IDamageable Target;
            public float Distance;
        }

        readonly StatusServices _services;
        readonly StatusDefinition _status;
        readonly DamageTags _inheritedTags;
        readonly HashSet<IDamageable> _visited = new HashSet<IDamageable>();
        readonly List<Candidate> _candidates = new List<Candidate>(8);
        readonly List<IDamageable> _tied = new List<IDamageable>(4);

        IDamageable _pendingTarget;
        IDamageable _from;
        Vector2 _fromPosition;
        bool _leftConducted;
        float _factor = 1f;

        internal ChainRun(ChainLightningBehaviour behaviour, StatusServices services, StatusDefinition status, SourceId source,
            int jumps, DamageTags inheritedTags, IDamageable origin)
        {
            Behaviour = behaviour;
            _services = services;
            _status = status;
            Source = source;
            JumpsLeft = Mathf.Max(0, jumps);
            _inheritedTags = inheritedTags;
            Origin = origin;
            HitId = services.Damage.NextHitId();
            _visited.Add(origin);
        }

        public ChainLightningBehaviour Behaviour { get; }

        /// <summary>The original attacker: every jump is attributed to it (Combo, RAGE, telemetry).</summary>
        public SourceId Source { get; }

        public IDamageable Origin { get; }
        public int HitId { get; }
        public int JumpsLeft { get; private set; }
        public int JumpsMade { get; private set; }
        public int VisitedCount => _visited.Count;

        /// <summary>
        /// The chain reached <paramref name="target"/> (the origin, or a jump that landed). Adds the target's chain bonus and
        /// sends the next jump, if any jump is left and a living, unvisited enemy is in range.
        /// </summary>
        internal void Arrived(IDamageable target, in StatusApplyInfo info, float now)
        {
            if (target != Origin)
            {
                if (target != _pendingTarget) return; // not the jump this run sent (should not happen)
                _pendingTarget = null;
                _services.Events.RaiseChainJumped(new ChainJumped(Source, _from, target, _fromPosition, target.Position,
                    JumpsMade, _leftConducted || info.Conducted, now));
            }

            var bonus = info.ChainBonus;
            JumpsLeft += bonus.MaxJumps;
            if (JumpsLeft <= 0 || JumpsMade >= Behaviour.MaxTotalJumps) return;
            Vector2 from = target.Position;
            var next = PickNext(from, Mathf.Max(0f, Behaviour.Radius + bonus.Radius));
            if (next == null) return;

            JumpsLeft--;
            JumpsMade++;
            _visited.Add(next);
            _pendingTarget = next;
            _from = target;
            _fromPosition = from;
            _leftConducted = !bonus.IsNone;
            _factor *= Mathf.Clamp01(Behaviour.Falloff + bonus.Falloff);

            var request = new DamageRequest(Source, Behaviour.Damage * _factor, Behaviour.JumpTags | _inheritedTags, Vector2.zero,
                new StatusPayload(_status, 1, false, this), HitId, HitWeight.Light);
            _services.Damage.Apply(next, request, now);
        }

        static int CompareIds(IDamageable a, IDamageable b)
        {
            int kind = a.Id.Kind.CompareTo(b.Id.Kind);
            return kind != 0 ? kind : a.Id.Value.CompareTo(b.Id.Value);
        }

        IDamageable PickNext(Vector2 from, float radius)
        {
            _candidates.Clear();
            float best = float.PositiveInfinity;
            var all = _services.Enemies.All;
            for (int i = 0; i < all.Count; i++)
            {
                // Only targets that can carry Shock continue a chain (the landing's status application drives the next hop).
                if (!(all[i] is IDamageable target) || target.Statuses == null || !target.IsAlive || _visited.Contains(target)) continue;
                float distance = Vector2.Distance(from, target.Position);
                if (distance > radius) continue;
                _candidates.Add(new Candidate { Target = target, Distance = distance });
                if (distance < best) best = distance;
            }
            if (_candidates.Count == 0) return null;

            _tied.Clear();
            float limit = best + Behaviour.TieEpsilon;
            for (int i = 0; i < _candidates.Count; i++)
            {
                if (_candidates[i].Distance <= limit) _tied.Add(_candidates[i].Target);
            }
            _candidates.Clear();
            // Stable order (by id), so the pick depends on the seed only, not on registration order.
            if (_tied.Count > 1) _tied.Sort(CompareIds);
            // Only a real tie draws from the Status stream, so untied chains never shift other status randomness.
            var pick = _tied.Count == 1 ? _tied[0] : _tied[_services.Rng.Range(0, _tied.Count)];
            _tied.Clear();
            return pick;
        }
    }
}

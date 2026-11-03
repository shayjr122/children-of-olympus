using System;
using System.Collections.Generic;
using UnityEngine;

namespace GMR.Core.Combat
{
    /// <summary>
    /// The single damage pipeline (AD-9, AD-16). Every hit goes through <see cref="Apply"/>, which enqueues; requests
    /// raised while a request is resolving (event handlers, later chain jumps) run FIFO after it, in the same frame.
    /// At most <see cref="MaxRequestsPerFrame"/> requests are accepted per frame (<see cref="BeginFrame"/>); the rest
    /// are dropped with a warning. Per request: invulnerability check -> (modifier fold, Epic 5) -> HP -> knockback
    /// impulse -> target.ApplyResolved -> events -> status payload hook. Sole owner of HP changes and of HitLanded,
    /// HitAvoided, PlayerDamaged and EnemyDied.
    /// </summary>
    public sealed class DamageSystem
    {
        public const int MaxRequestsPerFrame = 256;

        readonly struct Pending
        {
            public readonly IDamageable Target;
            public readonly DamageRequest Request;
            public readonly float Now;

            public Pending(IDamageable target, in DamageRequest request, float now)
            {
                Target = target;
                Request = request;
                Now = now;
            }
        }

        sealed class NoStatusApplier : IStatusApplier
        {
            public void Apply(IDamageable target, in StatusPayload payload, SourceId source, float now) { }
        }

        readonly CombatEvents _events;
        readonly Queue<Pending> _queue = new Queue<Pending>(16);
        IStatusApplier _statusApplier = new NoStatusApplier();
        bool _resolving;
        bool _warnedThisFrame;
        int _nextHitId = 1;

        public DamageSystem(CombatEvents events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <summary>Requests accepted since the last <see cref="BeginFrame"/>.</summary>
        public int RequestsThisFrame { get; private set; }

        /// <summary>Requests dropped by the cap since the last <see cref="BeginFrame"/>.</summary>
        public int DroppedThisFrame { get; private set; }

        /// <summary>Receives the status payload of every landed hit. No-op until the Status System (Epic 5).</summary>
        public IStatusApplier StatusApplier
        {
            get => _statusApplier;
            set => _statusApplier = value ?? new NoStatusApplier();
        }

        /// <summary>A fresh hit id (one per ability activation, projectile or enemy attack), unique per Encounter.</summary>
        public int NextHitId() => _nextHitId++;

        /// <summary>Resets the per-frame cap. Called once per frame by EncounterBootstrap.</summary>
        public void BeginFrame()
        {
            RequestsThisFrame = 0;
            DroppedThisFrame = 0;
            _warnedThisFrame = false;
        }

        /// <summary>
        /// Damages <paramref name="target"/> at scaled time <paramref name="now"/>. Resolves immediately unless a
        /// resolution is already running, in which case it is queued and runs after it (FIFO, same frame).
        /// </summary>
        public void Apply(IDamageable target, in DamageRequest request, float now)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (RequestsThisFrame >= MaxRequestsPerFrame)
            {
                DroppedThisFrame++;
                if (!_warnedThisFrame)
                {
                    _warnedThisFrame = true;
                    Debug.LogWarning($"[GMR/Combat] DamageSystem cap of {MaxRequestsPerFrame} requests per frame reached; " +
                                     $"dropping request from {request.Source} (hit {request.HitId}).");
                }
                return;
            }

            RequestsThisFrame++;
            _queue.Enqueue(new Pending(target, request, now));
            if (_resolving) return;

            _resolving = true;
            try
            {
                while (_queue.Count > 0)
                {
                    var next = _queue.Dequeue();
                    Resolve(next.Target, next.Request, next.Now);
                }
            }
            finally
            {
                _resolving = false;
                _queue.Clear(); // only non-empty if a handler threw; never carry stale requests into the next Apply
            }
        }

        /// <summary>Restores <paramref name="target"/> to full HP (training dummy respawn). The only other HP write.</summary>
        public void Restore(IDamageable target, float maxHp)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            target.Health.Restore(maxHp);
        }

        void Resolve(IDamageable target, in DamageRequest req, float now)
        {
            if (!target.IsAlive || target.Health == null) return;
            Vector2 position = target.Position;

            if (target.IsInvulnerable(now))
            {
                _events.RaiseHitAvoided(new HitAvoided(target, target.Id, req.Source, position, req.HitId, now));
                return;
            }

            // Modifier fold (statuses, archetype armor) arrives with Epic 5; the amount passes through unchanged.
            float amount = req.Amount;
            bool wasDepleted = target.Health.IsDepleted;
            float dealt = target.Health.TakeDamage(amount);
            bool killed = !wasDepleted && target.Health.IsDepleted;

            float resistance = Mathf.Clamp01(target.KnockbackResistance);
            Vector2 impulse = req.Knockback * (1f - resistance);

            var result = new DamageResult(dealt, killed, false, impulse, req.Tags, req.Source, req.Weight, req.HitId, now);
            target.ApplyResolved(result);

            var id = target.Id;
            if (id.IsPlayer) _events.RaisePlayerDamaged(new PlayerDamaged(id.Player, target, position, result));
            else _events.RaiseHitLanded(new HitLanded(target, id, position, result));

            if (req.Status.HasStatus) _statusApplier.Apply(target, req.Status, req.Source, now);

            if (killed && !id.IsPlayer) _events.RaiseEnemyDied(new EnemyDied(target, id, req.Source, position, now));
        }
    }
}

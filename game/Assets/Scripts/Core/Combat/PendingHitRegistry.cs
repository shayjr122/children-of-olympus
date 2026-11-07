using System;
using System.Collections.Generic;
using GMR.Core.Players;
using UnityEngine;

namespace GMR.Core.Combat
{
    /// <summary>
    /// Enemy hits that are telegraphed but have not landed yet (AD-12). Attacks register when the telegraph starts
    /// and remove themselves when they resolve. A DASH is a Perfect Dodge if it starts within the Perfect Dodge
    /// Window before a pending hit's land time while overlapping its area, at most once per (attackId, PlayerId).
    /// </summary>
    public sealed class PendingHitRegistry
    {
        readonly List<PendingHit> _hits = new List<PendingHit>(8);
        readonly HashSet<long> _claimed = new HashSet<long>();
        int _nextAttackId = 1;

        public IReadOnlyList<PendingHit> All => _hits;

        /// <summary>A fresh attack id, unique within this Encounter.</summary>
        public int NextAttackId() => _nextAttackId++;

        public void Register(in PendingHit hit)
        {
            for (int i = 0; i < _hits.Count; i++)
            {
                if (_hits[i].AttackId == hit.AttackId)
                    throw new InvalidOperationException($"Attack {hit.AttackId} is already pending.");
            }
            _hits.Add(hit);
        }

        /// <summary>Removes a resolved or cancelled attack and forgets its Perfect Dodge claims.</summary>
        public bool Remove(int attackId)
        {
            for (int i = 0; i < _hits.Count; i++)
            {
                if (_hits[i].AttackId != attackId) continue;
                _hits.RemoveAt(i);
                _claimed.RemoveWhere(k => (int)(k >> 32) == attackId);
                return true;
            }
            return false;
        }

        /// <summary>True if <paramref name="dashStart"/> is at most <paramref name="window"/> before (or at) <paramref name="landTime"/>.</summary>
        public static bool IsInPerfectWindow(float landTime, float dashStart, float window) =>
            dashStart <= landTime && landTime - dashStart <= window;

        /// <summary>
        /// Finds the first pending hit (registration order) that this DASH perfectly dodges and that this Player has
        /// not claimed yet, and claims it.
        /// </summary>
        public bool TryClaimPerfectDodge(PlayerId player, Vector2 position, float radius, float dashStart, float window,
            out PendingHit dodged)
        {
            for (int i = 0; i < _hits.Count; i++)
            {
                var hit = _hits[i];
                if (!IsInPerfectWindow(hit.LandTime, dashStart, window)) continue;
                if (!hit.Overlaps(position, radius)) continue;
                if (!_claimed.Add(Key(hit.AttackId, player))) continue;
                dodged = hit;
                return true;
            }
            dodged = default;
            return false;
        }

        /// <summary>True if <paramref name="player"/> already got a Perfect Dodge for <paramref name="attackId"/>.</summary>
        public bool HasClaimed(int attackId, PlayerId player) => _claimed.Contains(Key(attackId, player));

        static long Key(int attackId, PlayerId player) => ((long)attackId << 32) | (uint)player.Value;
    }
}

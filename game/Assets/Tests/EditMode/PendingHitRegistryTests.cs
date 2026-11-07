using System;
using GMR.Core.Combat;
using GMR.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace GMR.Tests
{
    public sealed class PendingHitRegistryTests
    {
        const float Window = 0.08f;
        static readonly PlayerId P0 = new PlayerId(0);
        static readonly PlayerId P1 = new PlayerId(1);

        static PendingHitRegistry WithHit(out int attackId, float landTime = 2f)
        {
            var r = new PendingHitRegistry();
            attackId = r.NextAttackId();
            r.Register(new PendingHit(attackId, landTime, Vector2.zero, 1.5f));
            return r;
        }

        [Test]
        public void DashInsideWindow_Overlapping_IsPerfect()
        {
            var r = WithHit(out int id);
            Assert.IsTrue(r.TryClaimPerfectDodge(P0, new Vector2(1f, 0f), 0.4f, 2f - 0.05f, Window, out var hit));
            Assert.AreEqual(id, hit.AttackId);
        }

        [Test]
        public void DashAtWindowEdges()
        {
            Assert.IsTrue(PendingHitRegistry.IsInPerfectWindow(2f, 2f - 0.0799f, Window), "at the start of the window");
            Assert.IsTrue(PendingHitRegistry.IsInPerfectWindow(2f, 2f, Window), "at land time");
            Assert.IsFalse(PendingHitRegistry.IsInPerfectWindow(2f, 2f - 0.09f, Window), "just outside (too early)");
            Assert.IsFalse(PendingHitRegistry.IsInPerfectWindow(2f, 2.01f, Window), "after landing");
        }

        [Test]
        public void DashJustOutsideWindow_IsNotPerfect()
        {
            var r = WithHit(out _);
            Assert.IsFalse(r.TryClaimPerfectDodge(P0, Vector2.zero, 0.4f, 2f - 0.09f, Window, out _));
        }

        [Test]
        public void DashNotOverlappingArea_IsNotPerfect()
        {
            var r = WithHit(out _);
            Assert.IsFalse(r.TryClaimPerfectDodge(P0, new Vector2(3f, 0f), 0.4f, 1.97f, Window, out _));
        }

        [Test]
        public void AtMostOnePerAttackAndPlayer()
        {
            var r = WithHit(out int id);
            Assert.IsTrue(r.TryClaimPerfectDodge(P0, Vector2.zero, 0.4f, 1.95f, Window, out _));
            Assert.IsFalse(r.TryClaimPerfectDodge(P0, Vector2.zero, 0.4f, 1.99f, Window, out _));
            Assert.IsTrue(r.HasClaimed(id, P0));
        }

        [Test]
        public void SameAttack_DifferentPlayers_EachGetOne()
        {
            var r = WithHit(out _);
            Assert.IsTrue(r.TryClaimPerfectDodge(P0, Vector2.zero, 0.4f, 1.95f, Window, out _));
            Assert.IsTrue(r.TryClaimPerfectDodge(P1, new Vector2(0.5f, 0f), 0.4f, 1.96f, Window, out _));
        }

        [Test]
        public void TwoAttacks_SamePlayer_CanEachBePerfect()
        {
            var r = WithHit(out int first);
            int second = r.NextAttackId();
            r.Register(new PendingHit(second, 2f, new Vector2(0.5f, 0f), 1f));
            Assert.IsTrue(r.TryClaimPerfectDodge(P0, Vector2.zero, 0.4f, 1.95f, Window, out var a));
            Assert.IsTrue(r.TryClaimPerfectDodge(P0, Vector2.zero, 0.4f, 1.95f, Window, out var b));
            Assert.AreEqual(first, a.AttackId);
            Assert.AreEqual(second, b.AttackId);
        }

        [Test]
        public void Remove_ForgetsHit_AndAttackIdsAreUnique()
        {
            var r = WithHit(out int id);
            Assert.IsTrue(r.Remove(id));
            Assert.IsFalse(r.Remove(id));
            Assert.AreEqual(0, r.All.Count);
            Assert.AreNotEqual(id, r.NextAttackId());
            Assert.IsFalse(r.TryClaimPerfectDodge(P0, Vector2.zero, 0.4f, 1.95f, Window, out _));
        }

        [Test]
        public void DuplicateRegistration_Throws()
        {
            var r = WithHit(out int id);
            Assert.Throws<InvalidOperationException>(() => r.Register(new PendingHit(id, 3f, Vector2.zero, 1f)));
        }
    }
}

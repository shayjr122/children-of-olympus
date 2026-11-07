using System.Linq;
using GMR.Core.Combat;
using GMR.Core.Status;
using NUnit.Framework;
using UnityEngine;

namespace GMR.Tests
{
    /// <summary>Story 5.3: Wet and the data-driven Rule_WetConductsShock (x2 Shock, +1 jump, +1.5 u, consume toggle).</summary>
    public sealed class StatusInteractionTests
    {
        StatusTestWorld _w;
        StatusDefinition _shock;
        StatusDefinition _wet;
        StatusInteractionRule _rule;

        [SetUp]
        public void SetUp()
        {
            _w = new StatusTestWorld();
            _shock = _w.Shock(out _);
            _wet = _w.Status("wet", 6f);
            _rule = _w.WetConductsShock(_shock, _wet);
        }

        [TearDown] public void TearDown() => _w.Dispose();

        void Wet(params StatusTestWorld.FakeEnemy[] targets)
        {
            foreach (var t in targets) t.Statuses.Apply(_wet, 1, StatusTestWorld.P0, 0f);
        }

        [Test]
        public void WetTarget_ShockDamageIsDoubled_AndConductedShockRaised()
        {
            var target = _w.Enemy(Vector2.zero);
            Wet(target);
            _w.ShockHit(target, _shock);
            Assert.AreEqual(16f, _w.Hits[0].Result.Dealt, 1e-4f, "8 x 2.0");
            Assert.IsTrue(_w.Hits[0].Result.Conducted);
            Assert.AreEqual(1, _w.Conducted.Count);
            var e = _w.Conducted[0];
            Assert.AreEqual("wet_conducts_shock", e.Tag);
            Assert.AreEqual(StatusTestWorld.P0, e.Source);
            Assert.AreSame(target, e.Target);
            Assert.AreSame(_wet, e.Conductor);
            Assert.AreEqual(2f, e.Multiplier);
        }

        [Test]
        public void DryTarget_ShockDamageUnchanged_NoConductedShock()
        {
            var target = _w.Enemy(Vector2.zero);
            _w.ShockHit(target, _shock);
            Assert.AreEqual(8f, _w.Hits[0].Result.Dealt, 1e-4f);
            Assert.IsEmpty(_w.Conducted);
        }

        [Test]
        public void ChainJumpOntoWetTarget_IsDoubled()
        {
            var line = _w.Line(2, 3f);
            Wet(line[1]);
            _w.ShockHit(line[0], _shock);
            Assert.AreEqual(8f * 0.7f * 2f, _w.Hits[1].Result.Dealt, 1e-4f);
            Assert.IsTrue(_w.Conducted.Single().IsChainJump);
            Assert.IsTrue(_w.Jumps.Single().Conducted, "the jump landing on Wet draws the conducted arc");
        }

        [Test]
        public void ChainLeavingWetTarget_GetsOneMoreJump()
        {
            var line = _w.Line(6, 3f);
            _w.ShockHit(line[0], _shock);
            int dry = _w.Hits.Select(h => h.Target).Distinct().Count();

            using (var w2 = new StatusTestWorld())
            {
                var shock = w2.Shock(out _);
                var wet = w2.Status("wet", 6f);
                w2.WetConductsShock(shock, wet);
                var line2 = w2.Line(6, 3f);
                line2[0].Statuses.Apply(wet, 1, StatusTestWorld.P0, 0f);
                w2.ShockHit(line2[0], shock);
                int wetted = w2.Hits.Select(h => h.Target).Distinct().Count();
                Assert.AreEqual(4, dry);
                Assert.AreEqual(dry + 1, wetted, "+1 jump for the chain leaving the Wet target");
                Assert.IsTrue(w2.Jumps[0].Conducted, "the arc leaving Wet is conducted");
                Assert.IsFalse(w2.Jumps[1].Conducted);
            }
        }

        [Test]
        public void ChainLeavingWetTarget_GetsPlus1_5Radius()
        {
            var origin = _w.Enemy(Vector2.zero);
            var far = _w.Enemy(new Vector2(4.5f, 0f)); // beyond 3.5, inside 3.5 + 1.5
            _w.ShockHit(origin, _shock);
            Assert.AreEqual(1, _w.Hits.Count, "dry: out of reach");

            _w.Hits.Clear();
            Wet(origin);
            _w.ShockHit(origin, _shock);
            Assert.AreEqual(2, _w.Hits.Count, "Wet: radius 5.0 reaches it");
            Assert.AreSame(far, _w.Hits[1].Target);
        }

        [Test]
        public void SameLayout_WetChainReachesMoreTargets()
        {
            // Five 3 u apart: dry reaches 4 (N 3); all Wet reaches all 5.
            var line = _w.Line(5, 3f);
            _w.ShockHit(line[0], _shock);
            int dry = _w.Hits.Select(h => h.Target).Distinct().Count();
            foreach (var e in line) e.Statuses.Clear(0f);
            _w.Hits.Clear();
            _w.Jumps.Clear();

            Wet(line.ToArray());
            _w.ShockHit(line[0], _shock);
            int wet = _w.Hits.Select(h => h.Target).Distinct().Count();
            Assert.AreEqual(4, dry);
            Assert.AreEqual(5, wet);
            Assert.IsTrue(_w.Jumps.All(j => j.Conducted));
            Assert.AreEqual(5, _w.Conducted.Count, "one ConductedShock per conducted application");
            for (int i = 0; i < 5; i++) // every hit doubled vs. its dry value
                Assert.AreEqual(8f * Mathf.Pow(0.7f, i) * 2f, _w.Hits[i].Result.Dealt, 1e-3f);
        }

        [Test]
        public void FiveInALine_4uApart_Dry1Hit_AllWet5Hits()
        {
            // Matrix row: every hop needs the +1.5 u of the Wet target it leaves (4 u > R 3.5 u).
            var line = _w.Line(5, 4f);
            _w.ShockHit(line[0], _shock);
            Assert.AreEqual(1, _w.Hits.Count, "dry: no jump reaches 4 u");
            foreach (var e in line) e.Statuses.Clear(0f);
            _w.Hits.Clear();

            Wet(line.ToArray());
            _w.ShockHit(line[0], _shock);
            CollectionAssert.AreEqual(line, _w.Hits.Select(h => h.Target).ToList(), "Wet: every hop conducted, all five once");
        }

        [Test]
        public void WetChain_IsCappedByMaxTotalJumps()
        {
            var line = _w.Line(15, 3f);
            Wet(line.ToArray());
            ((ChainLightningBehaviour)_shock.Behaviours[0]).MaxTotalJumps = 10;
            _w.ShockHit(line[0], _shock);
            Assert.AreEqual(11, _w.Hits.Count, "+1 per Wet hop never exceeds the hard cap");
        }

        [Test]
        public void ConsumeTargetStatusFalse_WetStays()
        {
            var target = _w.Enemy(Vector2.zero);
            Wet(target);
            _w.ShockHit(target, _shock);
            Assert.IsTrue(target.Statuses.Has(_wet), "default: Wet is not consumed by Shock");
        }

        [Test]
        public void ConsumeTargetStatusTrue_WetRemoved_SecondHitIsDry()
        {
            _rule.ConsumeTargetStatus = true;
            var target = _w.Enemy(Vector2.zero);
            Wet(target);
            _w.ShockHit(target, _shock);
            Assert.AreEqual(16f, _w.Hits[0].Result.Dealt, 1e-4f);
            Assert.IsFalse(target.Statuses.Has(_wet));
            Assert.AreEqual(StatusEndReason.Consumed, _w.Expired.Single(e => e.Status == _wet).Reason);
            _w.ShockHit(target, _shock);
            Assert.AreEqual(8f, _w.Hits[1].Result.Dealt, 1e-4f);
        }

        [Test]
        public void PhysicalHitApplyingShock_OnWet_NotDoubled_ButChainBonusAndConducted()
        {
            var line = _w.Line(2, 4.5f);
            Wet(line[0]);
            _w.ShockHit(line[0], _shock, 20f, DamageTags.Physical); // Storm Whirl
            Assert.AreEqual(20f, _w.Hits[0].Result.Dealt, 1e-4f, "Wet multiplies Shock damage only");
            Assert.AreEqual(2, _w.Hits.Count, "chain leaving Wet still gets +1.5 u");
            Assert.AreEqual(1f, _w.Conducted[0].Multiplier);
        }

        [Test]
        public void Wet_RefreshesOnReapply_ExpiresAfter6s_AndShockDoesNotConsumeIt()
        {
            var target = _w.Enemy(Vector2.zero);
            Wet(target);
            for (int i = 0; i < 40; i++) target.Statuses.Tick(0.1f, i * 0.1f); // 4 s
            _w.ShockHit(target, _shock, now: 4f);
            Wet(target); // re-applied before expiry: back to 6 s
            Assert.AreEqual(6f, target.Statuses.Instances.First(s => s.Definition == _wet).Remaining, 1e-4f);
            for (int i = 0; i < 59; i++) target.Statuses.Tick(0.1f, 4f + i * 0.1f);
            Assert.IsTrue(target.Statuses.Has(_wet));
            target.Statuses.Tick(0.1f, 10f);
            Assert.IsFalse(target.Statuses.Has(_wet), "expires 6 s after the last application");
        }

        [Test]
        public void NoRuleSet_NoInteraction()
        {
            _w.Services.Rules = null;
            var target = _w.Enemy(Vector2.zero);
            Wet(target);
            _w.ShockHit(target, _shock);
            Assert.AreEqual(8f, _w.Hits[0].Result.Dealt, 1e-4f);
            Assert.IsEmpty(_w.Conducted);
        }
    }
}

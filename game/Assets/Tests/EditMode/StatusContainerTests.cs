using System.Linq;
using System.Reflection;
using GMR.Core.Combat;
using GMR.Core.Status;
using NUnit.Framework;
using UnityEngine;

namespace GMR.Tests
{
    /// <summary>Story 5.1: generic StatusContainer (apply, refresh rule, max stacks, expiry by Tick, source, events, fold).</summary>
    public sealed class StatusContainerTests
    {
        /// <summary>
        /// A behaviour that exists only in the test assembly: counts its hooks, deals 1 StatusTick damage per tick
        /// through DamageSystem and adds a flat +5 to incoming damage. Proves new behaviour types need no core edit.
        /// </summary>
        sealed class TestBurnBehaviour : StatusBehaviour
        {
            public int Applies, Ticks, Expires;
            public StatusEndReason LastEnd;

            public override void OnApply(in StatusApplyContext c) => Applies++;

            public override void OnTick(in StatusTickContext c)
            {
                Ticks++;
                c.Services.Damage.Apply(c.Target, new DamageRequest(c.Source, 1f, DamageTags.StatusTick, Vector2.zero,
                    default, c.Services.Damage.NextHitId()), c.Now);
            }

            public override void OnExpire(in StatusExpireContext c)
            {
                Expires++;
                LastEnd = c.Reason;
            }

            public override void ModifyIncoming(in StatusModifyContext c, ref float amount, ref ChainParams chain, in DamageRequest request)
            {
                if ((request.Tags & DamageTags.Physical) != 0) amount += 5f;
            }
        }

        StatusTestWorld _w;

        [SetUp] public void SetUp() => _w = new StatusTestWorld();
        [TearDown] public void TearDown() => _w.Dispose();

        [Test]
        public void RefreshDuration_AppliedTwice2sApart_OneInstanceResetTo6_ExpiresAfter6sOfTick()
        {
            var wet = _w.Status("wet", 6f);
            var dummy = _w.Enemy(Vector2.zero);
            var c = dummy.Statuses;

            c.Apply(wet, 1, StatusTestWorld.P0, 0f);
            c.Tick(2f, 2f);
            Assert.AreEqual(4f, c.Instances[0].Remaining, 1e-4f);
            c.Apply(wet, 1, StatusTestWorld.P0, 2f);

            Assert.AreEqual(1, c.Count, "one instance");
            Assert.AreEqual(6f, c.Instances[0].Remaining, 1e-4f, "duration reset to 6 s");
            for (int i = 0; i < 59; i++) c.Tick(0.1f, 2f + i * 0.1f);
            Assert.IsTrue(c.Has(wet), "still active just before 6 s");
            c.Tick(0.1f, 8f);
            Assert.IsFalse(c.Has(wet), "expired after 6 s of Tick");
            Assert.AreEqual(1, _w.Expired.Count);
            Assert.AreEqual(StatusEndReason.Expired, _w.Expired[0].Reason);
        }

        [Test]
        public void AddStacks_ClampsToMaxStacks_AndRefreshes()
        {
            var burn = _w.Status("burn", 3f, 3, RefreshRule.AddStacks);
            var c = _w.Enemy(Vector2.zero).Statuses;
            for (int i = 0; i < 5; i++) c.Apply(burn, 1, StatusTestWorld.P0, i);
            Assert.AreEqual(1, c.Count);
            Assert.AreEqual(3, c.Instances[0].Stacks);
            Assert.AreEqual(3f, c.Instances[0].Remaining, 1e-4f);
        }

        [Test]
        public void MaxStacks1_RefreshDuration_NeverStacks()
        {
            var wet = _w.Status("wet", 6f);
            var c = _w.Enemy(Vector2.zero).Statuses;
            c.Apply(wet, 4, StatusTestWorld.P0, 0f);
            c.Apply(wet, 2, StatusTestWorld.P0, 0f);
            Assert.AreEqual(1, c.Instances[0].Stacks);
        }

        [Test]
        public void KeepExisting_DoesNotRefresh()
        {
            var mark = _w.Status("mark", 5f, 1, RefreshRule.KeepExisting);
            var c = _w.Enemy(Vector2.zero).Statuses;
            c.Apply(mark, 1, StatusTestWorld.P0, 0f);
            c.Tick(2f, 2f);
            c.Apply(mark, 1, StatusTestWorld.P0, 2f);
            Assert.AreEqual(3f, c.Instances[0].Remaining, 1e-4f);
        }

        [Test]
        public void Apply_StoresSource_AndRaisesStatusAppliedOnce()
        {
            var wet = _w.Status("wet", 6f);
            var dummy = _w.Enemy(new Vector2(2f, 1f));
            var p1 = SourceId.ForPlayer(new GMR.Core.Players.PlayerId(1));

            dummy.Statuses.Apply(wet, 1, StatusTestWorld.P0, 0.5f);
            Assert.AreEqual(StatusTestWorld.P0, dummy.Statuses.Instances[0].Source);
            Assert.AreEqual(1, _w.Applied.Count);
            var e = _w.Applied[0];
            Assert.AreSame(wet, e.Status);
            Assert.AreEqual(StatusTestWorld.P0, e.Source);
            Assert.AreSame(dummy, e.Target);
            Assert.IsTrue(e.IsNew);

            dummy.Statuses.Apply(wet, 1, p1, 1f);
            Assert.AreEqual(p1, dummy.Statuses.Instances[0].Source, "the latest applier owns the refreshed instance");
            Assert.AreEqual(2, _w.Applied.Count);
            Assert.IsFalse(_w.Applied[1].IsNew);
        }

        [Test]
        public void StatusEvents_CanOnlyBeRaisedInsideCore()
        {
            foreach (var name in new[] { "RaiseStatusApplied", "RaiseStatusExpired", "RaiseConductedShock", "RaiseChainJumped" })
            {
                var m = typeof(CombatEvents).GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Assert.IsNotNull(m, name);
                Assert.IsTrue(m.IsAssembly, name + " must be internal (single owner, AD-16)");
            }
        }

        [Test]
        public void CustomBehaviour_NoCoreChange()
        {
            var behaviour = _w.Create<TestBurnBehaviour>();
            var burn = _w.Status("burn", 1f, 1, RefreshRule.RefreshDuration, behaviour);
            var dummy = _w.Enemy(Vector2.zero, 100f);

            dummy.Statuses.Apply(burn, 1, StatusTestWorld.P0, 0f);
            Assert.AreEqual(1, behaviour.Applies);

            _w.Damage.Apply(dummy, new DamageRequest(StatusTestWorld.P0, 10f, DamageTags.Physical, Vector2.zero, default, 99), 0f);
            Assert.AreEqual(15f, _w.Hits.Last().Result.Dealt, 1e-4f, "ModifyIncoming folded (+5 flat)");

            for (int i = 0; i < 4; i++) { _w.Damage.BeginFrame(); dummy.Statuses.Tick(0.25f, 0.25f * (i + 1)); }
            Assert.AreEqual(4, behaviour.Ticks);
            Assert.AreEqual(1, behaviour.Expires, "expires after its 1 s");
            Assert.AreEqual(StatusEndReason.Expired, behaviour.LastEnd);
            var ticks = _w.Hits.Where(h => (h.Result.Tags & DamageTags.StatusTick) != 0).ToList();
            Assert.AreEqual(4, ticks.Count, "tick damage goes through DamageSystem");
            Assert.IsTrue(ticks.All(h => h.Source == StatusTestWorld.P0), "attributed to the applier");
            Assert.AreEqual(100f - 15f - 4f, dummy.Health.Current, 1e-4f);
        }

        [Test]
        public void Remove_And_Clear_EndStatuses_WithReason()
        {
            var wet = _w.Status("wet", 6f);
            var shock = _w.Status("shockless", 1f);
            var c = _w.Enemy(Vector2.zero).Statuses;
            c.Apply(wet, 1, StatusTestWorld.P0, 0f);
            c.Apply(shock, 1, StatusTestWorld.P0, 0f);
            Assert.IsTrue(c.Remove(wet, 0f, StatusEndReason.Consumed));
            Assert.IsFalse(c.Has(wet));
            c.Clear(0f);
            Assert.AreEqual(0, c.Count);
            Assert.AreEqual(new[] { StatusEndReason.Consumed, StatusEndReason.Cleared }, _w.Expired.Select(e => e.Reason).ToArray());
        }

        [Test]
        public void Registry_TicksEachRegisteredContainerExactlyOnce()
        {
            var a = _w.Enemy(Vector2.zero);
            var b = _w.Enemy(Vector2.one);
            var registry = new StatusRegistry();
            registry.Register(a.Statuses);
            registry.Register(b.Statuses);
            registry.Register(a.Statuses); // duplicate registration is ignored
            for (int frame = 0; frame < 3; frame++) registry.TickAll(1f / 60f, frame / 60f);
            Assert.AreEqual(3, a.Statuses.TickCount);
            Assert.AreEqual(3, b.Statuses.TickCount);
            registry.Unregister(b.Statuses);
            registry.TickAll(1f / 60f, 0.05f);
            Assert.AreEqual(4, a.Statuses.TickCount);
            Assert.AreEqual(3, b.Statuses.TickCount);
        }

        [Test]
        public void NoStatuses_DamagePassesThroughUnchanged()
        {
            var dummy = _w.Enemy(Vector2.zero, 100f);
            _w.Damage.Apply(dummy, new DamageRequest(StatusTestWorld.P0, 12f, DamageTags.Shock, Vector2.zero, default, 1), 0f);
            Assert.AreEqual(12f, _w.Hits[0].Result.Dealt, 1e-4f);
            Assert.IsTrue(_w.Hits[0].Result.Chain.IsNone);
            Assert.IsFalse(_w.Hits[0].Result.Conducted);
        }
    }
}

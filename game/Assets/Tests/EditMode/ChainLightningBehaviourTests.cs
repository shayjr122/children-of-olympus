using System.Linq;
using GMR.Core.Combat;
using GMR.Core.Combo;
using GMR.Core.Players;
using GMR.Core.Rage;
using GMR.Core.Status;
using NUnit.Framework;
using UnityEngine;

namespace GMR.Tests
{
    /// <summary>Story 5.2: Shock chain lightning (N 3, R 3.5 u, falloff x0.7), visits, tie-breaks, attribution.</summary>
    public sealed class ChainLightningBehaviourTests
    {
        StatusTestWorld _w;

        [SetUp] public void SetUp() => _w = new StatusTestWorld();
        [TearDown] public void TearDown() => _w.Dispose();

        [Test]
        public void FourInALine_3uApart_AllHitOnce_WithFalloffPerJump()
        {
            var shock = _w.Shock(out _);
            var line = _w.Line(4, 3f);

            _w.ShockHit(line[0], shock);

            Assert.AreEqual(4, _w.Hits.Count, "initial hit + 3 jumps");
            CollectionAssert.AreEqual(line, _w.Hits.Select(h => h.Target).ToList(), "each target once, nearest first");
            Assert.AreEqual(8f, _w.Hits[0].Result.Dealt, 1e-4f);
            Assert.AreEqual(8f * 0.7f, _w.Hits[1].Result.Dealt, 1e-4f);
            Assert.AreEqual(8f * 0.7f * 0.7f, _w.Hits[2].Result.Dealt, 1e-4f);
            Assert.AreEqual(8f * 0.7f * 0.7f * 0.7f, _w.Hits[3].Result.Dealt, 1e-4f);
            foreach (var jump in _w.Hits.Skip(1))
            {
                Assert.AreEqual(DamageTags.Shock | DamageTags.ChainJump, jump.Result.Tags);
                Assert.AreEqual(StatusTestWorld.P0, jump.Source, "attributed to the original Player");
            }
            Assert.AreEqual(3, _w.Jumps.Count, "one ChainJumped per landed jump");
            Assert.AreEqual(Enumerable.Range(1, 3), _w.Jumps.Select(j => j.JumpIndex));
            Assert.IsTrue(_w.Jumps.All(j => !j.Conducted));
            Assert.IsTrue(line.All(e => e.Statuses.Has(shock)), "every target hit by the chain is Shocked (visible)");
        }

        [Test]
        public void MaxJumps_LimitsTheChain()
        {
            var shock = _w.Shock(out _);
            var line = _w.Line(6, 3f);
            _w.ShockHit(line[0], shock);
            Assert.AreEqual(4, _w.Hits.Select(h => h.Target).Distinct().Count());
            Assert.AreEqual(0, line[4].Statuses.Count);
        }

        [Test]
        public void OutOfRadius_NoJump()
        {
            var shock = _w.Shock(out _);
            var line = _w.Line(3, 3.6f);
            _w.ShockHit(line[0], shock);
            Assert.AreEqual(1, _w.Hits.Count);
            Assert.IsEmpty(_w.Jumps);
        }

        [Test]
        public void Triangle_NoTargetRevisited()
        {
            var shock = _w.Shock(out _);
            _w.Enemy(new Vector2(0f, 0f));
            _w.Enemy(new Vector2(1.5f, 1f));
            _w.Enemy(new Vector2(1.5f, -1f));
            _w.ShockHit((IDamageable)_w.Enemies.All[0], shock);
            Assert.AreEqual(3, _w.Hits.Count, "N 3 but only two others: no target hit twice");
            Assert.AreEqual(3, _w.Hits.Select(h => h.Target).Distinct().Count());
        }

        [Test]
        public void MaxPotency_UsesMaximumChainCount()
        {
            var shock = _w.Shock(out _);
            var line = _w.Line(9, 3f);
            _w.ShockHit(line[0], shock, maxPotency: true);
            Assert.AreEqual(7, _w.Hits.Count, "1 + MaxChainJumps (6)");
        }

        [Test]
        public void UltimateHit_ChainKeepsUltimateTag_SoItNeverRefillsRage()
        {
            var shock = _w.Shock(out _);
            var line = _w.Line(2, 3f);
            _w.ShockHit(line[0], shock, 100f, DamageTags.Shock | DamageTags.Ultimate);
            Assert.AreEqual(DamageTags.Shock | DamageTags.ChainJump | DamageTags.Ultimate, _w.Hits[1].Result.Tags);
            Assert.AreEqual(RageSource.UltimateHit, PlayerCombatState.RageSourceFor(_w.Hits[1].Result.Tags));
        }

        [Test]
        public void ChainOf4_RaisesComboBy4_AndFillsRageAtTheStatusRate()
        {
            var combo = _w.Create<ComboConfig>();
            var rage = _w.Create<RageConfig>();
            var state = new PlayerCombatState(new PlayerId(0), combo, rage, _w.Events);
            state.Bind();
            var shock = _w.Shock(out _);
            var line = _w.Line(4, 3f);

            _w.ShockHit(line[0], shock);

            Assert.AreEqual(4, state.Combo.Count, "1 hit + 3 chain jumps");
            float expected = rage.AmountFor(RageSource.Hit) + 3f * rage.AmountFor(RageSource.StatusTick);
            Assert.AreEqual(expected, state.Rage.Value01, 1e-4f, "chain jumps fill RAGE at the status rate");
            state.Unbind();
        }

        [Test]
        public void EqualDistanceTie_PickComesFromStatusStream_SameSeedSamePick()
        {
            Assert.AreEqual(TiePick(777), TiePick(777), "same seed, same pick");
            Assert.AreEqual(TiePick(778), TiePick(778));
            var picks = Enumerable.Range(1, 24).Select(s => TiePick((ulong)s)).Distinct().Count();
            Assert.AreEqual(2, picks, "both tied candidates are reachable: the pick is random, not list order");
        }

        static int TiePick(ulong seed)
        {
            using (var w = new StatusTestWorld(seed))
            {
                var shock = w.Shock(out _);
                var origin = w.Enemy(Vector2.zero);
                var right = w.Enemy(new Vector2(3f, 0f));
                var left = w.Enemy(new Vector2(-3f, 0f));
                // N 1: exactly one of the two equidistant candidates is hit.
                ((ChainLightningBehaviour)shock.Behaviours[0]).MaxJumps = 1;
                w.ShockHit(origin, shock);
                Assert.AreEqual(2, w.Hits.Count);
                return w.Hits[1].Target == right ? 1 : w.Hits[1].Target == left ? -1 : 0;
            }
        }

        [Test]
        public void TieBreak_DoesNotDependOnRegistrationOrder()
        {
            for (ulong seed = 1; seed <= 6; seed++)
                Assert.AreEqual(TiePickByX(seed, false), TiePickByX(seed, true), "seed " + seed);
        }

        static float TiePickByX(ulong seed, bool reversed)
        {
            using (var w = new StatusTestWorld(seed))
            {
                var shock = w.Shock(out var chain);
                chain.MaxJumps = 1;
                var origin = w.Enemy(Vector2.zero);
                // Same ids (allocated in the same order), registered in opposite orders.
                var a = new StatusTestWorld.FakeEnemy(SourceId.ForEnemy(w.Enemies.NextEnemyId()), new Vector2(3f, 0f), 1000f, w.Services);
                var b = new StatusTestWorld.FakeEnemy(SourceId.ForEnemy(w.Enemies.NextEnemyId()), new Vector2(-3f, 0f), 1000f, w.Services);
                if (reversed) { w.Enemies.Register(b); w.Enemies.Register(a); }
                else { w.Enemies.Register(a); w.Enemies.Register(b); }
                w.ShockHit(origin, shock);
                return w.Hits[1].Target.Position.x;
            }
        }

        [Test]
        public void EnemyWithoutStatusContainer_IsNotAChainTarget()
        {
            var shock = _w.Shock(out _);
            var origin = _w.Enemy(Vector2.zero);
            var bare = new BareEnemy(SourceId.ForEnemy(_w.Enemies.NextEnemyId()), new Vector2(1f, 0f));
            _w.Enemies.Register(bare);
            var next = _w.Enemy(new Vector2(3f, 0f));
            _w.ShockHit(origin, shock);
            Assert.AreEqual(0, bare.Hits, "a jump that could not continue the chain is never sent");
            Assert.AreSame(next, _w.Hits[1].Target);
        }

        sealed class BareEnemy : IDamageable, GMR.Core.Enemies.IEnemyTarget
        {
            public BareEnemy(SourceId id, Vector2 p) { Id = id; Position = p; Health = new Health(100f); }
            public int Hits;
            public SourceId Id { get; }
            public bool IsAlive => true;
            public Vector2 Position { get; }
            public Health Health { get; }
            public StatusContainer Statuses => null;
            public float KnockbackResistance => 0f;
            public bool IsInvulnerable(float now) => false;
            public void ApplyResolved(in DamageResult result) => Hits++;
        }

        [Test]
        public void DeadAndNonDamageableEnemies_AreSkipped()
        {
            var shock = _w.Shock(out _);
            var line = _w.Line(3, 3f);
            _w.Damage.Apply(line[1], new DamageRequest(StatusTestWorld.P0, 5000f, DamageTags.Physical, Vector2.zero, default, 1), 0f);
            _w.Hits.Clear();
            _w.ShockHit(line[0], shock);
            Assert.AreEqual(1, _w.Hits.Count, "the dead middle enemy breaks the 3 u line (next is 6 u away)");
        }
    }
}

using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using GMR.Core.Combat;
using GMR.Core.Enemies;
using GMR.Core.Players;
using GMR.Core.Status;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GMR.Tests
{
    public sealed class DamageSystemTests
    {
        sealed class FakeTarget : IDamageable
        {
            public FakeTarget(SourceId id, float maxHp, float resistance = 0f)
            {
                Id = id;
                Health = new Health(maxHp);
                KnockbackResistance = resistance;
            }

            public readonly InvulnerabilityTracker Invulnerability = new InvulnerabilityTracker();
            public int ResolvedCount;
            public DamageResult LastResult;
            public Vector2 AppliedImpulse;

            public SourceId Id { get; }
            public bool IsAlive => !Health.IsDepleted;
            public Vector2 Position => new Vector2(1f, 2f);
            public Health Health { get; }
            public float KnockbackResistance { get; }
            public bool IsInvulnerable(float now) => Invulnerability.IsInvulnerable(now);

            public void ApplyResolved(in DamageResult result)
            {
                ResolvedCount++;
                LastResult = result;
                AppliedImpulse += result.KnockbackImpulse;
            }
        }

        sealed class SpyApplier : IStatusApplier
        {
            public int Calls;
            public StatusPayload Last;

            public void Apply(IDamageable target, in StatusPayload payload, SourceId source, float now)
            {
                Calls++;
                Last = payload;
            }
        }

        static readonly SourceId P0 = SourceId.ForPlayer(new PlayerId(0));

        CombatEvents _events;
        DamageSystem _damage;
        readonly List<HitLanded> _landed = new List<HitLanded>();
        readonly List<HitAvoided> _avoided = new List<HitAvoided>();
        readonly List<PlayerDamaged> _playerDamaged = new List<PlayerDamaged>();
        readonly List<EnemyDied> _died = new List<EnemyDied>();

        [SetUp]
        public void SetUp()
        {
            _events = new CombatEvents();
            _damage = new DamageSystem(_events);
            _landed.Clear();
            _avoided.Clear();
            _playerDamaged.Clear();
            _died.Clear();
            _events.OnHitLanded += _landed.Add;
            _events.OnHitAvoided += _avoided.Add;
            _events.OnPlayerDamaged += _playerDamaged.Add;
            _events.OnEnemyDied += _died.Add;
            _damage.BeginFrame();
        }

        static SourceId Enemy(int i) => SourceId.ForEnemy(new EnemyId(i));

        static DamageRequest Request(float amount, Vector2 knockback = default, SourceId? source = null) =>
            new DamageRequest(source ?? P0, amount, DamageTags.Physical, knockback, default, 7);

        [Test]
        public void TenDamage_HalfResistance_HpImpulseAndHitLanded()
        {
            var dummy = new FakeTarget(Enemy(0), 100f, 0.5f);
            _damage.Apply(dummy, Request(10f, new Vector2(4f, 0f)), 1f);

            Assert.AreEqual(90f, dummy.Health.Current);
            Assert.AreEqual(1, dummy.ResolvedCount, "the impulse is applied through ApplyResolved, once");
            Assert.AreEqual(new Vector2(2f, 0f), dummy.AppliedImpulse);
            Assert.AreEqual(1, _landed.Count);
            Assert.AreEqual(P0, _landed[0].Source, "HitLanded carries the request's SourceId");
            Assert.AreEqual(Enemy(0), _landed[0].TargetId);
            Assert.AreEqual(10f, _landed[0].Result.Dealt);
            Assert.AreEqual(new Vector2(2f, 0f), _landed[0].Result.KnockbackImpulse);
            Assert.AreEqual(7, _landed[0].Result.HitId);
            Assert.IsEmpty(_avoided);
            Assert.IsEmpty(_playerDamaged);
        }

        [Test]
        public void InvulnerableTarget_NoHpChange_HitAvoided()
        {
            var dummy = new FakeTarget(Enemy(0), 100f);
            dummy.Invulnerability.Register(0f, 1f, InvulnerabilitySource.PostHit);
            _damage.Apply(dummy, Request(10f, new Vector2(4f, 0f)), 0.5f);

            Assert.AreEqual(100f, dummy.Health.Current);
            Assert.AreEqual(0, dummy.ResolvedCount, "an avoided hit applies no knockback");
            Assert.AreEqual(1, _avoided.Count);
            Assert.AreEqual(P0, _avoided[0].Source);
            Assert.IsEmpty(_landed);
        }

        [Test]
        public void RequestEnqueuedDuringResolution_RunsFifoInSameFrame()
        {
            var a = new FakeTarget(Enemy(0), 100f);
            var b = new FakeTarget(Enemy(1), 100f);
            var c = new FakeTarget(Enemy(2), 100f);
            bool bStillFullInsideHandler = false;
            _events.OnHitLanded += e =>
            {
                if (e.TargetId != a.Id) return;
                _damage.Apply(b, Request(5f), 1f);
                _damage.Apply(c, Request(5f), 1f);
                bStillFullInsideHandler = b.Health.Current == 100f; // queued, not resolved re-entrantly
            };

            _damage.Apply(a, Request(5f), 1f);

            Assert.IsTrue(bStillFullInsideHandler, "requests raised during resolution must wait their turn");
            Assert.AreEqual(3, _landed.Count, "all queued requests resolve before Apply returns");
            Assert.AreEqual(a.Id, _landed[0].TargetId);
            Assert.AreEqual(b.Id, _landed[1].TargetId);
            Assert.AreEqual(c.Id, _landed[2].TargetId);
            Assert.AreEqual(95f, c.Health.Current);
        }

        [Test]
        public void Cap_257thRequestInAFrame_IsDroppedWithWarning()
        {
            var tank = new FakeTarget(Enemy(0), 10000f);
            for (int i = 0; i < DamageSystem.MaxRequestsPerFrame; i++) _damage.Apply(tank, Request(1f), 1f);
            Assert.AreEqual(10000f - 256f, tank.Health.Current);

            LogAssert.Expect(LogType.Warning, new Regex(@"^\[GMR/Combat\].*256"));
            _damage.Apply(tank, Request(1f), 1f);
            Assert.AreEqual(10000f - 256f, tank.Health.Current, "the 257th request is dropped");
            Assert.AreEqual(1, _damage.DroppedThisFrame);

            _damage.BeginFrame();
            _damage.Apply(tank, Request(1f), 1f);
            Assert.AreEqual(10000f - 257f, tank.Health.Current, "a new frame accepts requests again");
        }

        [Test]
        public void Cap_CountsReentrantRequests()
        {
            var a = new FakeTarget(Enemy(0), 100000f);
            int handled = 0;
            _events.OnHitLanded += _ =>
            {
                handled++;
                _damage.Apply(a, Request(1f), 1f); // runaway chain: every hit raises another
            };
            LogAssert.Expect(LogType.Warning, new Regex(@"^\[GMR/Combat\]"));
            _damage.Apply(a, Request(1f), 1f);
            Assert.AreEqual(DamageSystem.MaxRequestsPerFrame, handled, "a runaway chain stops at the cap");
        }

        [Test]
        public void KillingHit_RaisesEnemyDiedOnce_DeadTargetIgnored()
        {
            var dummy = new FakeTarget(Enemy(3), 100f);
            _damage.Apply(dummy, Request(150f), 2f);

            Assert.AreEqual(1, _died.Count);
            Assert.AreEqual(Enemy(3), _died[0].Enemy);
            Assert.AreEqual(P0, _died[0].Killer);
            Assert.IsTrue(_landed[0].Result.Killed);
            Assert.AreEqual(100f, _landed[0].Result.Dealt);

            _damage.Apply(dummy, Request(10f), 2.1f);
            Assert.AreEqual(1, _died.Count);
            Assert.AreEqual(1, _landed.Count, "a dead target takes no further hits");
        }

        [Test]
        public void PlayerTarget_RaisesPlayerDamaged_NotHitLanded()
        {
            var hero = new FakeTarget(SourceId.ForPlayer(new PlayerId(1)), 100f);
            _damage.Apply(hero, Request(10f, Vector2.zero, Enemy(0)), 1f);

            Assert.AreEqual(1, _playerDamaged.Count);
            Assert.AreEqual(1, _playerDamaged[0].Player.Value);
            Assert.AreEqual(Enemy(0), _playerDamaged[0].Result.Source);
            Assert.IsEmpty(_landed);
            Assert.AreEqual(90f, hero.Health.Current);
        }

        [Test]
        public void StatusPayload_GoesToApplier_OnlyWhenTheHitLands()
        {
            var shock = ScriptableObject.CreateInstance<StatusDefinition>();
            try
            {
                var spy = new SpyApplier();
                _damage.StatusApplier = spy;
                var dummy = new FakeTarget(Enemy(0), 100f);
                var req = new DamageRequest(P0, 5f, DamageTags.Shock, Vector2.zero, new StatusPayload(shock, 1), 1);

                _damage.Apply(dummy, req, 1f);
                Assert.AreEqual(1, spy.Calls);
                Assert.AreSame(shock, spy.Last.Def);

                dummy.Invulnerability.Register(2f, 1f, InvulnerabilitySource.Spawn);
                _damage.Apply(dummy, req, 2.5f);
                Assert.AreEqual(1, spy.Calls, "an avoided hit applies no status");

                _damage.StatusApplier = null; // back to the no-op default
                _damage.Apply(dummy, req, 5f);
                Assert.AreEqual(1, spy.Calls);
            }
            finally
            {
                Object.DestroyImmediate(shock);
            }
        }

        [Test]
        public void Restore_RefillsHp()
        {
            var dummy = new FakeTarget(Enemy(0), 100f);
            _damage.Apply(dummy, Request(100f), 1f);
            _damage.Restore(dummy, 100f);
            Assert.AreEqual(100f, dummy.Health.Current);
            Assert.IsTrue(dummy.IsAlive);
        }

        [TestCase("RaiseHitLanded")]
        [TestCase("RaiseHitAvoided")]
        [TestCase("RaisePlayerDamaged")]
        [TestCase("RaiseEnemyDied")]
        [TestCase("RaiseAbilityUsed")]
        public void DamageAndAbilityEvents_CanOnlyBeRaisedInsideCore(string raiser)
        {
            var m = typeof(CombatEvents).GetMethod(raiser, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(m, raiser);
            Assert.IsTrue(m.IsAssembly, $"{raiser} must be internal: only DamageSystem / AbilityRunner raise it (AD-16)");
        }

        [Test]
        public void HitIds_AreUnique()
        {
            int a = _damage.NextHitId();
            int b = _damage.NextHitId();
            Assert.AreNotEqual(a, b);
        }
    }
}

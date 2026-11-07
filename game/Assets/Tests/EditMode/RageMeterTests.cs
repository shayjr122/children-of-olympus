using System.Collections.Generic;
using GMR.Core.Combat;
using GMR.Core.Combo;
using GMR.Core.Enemies;
using GMR.Core.Players;
using GMR.Core.Rage;
using NUnit.Framework;
using UnityEngine;

namespace GMR.Tests
{
    public sealed class RageMeterTests
    {
        static readonly PlayerId P0 = new PlayerId(0);
        static readonly PlayerId P1 = new PlayerId(1);
        const float Frame = 1f / 60f;

        readonly List<RageChanged> _changed = new List<RageChanged>();
        readonly List<RageFull> _full = new List<RageFull>();
        CombatEvents _events;
        RageConfig _config;
        ComboConfig _combo;

        [SetUp]
        public void SetUp()
        {
            _events = new CombatEvents();
            _changed.Clear();
            _full.Clear();
            _events.OnRageChanged += _changed.Add;
            _events.OnRageFull += _full.Add;
            _config = ScriptableObject.CreateInstance<RageConfig>(); // defaults: 2% / 0.5% / +5% per 10 / 10%, 1.5 s, 8%/s
            _combo = ScriptableObject.CreateInstance<ComboConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_combo);
        }

        static HitLanded Hit(PlayerId player, DamageTags tags = DamageTags.Physical) =>
            new HitLanded(null, SourceId.ForEnemy(new EnemyId(7)), Vector2.zero,
                new DamageResult(10f, false, false, Vector2.zero, tags, SourceId.ForPlayer(player), HitWeight.Light, 1, 0f));

        PlayerCombatState BoundState(PlayerId player)
        {
            var state = new PlayerCombatState(player, _combo, _config, _events);
            state.Bind();
            return state;
        }

        [Test]
        public void DirectHit_Grants2Percent_AndRaisesRageChanged()
        {
            var state = BoundState(P0);
            _events.RaiseHitLanded(Hit(P0));
            Assert.AreEqual(0.02f, state.Rage.Value01, 1e-6f);
            Assert.AreEqual(1, _changed.Count);
            Assert.AreEqual(RageSource.Hit, _changed[0].Source);
            state.Unbind();
        }

        [Test]
        public void StatusTickAndChainJump_GrantHalfPercent()
        {
            var state = BoundState(P0);
            _events.RaiseHitLanded(Hit(P0, DamageTags.Shock | DamageTags.StatusTick));
            Assert.AreEqual(0.005f, state.Rage.Value01, 1e-6f);
            _events.RaiseHitLanded(Hit(P0, DamageTags.Shock | DamageTags.ChainJump));
            Assert.AreEqual(0.010f, state.Rage.Value01, 1e-6f);
            state.Unbind();
        }

        [Test]
        public void ComboMilestones_Grant5PercentEvery10()
        {
            var state = BoundState(P0);
            for (int i = 0; i < 9; i++) _events.RaiseHitLanded(Hit(P0));
            Assert.AreEqual(0.18f, state.Rage.Value01, 1e-5f);
            _events.RaiseHitLanded(Hit(P0)); // Combo 10: +2% hit +5% milestone
            Assert.AreEqual(0.25f, state.Rage.Value01, 1e-5f);
            for (int i = 0; i < 10; i++) _events.RaiseHitLanded(Hit(P0)); // Combo 20
            Assert.AreEqual(0.50f, state.Rage.Value01, 1e-5f);
            state.Unbind();
        }

        [Test]
        public void PerfectDodge_Grants10Percent_OnlyForOwnPlayer()
        {
            var state = BoundState(P0);
            _events.RaisePerfectDodged(new PerfectDodged(P1, 1, Vector2.zero, 0f));
            Assert.AreEqual(0f, state.Rage.Value01);
            _events.RaisePerfectDodged(new PerfectDodged(P0, 2, Vector2.zero, 0f));
            Assert.AreEqual(0.10f, state.Rage.Value01, 1e-6f);
            state.Unbind();
        }

        [Test]
        public void HitAvoided_GrantsNothing()
        {
            var state = BoundState(P0);
            _events.RaiseHitAvoided(new HitAvoided(null, SourceId.ForPlayer(P0), SourceId.ForEnemy(new EnemyId(1)), Vector2.zero, 3, 0f));
            _events.RaiseHitAvoided(new HitAvoided(null, SourceId.ForEnemy(new EnemyId(1)), SourceId.ForPlayer(P0), Vector2.zero, 4, 0f));
            Assert.AreEqual(0f, state.Rage.Value01);
            Assert.IsEmpty(_changed);
            state.Unbind();
        }

        [Test]
        public void OtherPlayersHits_GrantNothing()
        {
            var state = BoundState(P0);
            _events.RaiseHitLanded(Hit(P1));
            Assert.AreEqual(0f, state.Rage.Value01);
            state.Unbind();
        }

        [Test]
        public void UltimateHits_UseUltimateRate_DefaultZero()
        {
            var state = BoundState(P0);
            _events.RaiseHitLanded(Hit(P0, DamageTags.Shock | DamageTags.Ultimate));
            Assert.AreEqual(0f, state.Rage.Value01, "an Ultimate does not refill RAGE by default");
            Assert.AreEqual(1, state.Combo.Count, "but its hits count for Combo");
            state.Unbind();
        }

        [Test]
        public void UltimateHits_CrossingAComboMilestone_GrantNoMilestoneBonus()
        {
            var state = BoundState(P0);
            for (int i = 0; i < 8; i++) _events.RaiseHitLanded(Hit(P0));
            float before = state.Rage.Value01;
            for (int i = 0; i < 3; i++) _events.RaiseHitLanded(Hit(P0, DamageTags.Shock | DamageTags.Ultimate)); // Combo 9..11
            Assert.AreEqual(11, state.Combo.Count);
            Assert.AreEqual(before, state.Rage.Value01, 1e-6f, "an Ultimate never refills RAGE, milestones included");
            state.Unbind();
        }

        [Test]
        public void Clamp_0To1_AndRageFullRaisedOnce()
        {
            var rage = new RageMeter(P0, _config, _events);
            for (int i = 0; i < 12; i++) rage.Add(RageSource.PerfectDodge);
            Assert.AreEqual(1f, rage.Value01);
            Assert.IsTrue(rage.IsFull);
            Assert.AreEqual(1, _full.Count, "RageFull once per fill");
            Assert.IsFalse(rage.Add(RageSource.Hit, -0.5f), "negative amounts are ignored");
            Assert.AreEqual(1f, rage.Value01);

            rage.Consume();
            Assert.AreEqual(0f, rage.Value01);
            rage.Tick(10f);
            Assert.AreEqual(0f, rage.Value01, "never below 0");
            rage.Add(RageSource.Debug);
            Assert.AreEqual(2, _full.Count, "a second fill raises RageFull again");
        }

        [Test]
        public void Decay_StartsAfterGrace_At8PercentPerSecond()
        {
            var rage = new RageMeter(P0, _config, _events);
            rage.Add(RageSource.Hit, 0.5f);
            rage.Tick(1.5f);
            Assert.AreEqual(0.5f, rage.Value01, 1e-6f, "no decay during the 1.5 s grace");
            rage.Tick(1f);
            Assert.AreEqual(0.42f, rage.Value01, 1e-5f, "8% per second after the grace");
            rage.Add(RageSource.Hit);
            rage.Tick(1.4f);
            Assert.AreEqual(0.44f, rage.Value01, 1e-5f, "a gain restarts the grace");
        }

        [Test]
        public void FullRage_ReachesZero_InGracePlus12Point5Seconds()
        {
            var rage = new RageMeter(P0, _config, _events);
            rage.Add(RageSource.Debug);
            Assert.IsTrue(rage.IsFull);
            float t = 0f;
            bool lostFullAfterGrace = false;
            while (rage.Value01 > 0f && t < 20f)
            {
                rage.Tick(Frame);
                t += Frame;
                if (!rage.IsFull && !lostFullAfterGrace)
                {
                    lostFullAfterGrace = true;
                    Assert.That(t, Is.GreaterThan(1.5f - Frame), "full RAGE decays too, after the grace");
                }
            }
            Assert.AreEqual(1.5f + 12.5f, t, Frame + 1e-3f, "full RAGE reaches 0 at grace + 100/8 s (+-1 frame)");
        }

        [Test]
        public void DecayOverrideOff_HoldsValue()
        {
            var rage = new RageMeter(P0, _config, _events) { DecayOverride = false };
            rage.Add(RageSource.Debug);
            rage.Tick(30f);
            Assert.IsTrue(rage.IsFull);
        }
    }
}

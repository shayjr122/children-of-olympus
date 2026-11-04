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
    public sealed class ComboCounterTests
    {
        static readonly PlayerId P0 = new PlayerId(0);
        static readonly PlayerId P1 = new PlayerId(1);

        readonly List<ComboChanged> _changed = new List<ComboChanged>();
        readonly List<ComboReset> _resets = new List<ComboReset>();
        CombatEvents _events;
        ComboConfig _config;

        [SetUp]
        public void SetUp()
        {
            _events = new CombatEvents();
            _changed.Clear();
            _resets.Clear();
            _events.OnComboChanged += _changed.Add;
            _events.OnComboReset += _resets.Add;
            _config = ScriptableObject.CreateInstance<ComboConfig>(); // defaults: Fixed 2.0 s, 2.0 -> 1.0 over 0..50, min 0.8
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        static HitLanded Hit(SourceId source, DamageTags tags = DamageTags.Physical) =>
            new HitLanded(null, SourceId.ForEnemy(new EnemyId(7)), Vector2.zero,
                new DamageResult(10f, false, false, Vector2.zero, tags, source, HitWeight.Light, 1, 0f));

        static PlayerDamaged Damaged(PlayerId player) =>
            new PlayerDamaged(player, null, Vector2.zero,
                new DamageResult(10f, false, false, Vector2.zero, DamageTags.Physical, SourceId.ForEnemy(new EnemyId(1)), HitWeight.Light, 2, 0f));

        [Test]
        public void Hits_IncrementByOne_RepeatHitsCount_AndRaiseComboChanged()
        {
            var combo = new ComboCounter(P0, _config, _events);
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(combo.Handle(Hit(SourceId.ForPlayer(P0))));
                combo.Tick(0.5f);
            }
            Assert.AreEqual(5, combo.Count);
            Assert.AreEqual(5, _changed.Count);
            Assert.AreEqual(5, _changed[4].Count);
            Assert.AreEqual(2.0f, _changed[4].Window, 1e-5f);
        }

        [Test]
        public void Timeout_At2Seconds_ResetsWithTimeoutReason()
        {
            var combo = new ComboCounter(P0, _config, _events);
            combo.OnHit();
            combo.OnHit();
            combo.Tick(1.99f);
            Assert.AreEqual(2, combo.Count, "still inside the 2.0 s window");
            Assert.That(combo.WindowRemaining01, Is.InRange(0f, 0.01f));
            combo.Tick(0.02f);
            Assert.AreEqual(0, combo.Count);
            Assert.AreEqual(1, _resets.Count);
            Assert.AreEqual(ComboResetReason.Timeout, _resets[0].Reason);
            Assert.AreEqual(2, _resets[0].FinalCount);
        }

        [Test]
        public void HitInsideWindow_RestartsWindow()
        {
            var combo = new ComboCounter(P0, _config, _events);
            combo.OnHit();
            combo.Tick(1.9f);
            combo.OnHit();
            combo.Tick(1.9f);
            Assert.AreEqual(2, combo.Count);
            Assert.IsEmpty(_resets);
        }

        [Test]
        public void PlayerDamaged_ResetsImmediately_WithDamageReason()
        {
            var combo = new ComboCounter(P0, _config, _events);
            for (int i = 0; i < 18; i++) combo.OnHit();
            combo.Handle(Damaged(P0));
            Assert.AreEqual(0, combo.Count);
            Assert.AreEqual(1, _resets.Count);
            Assert.AreEqual(ComboResetReason.Damage, _resets[0].Reason);
            Assert.AreEqual(18, _resets[0].FinalCount);
        }

        [Test]
        public void OtherPlayersDamage_DoesNotReset_AndZeroComboRaisesNoReset()
        {
            var combo = new ComboCounter(P0, _config, _events);
            combo.Handle(Damaged(P0));
            Assert.IsEmpty(_resets, "no reset cue for an empty Combo");
            combo.OnHit();
            combo.Handle(Damaged(P1));
            Assert.AreEqual(1, combo.Count);
        }

        [Test]
        public void HitAvoided_DoesNotReset_NorGrantRage()
        {
            var rage = ScriptableObject.CreateInstance<RageConfig>();
            try
            {
                var state = new PlayerCombatState(P0, _config, rage, _events);
                state.Bind();
                for (int i = 0; i < 3; i++) state.Combo.OnHit();
                float before = state.Rage.Value01;
                _events.RaiseHitAvoided(new HitAvoided(null, SourceId.ForPlayer(P0), SourceId.ForEnemy(new EnemyId(1)), Vector2.zero, 5, 0f));
                Assert.AreEqual(3, state.Combo.Count);
                Assert.AreEqual(before, state.Rage.Value01);
                state.Unbind();
            }
            finally
            {
                Object.DestroyImmediate(rage);
            }
        }

        [Test]
        public void ShrinkingMode_At30_UsesCurve_AndNeverBelowMinimum()
        {
            var combo = new ComboCounter(P0, _config, _events) { ModeOverride = ComboMode.Shrinking };
            for (int i = 0; i < 30; i++) combo.OnHit();
            float expected = _config.ShrinkingCurve.Evaluate(30f); // 2.0 -> 1.0 over 0..50: 1.4 s at 30
            Assert.AreEqual(1.4f, expected, 1e-4f);
            Assert.AreEqual(expected, combo.Window, 1e-5f);

            _config.ShrinkingCurve = AnimationCurve.Linear(0f, 2.0f, 30f, 0.2f); // curve dips below the 0.8 s minimum
            combo.OnHit();
            Assert.AreEqual(0.8f, combo.Window, 1e-5f);
            Assert.AreEqual(0.8f, _config.WindowFor(30, ComboMode.Shrinking), 1e-5f);
        }

        [Test]
        public void FixedMode_IgnoresCurve()
        {
            var combo = new ComboCounter(P0, _config, _events);
            for (int i = 0; i < 30; i++) combo.OnHit();
            Assert.AreEqual(ComboMode.Fixed, combo.Mode);
            Assert.AreEqual(2.0f, combo.Window, 1e-5f);
        }

        [Test]
        public void OnlyOwnSourceId_IsCounted()
        {
            var combo = new ComboCounter(P0, _config, _events);
            Assert.IsFalse(combo.Handle(Hit(SourceId.ForPlayer(P1))));
            Assert.IsFalse(combo.Handle(Hit(SourceId.ForEnemy(new EnemyId(3)))));
            Assert.AreEqual(0, combo.Count);
            Assert.IsTrue(combo.Handle(Hit(SourceId.ForPlayer(P0))));
            Assert.AreEqual(1, combo.Count);
        }

        [Test]
        public void StatusTickAndChainHits_CountFully_ThroughPlayerCombatState()
        {
            var rage = ScriptableObject.CreateInstance<RageConfig>();
            try
            {
                var p0 = new PlayerCombatState(P0, _config, rage, _events);
                var p1 = new PlayerCombatState(P1, _config, rage, _events);
                p0.Bind();
                p1.Bind();
                _events.RaiseHitLanded(Hit(SourceId.ForPlayer(P0), DamageTags.Shock | DamageTags.StatusTick));
                _events.RaiseHitLanded(Hit(SourceId.ForPlayer(P0), DamageTags.Shock | DamageTags.ChainJump));
                _events.RaiseHitLanded(Hit(SourceId.ForPlayer(P0)));
                Assert.AreEqual(3, p0.Combo.Count, "status ticks and chain jumps count fully toward Combo");
                Assert.AreEqual(0, p1.Combo.Count, "Player 1 has an independent Combo");
                _events.RaisePlayerDamaged(Damaged(P1));
                Assert.AreEqual(3, p0.Combo.Count, "Player 1's damage does not reset Player 0");
                p0.Unbind();
                p1.Unbind();
                _events.RaiseHitLanded(Hit(SourceId.ForPlayer(P0)));
                Assert.AreEqual(3, p0.Combo.Count, "unbound state ignores events");
            }
            finally
            {
                Object.DestroyImmediate(rage);
            }
        }
    }
}

using System.Collections.Generic;
using GMR.Core.Abilities;
using GMR.Core.Combat;
using GMR.Core.Players;
using GMR.Core.Rage;
using NUnit.Framework;
using UnityEngine;

namespace GMR.Tests
{
    public sealed class UltimateGateTests
    {
        static readonly PlayerId P0 = new PlayerId(0);
        static readonly PlayerId P1 = new PlayerId(1);

        readonly List<AbilityUsed> _used = new List<AbilityUsed>();
        readonly List<UltimateUsed> _ultimates = new List<UltimateUsed>();
        readonly List<UltimateNotReady> _notReady = new List<UltimateNotReady>();
        CombatEvents _events;
        RageConfig _rageConfig;
        AbilityDefinition _wrath;
        RageMeter _rage;
        AbilityRunner _runner;
        InvulnerabilityTracker _invulnerability;
        UltimateGate _gate;

        [SetUp]
        public void SetUp()
        {
            _events = new CombatEvents();
            _used.Clear();
            _ultimates.Clear();
            _notReady.Clear();
            _events.OnAbilityUsed += _used.Add;
            _events.OnUltimateUsed += _ultimates.Add;
            _events.OnUltimateNotReady += _notReady.Add;
            _rageConfig = ScriptableObject.CreateInstance<RageConfig>();
            _wrath = ScriptableObject.CreateInstance<AbilityDefinition>();
            _wrath.HitShape = HitShape.AllEnemies;
            _wrath.DamageTags = DamageTags.Shock | DamageTags.Ultimate;
            _wrath.Startup = 0.35f;
            _wrath.Active = 0.1f;
            _wrath.Recovery = 0.4f;
            _wrath.Cooldown = 0f;
            _rage = new RageMeter(P0, _rageConfig, _events);
            _runner = new AbilityRunner(P0, AbilitySlot.Ultimate, _wrath, _events);
            _invulnerability = new InvulnerabilityTracker();
            _gate = new UltimateGate(P0, _events);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rageConfig);
            Object.DestroyImmediate(_wrath);
        }

        UltimateGateResult Press(float now, bool busy = false) =>
            _gate.TryFire(_rage, _runner, _invulnerability, busy, now, Vector2.zero, Vector2.right);

        [Test]
        public void BelowFull_InputIgnored_NotReadyRaised()
        {
            _rage.Add(RageSource.Hit, 0.99f);
            Assert.AreEqual(UltimateGateResult.NotReady, Press(1f));
            Assert.AreEqual(AbilityPhase.Ready, _runner.Phase, "nothing fires below 100%");
            Assert.IsEmpty(_used);
            Assert.IsEmpty(_ultimates);
            Assert.AreEqual(1, _notReady.Count);
            Assert.AreEqual(0.99f, _notReady[0].Rage01, 1e-6f);
            Assert.AreEqual(0.99f, _rage.Value01, 1e-6f, "RAGE untouched");
            Assert.AreEqual(0, _invulnerability.ActiveWindowCount(1f));
        }

        [Test]
        public void Full_Fires_ConsumesToZero_AndAbilityRunnerRaisesUltimateUsed()
        {
            _rage.Add(RageSource.Debug);
            Assert.AreEqual(UltimateGateResult.Fired, Press(2f));
            Assert.AreEqual(0f, _rage.Value01, "Consume sets RAGE to 0");
            Assert.AreEqual(AbilityPhase.Startup, _runner.Phase);
            Assert.AreEqual(1, _used.Count);
            Assert.AreEqual(AbilitySlot.Ultimate, _used[0].Slot);
            Assert.AreEqual(1, _ultimates.Count);
            Assert.AreSame(_wrath, _ultimates[0].Ability);
            Assert.IsEmpty(_notReady);
            Assert.AreEqual(UltimateGateResult.Waiting, Press(2.01f), "the Ultimate is still running");
            _runner.Tick(3f);
            Assert.AreEqual(AbilityPhase.Ready, _runner.Phase);
            Assert.AreEqual(UltimateGateResult.NotReady, Press(3f), "RAGE is spent");
        }

        [Test]
        public void Fire_RegistersIFrameWindow_CoveringStartup()
        {
            _rage.Add(RageSource.Debug);
            Press(2f);
            Assert.IsTrue(_invulnerability.IsInvulnerableFrom(InvulnerabilitySource.UltimateStartup, 2f));
            Assert.IsTrue(_invulnerability.IsInvulnerableFrom(InvulnerabilitySource.UltimateStartup, 2.34f));
            Assert.IsFalse(_invulnerability.IsInvulnerable(2.36f), "i-frames cover the startup only");
        }

        [Test]
        public void Full_ButHeroBusy_Waits_WithoutConsuming()
        {
            _rage.Add(RageSource.Debug);
            Assert.AreEqual(UltimateGateResult.Waiting, Press(1f, busy: true));
            Assert.IsTrue(_rage.IsFull);
            Assert.IsEmpty(_used);
            Assert.IsEmpty(_notReady);
        }

        [Test]
        public void BelowFull_WhileBusy_Waits_WithoutNotReadyCue()
        {
            _rage.Add(RageSource.Hit, 0.5f);
            Assert.AreEqual(UltimateGateResult.Waiting, Press(1f, busy: true), "RAGE is judged once the Hero is free");
            Assert.IsEmpty(_notReady);
            Assert.AreEqual(UltimateGateResult.NotReady, Press(1.2f));
            Assert.AreEqual(1, _notReady.Count);
        }

        [Test]
        public void UltimateUsedSubscribers_SeeRageConsumed_AndIFramesRegistered()
        {
            float rageSeen = -1f;
            bool invulnerableSeen = false;
            _events.OnUltimateUsed += e =>
            {
                rageSeen = _rage.Value01;
                invulnerableSeen = _invulnerability.IsInvulnerableFrom(InvulnerabilitySource.UltimateStartup, e.At);
            };
            _rage.Add(RageSource.Debug);
            Press(3f);
            Assert.AreEqual(0f, rageSeen);
            Assert.IsTrue(invulnerableSeen);
        }

        [Test]
        public void FiftyHits_FillRageExactly()
        {
            for (int i = 0; i < 50; i++) _rage.Add(RageSource.Hit);
            Assert.IsTrue(_rage.IsFull, "float sums of 2% snap to a full meter");
            Assert.AreEqual(UltimateGateResult.Fired, Press(1f));
        }

        [Test]
        public void NoUltimateInSlot_ReturnsNoUltimate()
        {
            _rage.Add(RageSource.Debug);
            Assert.AreEqual(UltimateGateResult.NoUltimate, _gate.TryFire(_rage, null, _invulnerability, false, 1f, Vector2.zero, Vector2.right));
            Assert.IsTrue(_rage.IsFull);
        }

        [Test]
        public void OtherPlayersRage_IsRejected()
        {
            var other = new RageMeter(P1, _rageConfig, _events);
            Assert.Throws<System.ArgumentException>(() =>
                _gate.TryFire(other, _runner, _invulnerability, false, 1f, Vector2.zero, Vector2.right));
        }

        [Test]
        public void NonUltimateSlots_NeverRaiseUltimateUsed()
        {
            var special = new AbilityRunner(P0, AbilitySlot.Special, _wrath, _events);
            Assert.IsTrue(special.TryStart(0f, Vector2.zero, Vector2.right));
            Assert.AreEqual(1, _used.Count);
            Assert.IsEmpty(_ultimates);
        }

        [Test]
        public void Loadout_NeverStartsUltimateFromBuffer()
        {
            var input = ScriptableObject.CreateInstance<GMR.Core.Controls.InputConfig>();
            try
            {
                var buffer = new GMR.Core.Controls.InputBuffer(input);
                var loadout = new AbilityLoadout(P0, _events, null, null, null, _wrath);
                Assert.IsNotNull(loadout.Get(AbilitySlot.Ultimate));
                buffer.Press(GMR.Core.Controls.BufferedAction.Ultimate, 0f);
                Assert.IsNull(loadout.TryStartBuffered(buffer, 0f, Vector2.zero, Vector2.right));
                Assert.IsEmpty(_used);
            }
            finally
            {
                Object.DestroyImmediate(input);
            }
        }
    }
}

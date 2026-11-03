using System.Collections.Generic;
using GMR.Core.Abilities;
using GMR.Core.Combat;
using GMR.Core.Controls;
using GMR.Core.Heroes;
using GMR.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace GMR.Tests
{
    public sealed class AbilityRunnerTests
    {
        readonly List<Object> _assets = new List<Object>();
        readonly List<AbilityUsed> _used = new List<AbilityUsed>();
        CombatEvents _events;
        static readonly PlayerId P0 = new PlayerId(0);

        [SetUp]
        public void SetUp()
        {
            _events = new CombatEvents();
            _used.Clear();
            _events.OnAbilityUsed += _used.Add;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var a in _assets) Object.DestroyImmediate(a);
            _assets.Clear();
        }

        AbilityDefinition Def(float startup = 0.1f, float active = 0.1f, float recovery = 0.2f, float cooldown = 3f,
            HitShape shape = HitShape.Box)
        {
            var d = ScriptableObject.CreateInstance<AbilityDefinition>();
            d.Startup = startup;
            d.Active = active;
            d.Recovery = recovery;
            d.Cooldown = cooldown;
            d.HitShape = shape;
            _assets.Add(d);
            return d;
        }

        InputBuffer Buffer(float window = 0.12f)
        {
            var c = ScriptableObject.CreateInstance<InputConfig>();
            c.BufferWindow = window;
            _assets.Add(c);
            return new InputBuffer(c);
        }

        [Test]
        public void Phases_FollowStartupActiveRecovery()
        {
            var r = new AbilityRunner(P0, AbilitySlot.Special, Def(), _events);
            Assert.IsTrue(r.TryStart(0f, Vector2.zero, Vector2.up));
            Assert.AreEqual(AbilityPhase.Startup, r.Phase);
            Assert.AreEqual(Vector2.up, r.Direction);

            Assert.AreEqual(AbilityStep.None, r.Tick(0.05f));
            Assert.AreEqual(AbilityStep.EnteredActive, r.Tick(0.1f));
            Assert.AreEqual(AbilityPhase.Active, r.Phase);
            Assert.AreEqual(AbilityStep.ExitedActive, r.Tick(0.2f));
            Assert.AreEqual(AbilityPhase.Recovery, r.Phase);
            Assert.AreEqual(AbilityStep.None, r.Tick(0.35f));
            Assert.AreEqual(AbilityStep.Finished, r.Tick(0.4f));
            Assert.AreEqual(AbilityPhase.Ready, r.Phase);
        }

        [Test]
        public void LongFrame_ReportsEveryTransition()
        {
            var r = new AbilityRunner(P0, AbilitySlot.Basic, Def(0f, 0.02f, 0.05f), _events);
            r.TryStart(0f, Vector2.zero, Vector2.right);
            var step = r.Tick(0.1f);
            Assert.AreEqual(AbilityStep.EnteredActive | AbilityStep.ExitedActive | AbilityStep.Finished, step,
                "a frame longer than the whole ability still reports Active so the resolver runs once");
        }

        [Test]
        public void TryStart_RaisesAbilityUsedOnce_WithSlotAndPlayer()
        {
            var def = Def();
            var r = new AbilityRunner(new PlayerId(1), AbilitySlot.Signature, def, _events);
            r.TryStart(0f, new Vector2(3f, 4f), Vector2.left);
            Assert.IsFalse(r.TryStart(0.01f, Vector2.zero, Vector2.left), "busy");
            Assert.AreEqual(1, _used.Count);
            Assert.AreEqual(1, _used[0].Player.Value);
            Assert.AreEqual(AbilitySlot.Signature, _used[0].Slot);
            Assert.AreSame(def, _used[0].Ability);
            Assert.AreEqual(new Vector2(3f, 4f), _used[0].Position);
        }

        [Test]
        public void Cooldown_BlocksUntilItEnds()
        {
            var r = new AbilityRunner(P0, AbilitySlot.Special, Def(cooldown: 3f), _events);
            r.TryStart(0f, Vector2.zero, Vector2.right);
            r.Tick(0.4f);
            Assert.AreEqual(AbilityPhase.Ready, r.Phase);
            Assert.AreEqual(2f, r.CooldownRemaining(1f), 1e-5f);
            Assert.IsFalse(r.CanStart(1f));
            Assert.IsFalse(r.TryStart(2.99f, Vector2.zero, Vector2.right), "on cooldown: nothing fires");
            Assert.AreEqual(1, _used.Count);
            Assert.IsTrue(r.TryStart(3f, Vector2.zero, Vector2.right));
        }

        [Test]
        public void BasicSlot_ForcesCooldownZero_WithoutChangingTheAsset()
        {
            var def = Def(cooldown: 3f);
            var basic = new AbilityRunner(P0, AbilitySlot.Basic, def, _events);
            Assert.AreEqual(0f, basic.EffectiveCooldown);
            basic.TryStart(0f, Vector2.zero, Vector2.right);
            basic.Tick(0.4f);
            Assert.IsTrue(basic.TryStart(0.4f, Vector2.zero, Vector2.right), "Basic is usable again right after recovery");
            Assert.AreEqual(3f, def.Cooldown, "the shared asset keeps its cooldown for other slots");
            Assert.AreEqual(3f, new AbilityRunner(P0, AbilitySlot.Special, def, _events).EffectiveCooldown);
        }

        [Test]
        public void DashCancel_OnlyDuringRecovery()
        {
            var loadout = new AbilityLoadout(P0, _events, Def(), null, null);
            var r = loadout.Get(AbilitySlot.Basic);
            r.TryStart(0f, Vector2.zero, Vector2.right);

            Assert.IsTrue(loadout.BlocksDash, "Startup blocks DASH");
            Assert.IsFalse(loadout.CancelRecovery());
            r.Tick(0.15f);
            Assert.AreEqual(AbilityPhase.Active, r.Phase);
            Assert.IsTrue(loadout.BlocksDash, "Active blocks DASH");
            Assert.IsFalse(loadout.CancelRecovery());

            r.Tick(0.25f);
            Assert.AreEqual(AbilityPhase.Recovery, r.Phase);
            Assert.IsFalse(loadout.BlocksDash, "Recovery can be dashed out of");
            Assert.IsTrue(loadout.CancelRecovery());
            Assert.AreEqual(AbilityPhase.Ready, r.Phase);
            Assert.IsNull(loadout.Current);
        }

        [Test]
        public void BufferedPress_ExecutesWhenLegal_Once()
        {
            var buffer = Buffer(0.12f);
            var loadout = new AbilityLoadout(P0, _events, Def(cooldown: 0f), null, null);
            var basic = loadout.Get(AbilitySlot.Basic);
            basic.TryStart(0f, Vector2.zero, Vector2.right);
            basic.Tick(0.3f); // Recovery until 0.4

            buffer.Press(BufferedAction.Basic, 0.3f);
            Assert.IsNull(loadout.TryStartBuffered(buffer, 0.3f, Vector2.zero, Vector2.right), "busy: the press waits");

            basic.Tick(0.4f);
            Assert.AreSame(basic, loadout.TryStartBuffered(buffer, 0.4f, Vector2.zero, Vector2.right), "legal 0.1 s later: fires");
            Assert.AreEqual(2, _used.Count);

            basic.Tick(0.9f);
            Assert.IsNull(loadout.TryStartBuffered(buffer, 0.9f, Vector2.zero, Vector2.right), "a press is consumed once");
        }

        [Test]
        public void BufferedPress_OlderThanWindow_IsDropped()
        {
            var buffer = Buffer(0.12f);
            var loadout = new AbilityLoadout(P0, _events, Def(cooldown: 0f), null, null);
            var basic = loadout.Get(AbilitySlot.Basic);
            basic.TryStart(0f, Vector2.zero, Vector2.right);
            buffer.Press(BufferedAction.Basic, 0.2f);
            basic.Tick(0.4f);
            Assert.IsNull(loadout.TryStartBuffered(buffer, 0.4f, Vector2.zero, Vector2.right));
            Assert.AreEqual(1, _used.Count);
        }

        [Test]
        public void BufferWindowZero_PressOnlyWorksInItsFrame()
        {
            var buffer = Buffer(0f);
            var loadout = new AbilityLoadout(P0, _events, Def(cooldown: 0f), null, null);
            buffer.Press(BufferedAction.Basic, 1f);
            Assert.IsNotNull(loadout.TryStartBuffered(buffer, 1f, Vector2.zero, Vector2.right));
        }

        [Test]
        public void SpecialOnCooldown_PressDoesNothing_OtherSlotsStillWork()
        {
            var buffer = Buffer();
            var loadout = new AbilityLoadout(P0, _events, Def(cooldown: 0f), Def(cooldown: 3f), Def(cooldown: 8f));
            var special = loadout.Get(AbilitySlot.Special);
            special.TryStart(0f, Vector2.zero, Vector2.right);
            special.Tick(0.4f);

            buffer.Press(BufferedAction.Special, 1f);
            Assert.IsNull(loadout.TryStartBuffered(buffer, 1f, Vector2.zero, Vector2.right));
            buffer.Press(BufferedAction.Signature, 1.01f);
            Assert.AreSame(loadout.Get(AbilitySlot.Signature), loadout.TryStartBuffered(buffer, 1.01f, Vector2.zero, Vector2.right));
        }

        [Test]
        public void EmptySlot_PressIsConsumedAndIgnored()
        {
            var buffer = Buffer();
            var loadout = new AbilityLoadout(P0, _events, Def(), null, null);
            buffer.Press(BufferedAction.Special, 0f);
            Assert.IsNull(loadout.TryStartBuffered(buffer, 0f, Vector2.zero, Vector2.right));
            Assert.IsFalse(buffer.IsBuffered(BufferedAction.Special, 0f));
        }

        [Test]
        public void SlotAgnostic_ShotAssetRunsInTheBasicSlot()
        {
            var shot = Def(cooldown: 3f, shape: HitShape.Projectile);
            var loadout = new AbilityLoadout(P0, _events, shot, null, null);
            var r = loadout.Get(AbilitySlot.Basic);
            Assert.IsTrue(r.TryStart(0f, Vector2.zero, Vector2.right));
            r.Tick(1f);
            Assert.IsTrue(r.TryStart(1f, Vector2.zero, Vector2.right), "no cooldown in Basic");
        }

        [Test]
        public void RootsCaster_OnlyWhileTheRootingAbilityRuns()
        {
            var rooting = Def(cooldown: 0f);
            rooting.RootsCaster = true;
            var loadout = new AbilityLoadout(P0, _events, rooting, null, null);
            Assert.IsFalse(loadout.RootsCaster);
            loadout.Get(AbilitySlot.Basic).TryStart(0f, Vector2.zero, Vector2.right);
            Assert.IsTrue(loadout.RootsCaster);
            loadout.Get(AbilitySlot.Basic).Tick(1f);
            Assert.IsFalse(loadout.RootsCaster);
        }

        [Test]
        public void HeroDefinition_SlotsAreReadBySlot()
        {
            var hero = ScriptableObject.CreateInstance<HeroDefinition>();
            _assets.Add(hero);
            var a = Def();
            var b = Def();
            hero.Basic = a;
            hero.Signature = b;
            Assert.AreSame(a, hero.Get(AbilitySlot.Basic));
            Assert.AreSame(b, hero.Get(AbilitySlot.Signature));
            Assert.IsNull(hero.Get(AbilitySlot.Special));
        }
    }
}

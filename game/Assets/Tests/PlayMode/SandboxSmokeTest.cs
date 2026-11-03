using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GMR.Core.Abilities;
using GMR.Core.Combat;
using GMR.Core.Heroes;
using GMR.Core.Players;
using GMR.Runtime.Encounter;
using GMR.Runtime.Feedback;
using GMR.Runtime.Heroes;
using GMR.Runtime.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GMR.Tests
{
    /// <summary>
    /// Sandbox smoke test. M4a: loads Sandbox with a virtual gamepad, checks Player 0 joins through PlayerInputManager,
    /// drives a real DASH through the input pipeline (gamepad South -> PlayerInputReader -> InputBuffer ->
    /// DashController), checks a Perfect Dodge against a pending hit and that pause goes through TimeScaleService.
    /// M4b: B closes the menu without firing an ability; Club Strike (X), Thunderbolt (Y) and Storm Whirl (B) each fire
    /// once through the input pipeline and damage training dummies; Special cooldown; dummy death + respawn; Player
    /// damage with post-hit invulnerability; SFX hooks fire.
    /// </summary>
    public sealed class SandboxSmokeTest : InputTestFixture
    {
        Gamepad _gamepad;
        Keyboard _keyboard;

        public override void Setup()
        {
            base.Setup();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            Time.captureFramerate = 60; // fixed 1/60 s of game time per frame, so the 80 ms window test is stable
        }

        public override void TearDown()
        {
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            base.TearDown();
        }

        IEnumerator Tap(UnityEngine.InputSystem.Controls.ButtonControl button)
        {
            Press(button);
            yield return null;
            Release(button);
            yield return null;
        }

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        static void Teleport(HeroRuntime hero, Vector2 position)
        {
            hero.Body.position = position;
            hero.transform.position = position;
            hero.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }

        [UnityTest]
        public IEnumerator Sandbox_Player0Joins_Dashes_PerfectDodges_AndPauses()
        {
            SceneManager.LoadScene("Sandbox");
            yield return Frames(3);

            var bootstrap = Object.FindAnyObjectByType<EncounterBootstrap>();
            Assert.IsNotNull(bootstrap, "EncounterBootstrap missing from Sandbox");
            Assert.AreEqual(1, bootstrap.PlayerRoots.Count, "Player 0 should join on start");
            var context = bootstrap.Context;
            Assert.AreEqual(1, context.Players.Count);
            Assert.AreEqual(0, context.Players.All[0].Id.Value);

            var root = bootstrap.PlayerRoots[0];
            Assert.IsNotNull(root.Hero, "HeroRuntime should be built from the HeroDefinition");
            Assert.AreEqual("heracles", root.Hero.Definition.Id);
            Assert.That(context.Enemies.Count, Is.GreaterThanOrEqualTo(1), "dummy attackers register as enemies");

            // A pending hit landing soon right on the Hero: a DASH now is a Perfect Dodge.
            int perfects = 0;
            int dashes = 0;
            context.Events.OnPerfectDodged += _ => perfects++;
            context.Events.OnDashStarted += _ => dashes++;
            int attackId = context.PendingHits.NextAttackId();
            context.PendingHits.Register(new PendingHit(attackId, Time.time + 0.06f, root.Hero.Position, 1f));

            Vector2 before = root.Hero.Position;
            Press(_gamepad.buttonSouth);
            yield return null;
            Release(_gamepad.buttonSouth);
            yield return null;

            Assert.AreEqual(1, dashes, "gamepad South should start exactly one DASH");
            Assert.AreEqual(1, perfects, "DASH inside the Perfect Dodge Window should raise PerfectDodged once");
            Assert.AreNotEqual(DashPhase.Ready, root.Hero.Dash.Phase);
            Assert.IsTrue(root.IsInvulnerable(Time.time), "DASH start must register i-frames");
            context.PendingHits.Remove(attackId);

            // Let the DASH finish: the Hero travelled roughly DashConfig.Distance, and i-frames ended.
            yield return new WaitForSeconds(0.4f);
            float travelled = Vector2.Distance(before, root.Hero.Position);
            Assert.That(travelled, Is.InRange(2f, 3.6f), $"DASH travel {travelled} should be about 3 units");
            Assert.IsFalse(root.IsInvulnerable(Time.time), "i-frames only cover the start of the DASH");
            Assert.AreEqual(DashPhase.Ready, root.Hero.Dash.Phase);

            // Pause through input (Start) -> TimeScaleService.
            var pause = Object.FindAnyObjectByType<PauseController>();
            Assert.IsNotNull(pause);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Tap(_gamepad.startButton);
            Assert.IsTrue(pause.IsOpen, "Start should open the pause menu");
            Assert.AreEqual(0f, Time.timeScale);

            // Debug button (Select) toggles the same menu.
            yield return Tap(_gamepad.selectButton);
            Assert.IsFalse(pause.IsOpen, "Select should close the menu");
            Assert.AreEqual(1f, Time.timeScale);
            yield return Tap(_gamepad.selectButton);
            Assert.IsTrue(pause.IsOpen, "Select should open the menu");

            // A South press while paused is a menu press (it may click Resume) and must not DASH after resuming.
            yield return Tap(_gamepad.buttonSouth);
            if (pause.IsOpen) yield return Tap(_gamepad.startButton);
            yield return Frames(3);
            Assert.IsFalse(pause.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(1, dashes, "a press made while paused must not DASH on resume");

            // M4b deferred item: B (UI Cancel) closes the menu, and that B must not fire Storm Whirl (B) on resume.
            var used = new List<AbilityUsed>();
            context.Events.OnAbilityUsed += used.Add;
            yield return Tap(_gamepad.startButton);
            Assert.IsTrue(pause.IsOpen);
            yield return Tap(_gamepad.buttonEast);
            yield return Frames(3);
            Assert.IsFalse(pause.IsOpen, "gamepad B (UI Cancel) should close the menu");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsEmpty(used, "the B that closed the menu must not fire an ability");

            // Keyboard Esc is both Pause and UI Cancel: it must open, then close, without flickering.
            // The fixture does not auto-switch schemes, so pair the keyboard explicitly (a real single Player auto-switches).
            var playerInput = bootstrap.PlayerRoots[0].GetComponent<PlayerInput>();
            playerInput.SwitchCurrentControlScheme("Keyboard&Mouse", _keyboard);
            yield return Frames(1);
            yield return Tap(_keyboard.escapeKey);
            yield return Frames(2);
            Assert.IsTrue(pause.IsOpen, "Esc should open the menu (and not cancel it in the same frame)");
            yield return Tap(_keyboard.escapeKey);
            yield return Frames(2);
            Assert.IsFalse(pause.IsOpen, "Esc should close the menu (and not reopen it in the same frame)");
            Assert.AreEqual(1f, Time.timeScale);
            playerInput.SwitchCurrentControlScheme("Gamepad", _gamepad);
            yield return Frames(1);

            yield return AbilitiesHitTrainingDummies(context, root, used);
        }

        IEnumerator AbilitiesHitTrainingDummies(GMR.Core.Encounter.EncounterContext context, GMR.Runtime.Players.PlayerRoot root,
            List<AbilityUsed> used)
        {
            var dummies = context.Enemies.All.OfType<IDamageable>().ToList();
            Assert.AreEqual(3, dummies.Count, "three training dummies register as damageable enemies");
            var hits = new List<HitLanded>();
            context.Events.OnHitLanded += hits.Add;
            var sfx = Object.FindAnyObjectByType<SfxHookPlayer>();
            Assert.IsNotNull(sfx, "SFX hook player missing");
            var cues = new List<SfxCue>();
            sfx.CueFired += (cue, _) => cues.Add(cue);
            var p0 = SourceId.ForPlayer(new PlayerId(0));

            var front = dummies.OrderBy(d => d.Position.x).First(); // TrainingDummy_1 at (-0.3, 0)
            var hero = root.Hero;
            // FeedbackRouter subscribed first, so this sees whether it asked for hitstop for the same hit.
            var hitstopRequests = new List<int>();
            context.Events.OnHitLanded += _ => hitstopRequests.Add(context.TimeScale.ActiveCount);

            // Club Strike (Basic, gamepad X / West): Thrust box in front of the Hero.
            Teleport(hero, front.Position + new Vector2(-1f, 0f));
            root.Aim.Update(Vector2.right, false, false);
            yield return Frames(2);
            float frontXBefore = front.Position.x;
            yield return Tap(_gamepad.buttonWest);
            yield return new WaitForSeconds(0.45f);
            Assert.Greater(front.Position.x, frontXBefore + 0.02f, "knockback should push the dummy away from the Hero");
            Assert.That(hitstopRequests, Is.Not.Empty);
            Assert.GreaterOrEqual(hitstopRequests[0], 1, "a landed hit requests hitstop through TimeScaleService");
            Assert.AreEqual(0, context.Damage.RequestsThisFrame, "the DamageSystem cap window resets every frame");
            Assert.IsTrue(used.Any(u => u.Slot == AbilitySlot.Basic && u.Ability.Id == "heracles_club_strike"), "X should use Club Strike");
            Assert.IsTrue(hits.Any(h => h.Target == front && h.Source == p0), "Club Strike should hit the dummy in front");
            Assert.Less(front.Health.Current, front.Health.Max);

            // Thunderbolt (Special, gamepad Y / North): projectile along the aim hits the first dummy.
            int hitsBefore = hits.Count;
            int stopsBefore = hitstopRequests.Count;
            context.Toggles.Hitstop = false; // pause-menu override: no hitstop request
            Teleport(hero, front.Position + new Vector2(-4f, 0f));
            root.Aim.Update(Vector2.right, false, false);
            yield return Frames(2);
            yield return Tap(_gamepad.buttonNorth);
            yield return new WaitForSeconds(0.7f);
            Assert.AreEqual(1, used.Count(u => u.Slot == AbilitySlot.Special), "Y should use Thunderbolt once");
            Assert.IsTrue(hits.Skip(hitsBefore).Any(h => h.Target == front && (h.Result.Tags & DamageTags.Shock) != 0),
                "the Thunderbolt projectile should hit the first dummy");
            Assert.IsTrue(hitstopRequests.Skip(stopsBefore).All(n => n == 0), "Hitstop toggled off: no time-scale request");
            context.Toggles.Hitstop = null;

            // Special is on cooldown (3 s): pressing again does nothing.
            yield return Tap(_gamepad.buttonNorth);
            yield return Frames(10);
            Assert.AreEqual(1, used.Count(u => u.Slot == AbilitySlot.Special), "Thunderbolt is on cooldown");
            Assert.Greater(hero.Abilities.Get(AbilitySlot.Special).CooldownRemaining(Time.time), 0f);

            // DASH during Club Strike Startup waits (no DASH); DASH during Recovery cancels it (FR-6).
            int dashes = 0;
            context.Events.OnDashStarted += _ => dashes++;
            var basic = hero.Abilities.Get(AbilitySlot.Basic);
            context.Toggles.InputBuffering = false; // a buffered DASH would fire the moment Recovery starts
            yield return Frames(1);
            yield return Tap(_gamepad.buttonWest);
            Assert.AreNotEqual(AbilityPhase.Ready, basic.Phase);
            Press(_gamepad.buttonSouth);
            yield return null;
            Release(_gamepad.buttonSouth);
            Assert.AreEqual(0, dashes, "no DASH during ability Startup/Active");
            for (int i = 0; i < 60 && basic.Phase != AbilityPhase.Recovery; i++) yield return null;
            Assert.AreEqual(AbilityPhase.Recovery, basic.Phase);
            yield return Tap(_gamepad.buttonSouth);
            Assert.AreEqual(1, dashes, "DASH in Recovery starts");
            Assert.IsNull(hero.Abilities.Current, "and cancels the Recovery");
            context.Toggles.InputBuffering = null;

            // Storm Whirl (Signature, gamepad B / East): every dummy inside the radius around the Hero.
            yield return new WaitForSeconds(1.5f); // dummies walk back to their spots
            hitsBefore = hits.Count;
            Teleport(hero, new Vector2(0.8f, 0f));
            yield return Frames(2);
            yield return Tap(_gamepad.buttonEast);
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(1, used.Count(u => u.Slot == AbilitySlot.Signature), "B should use Storm Whirl once");
            int whirled = hits.Skip(hitsBefore).Select(h => h.TargetId).Distinct().Count();
            Assert.AreEqual(3, whirled, "Storm Whirl should hit all three dummies");
            Assert.IsTrue(cues.Contains(SfxCue.Hit) || cues.Contains(SfxCue.HeavyHit), "hit SFX hook should fire");

            // Dummy death and respawn.
            int deaths = 0;
            context.Events.OnEnemyDied += _ => deaths++;
            var victim = dummies.First(d => d != front);
            context.Damage.Apply(victim, new DamageRequest(p0, 1000f, DamageTags.Physical, Vector2.zero, default,
                context.Damage.NextHitId()), Time.time);
            Assert.AreEqual(1, deaths, "EnemyDied at 0 HP");
            Assert.IsFalse(victim.IsAlive);
            yield return new WaitForSeconds(2f);
            Assert.IsTrue(victim.IsAlive, "the dummy respawns after the configured delay");
            Assert.AreEqual(victim.Health.Max, victim.Health.Current);

            // Player damage: PlayerDamaged + 0.5 s post-hit invulnerability; an immediate second hit is avoided.
            int playerHits = 0, avoided = 0;
            context.Events.OnPlayerDamaged += _ => playerHits++;
            context.Events.OnHitAvoided += _ => avoided++;
            var enemySource = SourceId.ForEnemy(new GMR.Core.Enemies.EnemyId(99));
            float hpBefore = hero.Health.Current;
            context.Damage.Apply(root.Damageable, new DamageRequest(enemySource, 10f, DamageTags.Physical, Vector2.zero,
                default, context.Damage.NextHitId(), HitWeight.Heavy), Time.time);
            Assert.AreEqual(1, playerHits);
            Assert.AreEqual(hpBefore - 10f, hero.Health.Current);
            Assert.IsTrue(hero.Invulnerability.IsInvulnerableFrom(GMR.Core.Combat.InvulnerabilitySource.PostHit, Time.time));
            context.Damage.Apply(root.Damageable, new DamageRequest(enemySource, 10f, DamageTags.Physical, Vector2.zero,
                default, context.Damage.NextHitId()), Time.time);
            Assert.AreEqual(1, avoided, "post-hit invulnerability avoids the next hit");
            Assert.IsTrue(cues.Contains(SfxCue.PlayerHurt));

            // A DummyAttacker slam on the Hero goes through DamageSystem: PlayerDamaged with the configured damage.
            var attacker = GameObject.Find("DummyAttacker_A");
            Assert.IsNotNull(attacker);
            yield return new WaitForSeconds(0.6f); // post-hit invulnerability over
            var slams = new List<PlayerDamaged>();
            context.Events.OnPlayerDamaged += slams.Add;
            Teleport(hero, (Vector2)attacker.transform.position + new Vector2(0.9f, 0f));
            for (int i = 0; i < 300 && slams.Count == 0; i++) yield return null;
            Assert.IsNotEmpty(slams, "the dummy attacker's slam should damage the Hero");
            Assert.IsTrue(slams[0].Result.Source.IsEnemy);
            Assert.AreEqual(10f, slams[0].Result.Dealt, 1e-4f);
            Assert.IsTrue(root.Hero != null);
        }
    }
}

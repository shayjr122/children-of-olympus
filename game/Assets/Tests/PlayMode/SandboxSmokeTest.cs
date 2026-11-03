using System.Collections;
using GMR.Core.Combat;
using GMR.Core.Heroes;
using GMR.Runtime.Encounter;
using GMR.Runtime.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GMR.Tests
{
    /// <summary>
    /// M4a smoke test: loads Sandbox with a virtual gamepad, checks Player 0 joins through PlayerInputManager, drives a
    /// real DASH through the input pipeline (gamepad South -> PlayerInputReader -> InputBuffer -> DashController),
    /// checks a Perfect Dodge against a pending hit and that pause goes through TimeScaleService.
    /// </summary>
    public sealed class SandboxSmokeTest : InputTestFixture
    {
        Gamepad _gamepad;

        public override void Setup()
        {
            base.Setup();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            InputSystem.AddDevice<Keyboard>();
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

            // Let it run a couple of seconds (dummies attack) to catch exceptions / error logs.
            yield return new WaitForSecondsRealtime(2f);
            Assert.IsTrue(root.Hero != null);
        }
    }
}

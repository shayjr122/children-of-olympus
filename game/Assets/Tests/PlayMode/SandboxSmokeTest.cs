using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GMR.Core.Abilities;
using GMR.Core.Combat;
using GMR.Core.Heroes;
using GMR.Core.Players;
using GMR.Core.Status;
using GMR.DevTools;
using GMR.Runtime.Encounter;
using GMR.Runtime.Feedback;
using GMR.Runtime.Heroes;
using GMR.Runtime.Status;
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
    /// damage with post-hit invulnerability; SFX hooks fire. M5a: Combo on dummies + HUD panel, timeout and damage
    /// reset, Ultimate not-ready cue, Fill RAGE cheat, Wrath of Zeus on all dummies with RAGE consumed and i-frames.
    /// M5b: StatusTicker ticks each container once per frame; a dry Thunderbolt chains through the chain lane (Combo +4,
    /// lightning lines, Shock tint); "Apply Wet to all" (menu button and D-pad up) tints every dummy; a Thunderbolt on the
    /// Wet lane is conducted (x2, CONDUCTED cue, blue-white arcs) and reaches one more dummy.
    /// Time: captureFramerate fixes 1/60 s per frame and FrameClock makes hitstop/slow-mo count frames too, so the whole
    /// run is frame-deterministic; no step may wait on the wall clock (that was the M5a flakiness).
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

        static string Diag(GMR.Runtime.Players.PlayerRoot root)
        {
            var h = root.Hero;
            var cur = h.Abilities.Current;
            return $"[t={Time.time:0.000} ut={Time.unscaledTime:0.000} f={Time.frameCount} ts={Time.timeScale} " +
                   $"current={(cur != null ? cur.Slot + "/" + cur.Phase : "none")} dash={h.Dash.Phase} " +
                   $"rage={root.CombatState.Rage.Value01:0.00} scheme={root.GetComponent<PlayerInput>().currentControlScheme}]";
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
            // The Perfect Dodge slow-mo runs on the Encounter's unscaled clock, which under captureFramerate advances
            // 1/60 s per frame (FrameClock): it ends after a fixed number of frames on any machine. Never wait on the wall clock.
            for (int i = 0; i < 120 && context.TimeScale.ActiveCount > 0; i++) yield return null;
            Assert.AreEqual(0, context.TimeScale.ActiveCount, "Perfect Dodge slow-mo should have ended");
            Assert.AreEqual(1f, Time.timeScale);
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
            // The three training dummies in the middle (the chain lane along the bottom is for the M5b steps).
            var dummies = context.Enemies.All.OfType<IDamageable>()
                .Where(d => ((Component)d).name.StartsWith("TrainingDummy_")).ToList();
            Assert.AreEqual(3, dummies.Count, "three training dummies register as damageable enemies");
            Assert.AreEqual(8, context.Enemies.All.OfType<IDamageable>().Count(), "plus the five chain-lane dummies");
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
            Assert.AreEqual(1, used.Count(u => u.Slot == AbilitySlot.Special), "Y should use Thunderbolt once. " + Diag(root));
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
            Assert.AreNotEqual(AbilityPhase.Ready, basic.Phase, Diag(root));
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

            yield return ComboRageAndUltimate(context, root, front, cues);
            yield return StatusesAndChainLightning(context, root, cues);
        }

        /// <summary>
        /// M5a: Combo builds on a dummy through real Club Strikes and shows on the Player's HUD panel; it times out and
        /// resets on damage; session toggles reach Combo/RAGE; Ultimate (RT) below full RAGE only plays the not-ready cue;
        /// the pause-menu "Fill RAGE" cheat fills RAGE; a full-RAGE press during an ability is queued and then fires Wrath
        /// of Zeus on every dummy, consuming RAGE and granting startup i-frames; the new SFX hooks fire.
        /// </summary>
        IEnumerator ComboRageAndUltimate(GMR.Core.Encounter.EncounterContext context, GMR.Runtime.Players.PlayerRoot root,
            IDamageable front, List<SfxCue> cues)
        {
            var hero = root.Hero;
            var state = root.CombatState;
            Assert.IsNotNull(state, "PlayerRoot hosts the PlayerCombatState");

            // Per-Player HUD: exactly one panel, bound to Player 0.
            var hud = Object.FindAnyObjectByType<CombatHud>();
            Assert.IsNotNull(hud, "CombatHud missing from Sandbox");
            Assert.AreEqual(1, hud.Panels.Count, "one HUD panel per joined Player");
            var panel = hud.PanelFor(root);
            Assert.IsNotNull(panel);

            // Away from the dummy attackers; let any earlier Combo time out.
            Teleport(hero, new Vector2(-6f, 0f));
            yield return new WaitForSeconds(2.3f);
            Assert.AreEqual(0, state.Combo.Count, "Combo times out after its window");

            var resets = new List<GMR.Core.Combo.ComboReset>();
            context.Events.OnComboReset += resets.Add;
            float rageBefore = state.Rage.Value01;

            // Three Club Strikes in a row on the front dummy: Combo x3, RAGE +2% each.
            for (int i = 0; i < 3; i++)
            {
                Teleport(hero, front.Position + new Vector2(-1f, 0f));
                root.Aim.Update(Vector2.right, false, false);
                yield return Frames(2);
                yield return Tap(_gamepad.buttonWest);
                yield return new WaitForSeconds(0.45f);
            }
            Assert.AreEqual(3, state.Combo.Count, "each Club Strike hit adds 1 to the Combo");
            Assert.Greater(state.Rage.Value01, rageBefore, "hits fill RAGE");
            yield return Frames(1);
            Assert.AreEqual("x3", panel.ComboText, "the HUD shows the Combo");

            // No hit for longer than the 2.0 s window: PlayerRoot ticks the Combo and it times out.
            Teleport(hero, new Vector2(-6f, 0f));
            yield return new WaitForSeconds(2.1f);
            Assert.AreEqual(0, state.Combo.Count, "the Combo times out in game");
            Assert.IsTrue(resets.Any(r => r.Reason == GMR.Core.Combo.ComboResetReason.Timeout && r.FinalCount == 3));

            // One more hit, then taking damage resets the Combo immediately.
            Teleport(hero, front.Position + new Vector2(-1f, 0f));
            root.Aim.Update(Vector2.right, false, false);
            yield return Frames(2);
            yield return Tap(_gamepad.buttonWest);
            yield return new WaitForSeconds(0.45f);
            Assert.AreEqual(1, state.Combo.Count);
            var enemySource = SourceId.ForEnemy(new GMR.Core.Enemies.EnemyId(98));
            Teleport(hero, new Vector2(-6f, 0f));
            context.Damage.Apply(root.Damageable, new DamageRequest(enemySource, 1f, DamageTags.Physical, Vector2.zero,
                default, context.Damage.NextHitId()), Time.time);
            Assert.AreEqual(0, state.Combo.Count);
            Assert.IsTrue(resets.Any(r => r.Reason == GMR.Core.Combo.ComboResetReason.Damage && r.FinalCount == 1));
            Assert.IsTrue(cues.Contains(SfxCue.ComboLost), "Combo lost SFX hook on a damage reset");

            // Pause-menu session toggles reach the Player's Combo and RAGE through PlayerRoot.
            context.Toggles.ShrinkingComboWindow = true;
            context.Toggles.RageDecay = false;
            yield return Frames(1);
            Assert.AreEqual(GMR.Core.Combo.ComboMode.Shrinking, state.Combo.Mode);
            Assert.IsFalse(state.Rage.DecayOn);
            context.Toggles.ShrinkingComboWindow = false;
            yield return Frames(1);
            Assert.AreEqual(GMR.Core.Combo.ComboMode.Fixed, state.Combo.Mode);
            context.Toggles.ClearOverrides();
            yield return Frames(1);
            Assert.AreEqual(state.Combo.Config.Mode, state.Combo.Mode);
            Assert.IsTrue(state.Rage.DecayOn, "config default: RAGE decays");

            // Ultimate below full RAGE: nothing fires, "not ready" cue.
            var used = new List<UltimateUsed>();
            var notReady = new List<UltimateNotReady>();
            context.Events.OnUltimateUsed += used.Add;
            context.Events.OnUltimateNotReady += notReady.Add;
            Assert.IsFalse(state.Rage.IsFull);
            yield return Tap(_gamepad.rightTrigger);
            yield return Frames(2);
            Assert.IsEmpty(used, "no Ultimate below 100% RAGE");
            Assert.AreEqual(1, notReady.Count, "not-ready cue");
            Assert.IsTrue(cues.Contains(SfxCue.UltimateNotReady));

            // Fill RAGE through the pause-menu cheat (DevTools button), then resume.
            var pause = Object.FindAnyObjectByType<PauseController>();
            yield return Tap(_gamepad.startButton);
            Assert.IsTrue(pause.IsOpen);
            var fillButton = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include)
                .FirstOrDefault(b => b.name.StartsWith("Fill RAGE"));
            Assert.IsNotNull(fillButton, "Fill RAGE cheat button missing from the pause menu");
            fillButton.onClick.Invoke();
            Assert.IsTrue(state.Rage.IsFull, "Fill RAGE fills the meter");
            Assert.IsTrue(cues.Contains(SfxCue.RageFull));
            yield return Tap(_gamepad.startButton);
            Assert.IsFalse(pause.IsOpen);
            yield return Frames(1);
            StringAssert.Contains("READY", panel.RageText, "HUD shows the full RAGE state");

            // Ultimate at full RAGE pressed during a Club Strike: queued, then fires once the strike ends. Wrath of
            // Zeus hits every dummy, RAGE consumed, startup i-frames.
            var hits = new List<HitLanded>();
            context.Events.OnHitLanded += hits.Add;
            var abilities = hero.GetComponent<GMR.Runtime.Abilities.AbilityController>();
            Teleport(hero, front.Position + new Vector2(-1f, 0f));
            root.Aim.Update(Vector2.right, false, false);
            yield return Frames(1);
            yield return Tap(_gamepad.buttonWest);
            Assert.IsNotNull(hero.Abilities.Current, "Club Strike running");
            Press(_gamepad.rightTrigger);
            yield return null;
            Release(_gamepad.rightTrigger);
            Assert.IsEmpty(used, "the Ultimate waits for the running ability");
            Assert.IsTrue(abilities.UltimateQueued, "the full-RAGE press is queued, not dropped");
            float until = Time.time + 1.5f; // scaled time: hitstop slows the Club Strike in real frames
            while (used.Count == 0 && Time.time < until) yield return null;
            Assert.AreEqual(1, used.Count, "the queued press fires the Ultimate once the Hero is free. " + Diag(root));
            Assert.AreEqual("heracles_wrath_of_zeus", used[0].Ability.Id);
            Assert.AreEqual(0f, state.Rage.Value01, "RAGE consumed to 0");
            Assert.IsTrue(hero.Invulnerability.IsInvulnerableFrom(InvulnerabilitySource.UltimateStartup, used[0].At),
                "i-frames cover the Ultimate startup");
            Assert.IsTrue(cues.Contains(SfxCue.UltimateUsed));
            yield return new WaitForSeconds(0.8f);
            var struck = hits.Where(h => (h.Result.Tags & DamageTags.Ultimate) != 0).Select(h => h.TargetId).Distinct().Count();
            Assert.AreEqual(context.Enemies.All.OfType<IDamageable>().Count(), struck, "Wrath of Zeus strikes every training dummy");
            Assert.IsTrue(hits.Any(h => (h.Result.Tags & (DamageTags.Ultimate | DamageTags.ChainJump)) ==
                                        (DamageTags.Ultimate | DamageTags.ChainJump)), "Wrath of Zeus applies Shock: its chains keep the Ultimate tag");
            Assert.AreEqual(1, used.Count);
        }

        /// <summary>M5b (Stories 5.1-5.3): status ticking, Shock chain lightning, Wet and Conducted Shock in the Sandbox.</summary>
        IEnumerator StatusesAndChainLightning(GMR.Core.Encounter.EncounterContext context, GMR.Runtime.Players.PlayerRoot root,
            List<SfxCue> cues)
        {
            var hero = root.Hero;
            var state = root.CombatState;

            // StatusTicker: every registered container exactly once per frame.
            var ticker = Object.FindAnyObjectByType<StatusTicker>();
            Assert.IsNotNull(ticker, "StatusTicker missing from Sandbox");
            Assert.That(context.Statuses.Count, Is.GreaterThanOrEqualTo(9), "8 dummies + the Hero carry status containers");
            var before = context.Statuses.All.Select(c => c.TickCount).ToArray();
            int framesBefore = ticker.FramesTicked;
            yield return Frames(5);
            Assert.AreEqual(5, ticker.FramesTicked - framesBefore);
            for (int i = 0; i < before.Length; i++)
                Assert.AreEqual(before[i] + 5, context.Statuses.All[i].TickCount, "ticked exactly once per frame");

            // Lane dummies (Wrath of Zeus broke them all: wait for the respawn) and a quiet Combo.
            var lane = Enumerable.Range(1, 5).Select(i => GameObject.Find("ChainDummy_" + i).GetComponent<TrainingDummy>()).ToList();
            Teleport(hero, new Vector2(-8.5f, -4.8f));
            yield return new WaitForSeconds(2.2f);
            Assert.IsTrue(lane.All(d => d.IsAlive), "lane dummies respawned");
            Assert.AreEqual(0, state.Combo.Count);
            var chainFx = Object.FindAnyObjectByType<ChainLightningFeedback>();
            Assert.IsNotNull(chainFx, "ChainLightningFeedback missing");
            var hits = new List<HitLanded>();
            var conducted = new List<ConductedShock>();
            context.Events.OnHitLanded += hits.Add;
            context.Events.OnConductedShock += conducted.Add;

            // Dry Thunderbolt on the left lane dummy: Shock chains through four (N 3), each once, with falloff.
            int lines = chainFx.LinesDrawn;
            yield return ThunderboltAlongLane(root, lane[0]);
            var dryHit = hits.Where(h => lane.Contains(h.Target as TrainingDummy)).ToList();
            CollectionAssert.AreEquivalent(lane.Take(4).ToList(), dryHit.Select(h => h.Target).Distinct().ToList(),
                "dry chain: first four lane dummies");
            Assert.AreEqual(4, dryHit.Count, "each hit once");
            Assert.AreEqual(3, dryHit.Count(h => (h.Result.Tags & DamageTags.ChainJump) != 0));
            Assert.IsTrue(dryHit.All(h => h.Source == SourceId.ForPlayer(root.Id)), "chain jumps carry the Player's SourceId");
            Assert.AreEqual(3, chainFx.LinesDrawn - lines, "a lightning line per jump");
            Assert.AreEqual(4, state.Combo.Count, "a chain hitting 4 enemies raises Combo by 4");
            Assert.IsEmpty(conducted);
            float dryFirst = dryHit[0].Result.Dealt;

            // Apply Wet to all: pause-menu button, then the debug key (D-pad up) refreshes it.
            var tools = Object.FindAnyObjectByType<DebugStatusTools>();
            Assert.IsNotNull(tools, "DebugStatusTools missing");
            var pause = Object.FindAnyObjectByType<PauseController>();
            yield return Tap(_gamepad.startButton);
            Assert.IsTrue(pause.IsOpen);
            var wetButton = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include)
                .FirstOrDefault(b => b.name.StartsWith("Apply Wet"));
            Assert.IsNotNull(wetButton, "Apply Wet to all button missing from the pause menu");
            wetButton.onClick.Invoke();
            int damageables = context.Enemies.All.OfType<IDamageable>().Count();
            Assert.AreEqual(damageables, tools.LastAppliedCount);
            yield return Tap(_gamepad.startButton);
            Assert.IsFalse(pause.IsOpen);
            var wet = lane[0].Statuses.Instances.Select(i => i.Definition).FirstOrDefault(d => d.Id == "wet");
            Assert.IsNotNull(wet, "lane dummy is Wet");
            foreach (var d in context.Enemies.All.OfType<IDamageable>()) d.Statuses.Remove(wet, Time.time);
            Assert.IsFalse(lane[0].Statuses.Has(wet));
            yield return Tap(_gamepad.dpad.up);
            Assert.AreEqual(damageables, tools.LastAppliedCount, "D-pad up applies Wet to all too");
            Assert.IsTrue(context.Enemies.All.OfType<IDamageable>().All(d => d.Statuses.Has(wet)));
            yield return Frames(1);
            var visuals = lane[4].GetComponent<StatusVisuals>();
            Assert.Greater(visuals.CurrentTint.b, visuals.CurrentTint.r, "Wet tints the dummy blue");
            Assert.GreaterOrEqual(visuals.VisibleIcons, 1, "status icon above the dummy");

            // Wet Thunderbolt after the cooldown and a Combo timeout: conducted, x2, reaches the fifth dummy.
            yield return new WaitForSeconds(2.2f);
            Assert.AreEqual(0, state.Combo.Count);
            hits.Clear();
            lines = chainFx.LinesDrawn;
            int conductedLines = chainFx.ConductedLinesDrawn;
            yield return ThunderboltAlongLane(root, lane[0]);
            var wetHit = hits.Where(h => lane.Contains(h.Target as TrainingDummy)).ToList();
            CollectionAssert.AreEquivalent(lane, wetHit.Select(h => h.Target).Distinct().ToList(), "Wet chain reaches all five");
            Assert.AreEqual(5, wetHit.Count);
            Assert.AreEqual(dryFirst * 2f, wetHit[0].Result.Dealt, 1e-3f, "Shock on a Wet dummy deals 2x its dry damage");
            Assert.That(conducted.Count, Is.GreaterThanOrEqualTo(5), "ConductedShock for each conducted application");
            Assert.AreEqual(4, chainFx.ConductedLinesDrawn - conductedLines, "blue-white arcs");
            Assert.IsTrue(cues.Contains(SfxCue.Conducted), "CONDUCTED! popup + cue");
            Assert.AreEqual(5, state.Combo.Count);
            Assert.IsTrue(lane[0].Statuses.Has(wet), "Wet is not consumed by Shock (default)");
        }

        IEnumerator ThunderboltAlongLane(GMR.Runtime.Players.PlayerRoot root, TrainingDummy first)
        {
            var hero = root.Hero;
            Teleport(hero, first.Position + new Vector2(-2.5f, 0f));
            root.Aim.Update(Vector2.right, false, false);
            yield return Frames(2);
            int specials = 0;
            System.Action<AbilityUsed> count = u => { if (u.Slot == AbilitySlot.Special) specials++; };
            root.Context.Events.OnAbilityUsed += count;
            yield return Tap(_gamepad.buttonNorth);
            yield return new WaitForSeconds(0.6f);
            root.Context.Events.OnAbilityUsed -= count;
            Assert.AreEqual(1, specials, "Y fires Thunderbolt. " + Diag(root));
        }
    }
}

---
status: final
stepsCompleted: [1, 2, 3, 4]
mode: headless (orchestrator, non-interactive); assumptions indexed at the end
created: 2026-11-03
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-greek-mythology-roguelike-2026-11-03/prd.md
  - _bmad-output/planning-artifacts/prds/prd-greek-mythology-roguelike-2026-11-03/addendum.md
  - _bmad-output/planning-artifacts/architecture/architecture-greek-mythology-roguelike-2026-11-03/ARCHITECTURE-SPINE.md
  - prompts/dev-kickoff-prompt.md
approval: pending human approval at the kickoff milestone-2 gate (milestone splits included)
---

# Greek Mythology Roguelike - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Greek Mythology Roguelike (combat prototype scope, kickoff milestones 3-7), decomposing the requirements from the PRD and the Architecture Spine into implementable stories. No UX design document exists for the prototype; HUD/menu requirements come from the PRD (FR-28, FR-29).

**Capacity rule (NFR-8, kickoff).** The team has about 3 hours per week in total. AI agents write the code; **human time** is reviewing, opening Unity, playtesting and tuning. Every milestone below fits one 1-3 h human session (target <= 150 min, hard cap 180 min) and ends with something runnable or hearable in Unity. Each story is sized for one AI-agent implementation session. Story estimates are human minutes only; each milestone adds 10-25 min of gate overhead (reading the gate report, approving the commit).

**Story metadata.** Every story lists: Milestone, Est. human minutes, Refs (FR / NFR / AD), and Tests (EditMode `GMR.Tests.EditMode`, PlayMode `GMR.Tests.PlayMode`, or a manual Play Mode check). `BannedApiTests` (AD-3) must stay green at the end of every story; this is an implicit acceptance criterion of all stories from 1.2 onward.

## Requirements Inventory

### Functional Requirements

- FR-1: Free analog movement (dead-zone, acceleration, per-Hero max speed), movement continues during Abilities unless `rootsCaster`, Hero stays inside Arena walls.
- FR-2: Aim Direction = last non-zero movement direction; persistent visible aim indicator.
- FR-3: Aim Assist (toggleable): snap to nearest living enemy in a cone (angle/range tunable) when firing.
- FR-4: Direction Lock (toggleable): holding the button freezes Aim Direction while movement stays free.
- FR-5: Right-stick / mouse aim fallback (Could; build only if SM-6 fails).
- FR-6: DASH with no cooldown; i-frames only at start; Dash Recovery; passes through enemies, not walls; cancels own Ability recovery.
- FR-7: Perfect Dodge: DASH started inside the window before a hit lands; RAGE gain, flash + time-slow; at most one per attack per Player; counted in telemetry.
- FR-8: Input buffering for DASH and Ability inputs; buffer 0 disables it.
- FR-9: Data-driven Ability Slots (Basic / Special / Signature), Ability assets define type, damage, shape, timings, cooldown, status, roots; Basic cooldown 0; Spin/Shot/Thrust hit shapes.
- FR-10: Heracles kit: Club Strike (Thrust), Thunderbolt (Shot, Shock), Storm Whirl (Spin, Shock).
- FR-11: Theseus kit: Trident Thrust (Thrust, strong knockback), Riptide Bolt (Shot, piercing, Wet), Maelstrom (Spin, push + Wet).
- FR-12: One data-driven Ability Chain per Hero (Thunderhead, Undertow) with window and armed cue.
- FR-13: Per-Player Combo counter: +1 per hit (incl. status/chain hits), resets on window timeout or on taking damage; HUD with window bar and reset cue.
- FR-14: Shrinking Combo Window toggle (curve + minimum).
- FR-15: RAGE fill from hits, Combo milestones, Perfect Dodges; status ticks at a lower rate; clamped 0-100%.
- FR-16: RAGE decay after grace period, also when full; telemetry for RAGE-full and wasted RAGE.
- FR-17: Ultimate when RAGE full, consumes RAGE (Wrath of Zeus, Poseidon's Flood); "not ready" cue; i-frames at startup.
- FR-18: Generic Status System: Status Definition assets, behaviours, interaction rules, tint + icon, source-Player attribution.
- FR-19: Shock: damage plus chain to up to N enemies within R with falloff, each enemy once per chain, seeded tie-breaks, visible lightning line.
- FR-20: Wet: blue tint, duration, refresh on re-apply, no damage.
- FR-21: Wet amplifies Shock (Conducted Shock): x2 damage, +1 jump, +radius, distinct visual, Wet not consumed (tunable), telemetry.
- FR-22: Hero Select screen before each Encounter (controller + keyboard), quick restart.
- FR-23: Hero Swap in place: statuses/arena/waves kept, HP fraction/Combo/RAGE carry, incoming cooldowns ready, swap cooldown, toggleable, ignored during DASH/Ultimate.
- FR-24: Three enemy Archetypes (Chaser, Shooter, Tank) with per-Archetype telegraphs, nearest-Player targeting, hit flash, knockback by resistance, death effect.
- FR-25: One Arena with walls/pillars; seeded, budgeted, weighted Waves (default 3) at seeded spawn markers; spawn telegraph; results panel; death ends Encounter with Restart.
- FR-26: Single Run Seed with independent per-subsystem streams; shown on HUD/results; seed entry; replay same seed; no unseeded Random.
- FR-27: Hit feedback (flash, hitstop, knockback, damage numbers, shake), Player red flash + post-hit invulnerability, SFX hooks; all tunable/toggleable.
- FR-28: Per-Player Combat HUD: HP, Combo + window bar, RAGE (full state), Special/Signature cooldowns, active Hero, swap cooldown, Run Seed; readable at 640x400.
- FR-29: Controller-navigable pause/settings/debug menu with all listed toggles, seed entry, restart, god mode, enemy freeze.
- FR-30: Session Telemetry: local per-session file with the listed metrics; summary on results panel; no network.
- FR-31: Combat Intensity 0-1 from enemy count, RAGE, HP; smoothed; max across Players.
- FR-32: Four independent looping music tracks (calm, combat, advanced-combat, boss) crossfaded by game state through an Audio Mixer (no shared BPM/key); ACE-Step placeholders with manifest.

### NonFunctional Requirements

- NFR-1: All randomness from the Run Seed via per-subsystem streams.
- NFR-2: N-player ready: no static player; per-Player Combo/RAGE/aim/input/HUD; debug "Spawn Player 2" works with zero combat-code changes.
- NFR-3: Unity Input System action maps; full controller (Xbox/PS/Steam Deck) and basic keyboard; no hard-coded keys; rebindable in data.
- NFR-4: Every gameplay number in ScriptableObject Tuning Assets; changes apply at least on next Play Mode entry (live where feasible).
- NFR-5: 60 FPS on Steam Deck at 1280x800 with 15 enemies, 30 projectiles, full Shock chains; input-to-action within one frame.
- NFR-6: URP 2D, Pixel Perfect Camera 640x400, 32x32 characters, placeholder shapes only.
- NFR-7: Core rules (Combo, RAGE, Status/Wet-Shock, seeded streams) engine-light and covered by EditMode tests.
- NFR-8: Every story/milestone fits a 1-3 h work session and ends in something runnable; ~3 h/week team capacity.
- NFR-9: Unity 6000.6.7f1, Windows PC build, Steam Deck target from day one.
- NFR-10: Prototype code carried into the vertical slice; throwaway limited to debug tools and Hero Swap UI.

### Additional Requirements

From the Architecture Spine (no starter template; project is created fresh with the Unity CLI / Unity plugin in `game/`):

- AD-1: Six asmdefs (`GMR.Core`, `GMR.Runtime`, `GMR.DevTools`, `GMR.Editor`, `GMR.Tests.EditMode`, `GMR.Tests.PlayMode`) with one-way dependencies; Core free of MonoBehaviour/Time/Physics/Random; DevTools behind `UNITY_EDITOR || DEVELOPMENT_BUILD`.
- AD-2: `RunRng` (SplitMix64 + FNV-1a + PCG32, hand-written) with named streams in `RngStreams`; per-entity `Derive(subName, index)`.
- AD-3: `BannedApiTests` EditMode text scan for banned random/time/GUID APIs and mutable statics.
- AD-4: `PlayerInputManager` (manual join), `PlayerId`, `PlayerRegistry`, `SourceId` on every damage/status/event; `ITargetSelector`; "Spawn Player 2" acceptance test.
- AD-5: Domain reload off; no mutable statics; one `EncounterContext` passed via `Init`; no runtime `FindObjectOfType`; SOs read-only at runtime.
- AD-6: `PlayerRoot` (person state: input, Combo, RAGE, aim, HUD) vs. replaceable `HeroRuntime`; `HeroSwapConfig` carry flags.
- AD-7: `GameControls.inputactions` maps Gameplay/UI/Debug; only `PlayerInputReader` touches `InputAction`; `InputFrame`, `InputBuffer`, `AimResolver`, `AimConfig.mode`.
- AD-8: `AbilityDefinition` / `HeroDefinition` / `AbilityChainDefinition`; one `AbilityRunner` per slot; hit resolver per `HitShape`; variants as SO subclasses (no `[SerializeReference]`); Ultimate is an `AbilityDefinition` gated by RAGE.
- AD-9: Single `DamageSystem.Apply` pipeline with `DamageTags` and `IDamageModifier` fold.
- AD-10: `StatusDefinition` + `StatusBehaviour` subclasses + `StatusInteractionRule` in a `StatusRuleSet`; `ChainLightningBehaviour` uses the `Status` stream.
- AD-11: `CombatEvents` typed past-tense events; `ComboCounter`/`RageMeter` per Player react only to own `SourceId`.
- AD-12: `TimeScaleService` sole writer of `Time.timeScale`; `PendingHitRegistry` for Perfect Dodge.
- AD-13: One `EnemyBrain` FSM; `ArchetypeDefinition` + `IAttackPattern` (Charge/RangedShot/Slam); `WaveBuilder` (Core) + `WaveSpawner`.
- AD-14: `ICombatIntensitySource` defined in Core; `CombatIntensityTracker` ships with telemetry (M5), consumed by `MusicStateMixer` (M6).
- AD-15: `TelemetryRecorder` passive JSONL writer to `persistentDataPath/telemetry/`.
- AD-16: Single owner per combat mutation (HP, invulnerability, knockback, ticking, events); FIFO re-entrancy with 256 cap.
- Infra: `.gitignore` (Unity template rooted at `game/`), `.gitattributes` LFS scoped to `game/Assets/**`, `tools/setup-git.ps1` for `unityyamlmerge`, Force Text serialization, pinned packages (URP, Input System 1.19.x, Test Framework).
- Tests: EditMode tests for every Core rule class; PlayMode `ArenaSmokeTest` and `SecondPlayerSmokeTest`; run via Unity CLI `-batchmode -runTests` (no `-quit`, no `-nographics` for PlayMode).
- Music: ACE-Step 1.5 placeholders, `AI_PLACEHOLDER_` prefix, `Assets/Audio/Placeholder/manifest.md`, generated in a session before M6.

### UX Design Requirements

No UX design contract exists for the prototype (placeholder shapes only, NFR-6). HUD and menu readability requirements are taken from FR-28/FR-29 and covered by Stories 4.3, 2.5, 7.3, 8.4.

### FR Coverage Map

| FR | Epic | Stories |
| --- | --- | --- |
| FR-1 | Epic 2 | 2.1 |
| FR-2 | Epic 2 | 2.2 |
| FR-3 | Epic 2 | 2.2 |
| FR-4 | Epic 2 | 2.2 |
| FR-5 | Epic 2 (hook) / Epic 9 (conditional) | 2.2 (`AimMode` enum hook only); 9.6 conditional - **deferred unless SM-6 fails** |
| FR-6 | Epic 2, Epic 3 | 2.3 (DASH), 3.2 (cancel Ability recovery) |
| FR-7 | Epic 2, Epic 4, Epic 9 | 2.4 (detection, feedback), 4.2 (RAGE gain), 9.1 (telemetry) |
| FR-8 | Epic 2, Epic 3 | 2.3 (buffer + DASH), 3.2 (Ability inputs) |
| FR-9 | Epic 3 | 3.1, 3.2, 3.3 |
| FR-10 | Epic 3, Epic 5 | 3.3 (kit), 5.2 (Shock payload) |
| FR-11 | Epic 8 | 8.1 |
| FR-12 | Epic 8 | 8.3 |
| FR-13 | Epic 4, Epic 5 | 4.1, 4.3, 5.2 (chain hits count) |
| FR-14 | Epic 4 | 4.1 |
| FR-15 | Epic 4 | 4.2 |
| FR-16 | Epic 4, Epic 9 | 4.2, 9.1 (telemetry) |
| FR-17 | Epic 4, Epic 5, Epic 8 | 4.4 (gate + Wrath of Zeus), 5.2 (Shock at max chain), 8.2 (Poseidon's Flood) |
| FR-18 | Epic 5 | 5.1 |
| FR-19 | Epic 5 | 5.2 |
| FR-20 | Epic 5 | 5.3 |
| FR-21 | Epic 5, Epic 9 | 5.3, 9.1 (telemetry) |
| FR-22 | Epic 8 | 8.4 |
| FR-23 | Epic 8, Epic 9 | 8.5, 9.1 (telemetry) |
| FR-24 | Epic 6 | 6.1, 6.2, 6.3 |
| FR-25 | Epic 7 | 7.1, 7.2, 7.3 |
| FR-26 | Epic 1, Epic 5, Epic 6, Epic 7 | 1.2 (RunRng + ban), 5.2 (tie-break), 6.1 (AI variation), 7.1/7.2/7.3 (waves, spawns, seed UI, replay) |
| FR-27 | Epic 3 | 3.4 |
| FR-28 | Epic 3, Epic 4, Epic 7, Epic 8 | 3.2 (cooldowns), 4.3 (HUD), 7.3 (seed), 8.5 (active Hero, swap cooldown) |
| FR-29 | Epic 2, 4, 6, 7, 8 | 2.5 (menu + aim toggles), 4.1 (shrink toggle), 3.4 (damage numbers), 6.1 (god mode, enemy freeze), 7.3 (seed entry, restart), 8.5 (swap toggle) |
| FR-30 | Epic 9 | 9.1, 9.2 |
| FR-31 | Epic 9, Epic 10 | 9.2 (tracker), 10.3 (consumed) |
| FR-32 | Epic 10 | 10.1, 10.2, 10.3 |

| NFR | Stories |
| --- | --- |
| NFR-1 | 1.2, 5.2, 6.1, 7.1, 7.2 |
| NFR-2 | 2.1, 4.1, 4.2, 4.3, 7.4 |
| NFR-3 | 1.3, 2.5, 8.4 |
| NFR-4 | every story that adds a number (Config/Definition assets); verified in 2.1, 9.4 |
| NFR-5 | 9.3 |
| NFR-6 | 1.1 |
| NFR-7 | 1.2, 2.2, 2.3, 4.1, 4.2, 5.1, 5.3, 7.1 |
| NFR-8 | Milestone plan (all) |
| NFR-9 | 1.1, 9.3 |
| NFR-10 | all (AD-1 split); code-review at every gate |

## Epic List

### Epic 1: Playable Project Foundation
The team can open the Unity project on both machines, run tests, and move a square with a controller in a pixel-perfect scene.
**FRs covered:** FR-26 (seed foundation); NFR-1, NFR-3, NFR-6, NFR-7, NFR-9. **Milestone:** M3.

### Epic 2: Responsive Movement, Aim and DASH
A Player moves, aims (with Assist and Direction Lock), dashes through a dummy attacker and lands Perfect Dodges, all tunable in the Inspector.
**FRs covered:** FR-1, FR-2, FR-3, FR-4, FR-5 (hook), FR-6, FR-7, FR-8, FR-29 (partial). **Milestone:** M4a.

### Epic 3: Heracles Fights Training Dummies
A Player plays Heracles' three data-driven abilities against training dummies with full hit feedback.
**FRs covered:** FR-6 (cancel), FR-8, FR-9, FR-10, FR-27, FR-28 (cooldowns). **Milestone:** M4b.

### Epic 4: Combo, RAGE and Ultimate Loop
A Player builds a Combo, fills and loses RAGE, and fires Heracles' Ultimate, all shown on a per-Player HUD.
**FRs covered:** FR-7 (RAGE gain), FR-13, FR-14, FR-15, FR-16, FR-17 (Heracles), FR-28, FR-29 (partial). **Milestone:** M5a.

### Epic 5: Shock, Wet and Conducted Shock
Heracles' attacks chain lightning between dummies; Wet (applied via debug in this epic) visibly amplifies the chain. Built on a generic Status System.
**FRs covered:** FR-10 (Shock payload), FR-13 (chain hits), FR-17 (Shock on Ultimate), FR-18, FR-19, FR-20, FR-21, FR-26 (tie-breaks). **Milestone:** M5b.

### Epic 6: Enemies That Fight Back
Three Archetypes (Chaser, Shooter, Tank) attack with readable telegraphs that DASH and Perfect Dodge can beat.
**FRs covered:** FR-24, FR-26 (AI stream), FR-29 (god mode, enemy freeze). **Milestone:** M5c.

### Epic 7: Seeded Arena Encounter
A full 3-Wave Encounter in the Arena, reproducible from a Run Seed, with results, restart and an N-player proof.
**FRs covered:** FR-25, FR-26, FR-28 (seed), FR-29 (seed entry, restart); NFR-2. **Milestone:** M5d.

### Epic 8: Theseus, Ability Chains and Hero Swap
A Player picks Heracles or Theseus, uses each Hero's chain, and swaps Heroes mid-fight to pay off Wet with Shock.
**FRs covered:** FR-11, FR-12, FR-17 (Theseus), FR-22, FR-23, FR-28, FR-29 (swap toggle). **Milestone:** M5e.

### Epic 9: Playtest Telemetry and the "Is It Fun" Gate
Every session writes telemetry; the team and external testers play a Steam Deck/PC build; the gate verdict and tuning changes are recorded.
**FRs covered:** FR-5 (conditional), FR-7, FR-16, FR-21, FR-23 (telemetry parts), FR-30, FR-31 (tracker); NFR-5. **Milestones:** M5f, M5g.

### Epic 10: Dynamic Combat Music
The music crossfades between four independent tracks (calm, combat, advanced-combat, boss) as the game state changes.
**FRs covered:** FR-31 (consumer), FR-32. **Milestones:** M6-pre (music generation), M6.

### Epic 11: Prototype Wrap-up
The team knows actual vs. estimated time, what was learned about combat, and has an approved next plan.
**FRs covered:** none (process; kickoff milestone 7). **Milestone:** M7.

---

## Epic 1: Playable Project Foundation

The team can open the Unity project on both machines, run tests from the CLI, and move a square with a controller in a pixel-perfect scene. Establishes AD-1/2/3/5/7 so every later epic builds on it.

### Story 1.1: Create the URP 2D Unity project with pixel-perfect camera and Git setup

- **ID:** 1.1 | **Milestone:** M3 | **Est. human:** 45 min (LFS install on both machines, first open of the project)
- **Refs:** NFR-6, NFR-9, NFR-10; AD-1 (folders), Structural Seed (Git, LFS)
- **Tests:** Manual: project opens in Unity 6000.6.7f1 without errors; `git lfs ls-files` after adding a test PNG under `game/Assets/`.

As a team member,
I want a fresh Unity 6000.6.7f1 URP 2D project in `game/` with a Pixel Perfect Camera at 640x400 and correct Git/LFS config,
So that both machines open the same project and assets are versioned safely.

**Acceptance Criteria:**

**Given** a clean clone of the repo
**When** the project in `game/` is opened with Unity 6000.6.7f1
**Then** it opens with no console errors, uses URP with the 2D Renderer, and URP's built-in `PixelPerfectCamera` set to 640x400 reference, 32 PPU
**And** `Packages/manifest.json` pins URP, Input System (1.19.x) and Test Framework versions
**And** asset serialization is Force Text and meta files are visible

**Given** the repo root
**When** `.gitignore` and `.gitattributes` are inspected
**Then** `.gitignore` uses the Unity template rooted at `game/`, and LFS rules are scoped to `game/Assets/**` (root `images/*.png` untouched)
**And** `tools/setup-git.ps1` configures `merge.unityyamlmerge.driver` and is documented in a one-line README note in `game/`

**Given** the folder tree in the Structural Seed
**When** the agent finishes
**Then** `Scripts/{Core,Runtime,DevTools,Editor}`, `Tests/{EditMode,PlayMode}`, `Data/`, `Input/`, `Prefabs/`, `Scenes/`, `Art/Placeholder/`, `Audio/{Placeholder,Mixers}/`, `Settings/` exist
**And** Enter Play Mode Options has domain reload disabled (AD-5)

### Story 1.2: Seeded RNG core, assemblies and banned-API test

- **ID:** 1.2 | **Milestone:** M3 | **Est. human:** 25 min
- **Refs:** FR-26, NFR-1, NFR-7; AD-1, AD-2, AD-3, AD-5
- **Tests:** EditMode `RunRngTests`, `BannedApiTests`; run via Unity CLI `-batchmode -runTests -testPlatform EditMode`.

As a designer-developer,
I want one Run Seed that produces independent, reproducible random streams, and a test that bans unseeded randomness,
So that every later system is reproducible for the Daily Run from day one.

**Acceptance Criteria:**

**Given** the six asmdefs from AD-1
**When** the project compiles
**Then** `GMR.Core` references no `UnityEngine` APIs except math types and `ScriptableObject`, `GMR.DevTools` has define constraint `UNITY_EDITOR || DEVELOPMENT_BUILD`, and test asmdefs reference `Unity.InputSystem.TestFramework`

**Given** `RunRng(12345)`
**When** the `Waves` stream is drawn 100 times in two separate instances
**Then** both sequences are identical (`RunRngTests.SameSeed_SameSequence`)
**And** drawing 1,000 values from `Combat` before drawing `Waves` does not change the `Waves` sequence (`RunRngTests.StreamsIndependent`)
**And** `Derive("chaser", 3)` is stable regardless of how many other children were derived

**Given** a gameplay script containing `UnityEngine.Random.Range`, `new System.Random`, `Guid.NewGuid`, `DateTime.Now` or a mutable `static` field
**When** `BannedApiTests` runs
**Then** it fails naming the file and line; files under `Scripts/DevTools/**` and `RngStreams.cs` are allow-listed
**And** the clean project passes `BannedApiTests` (one green EditMode run recorded in the gate report)

### Story 1.3: Input actions and a controller-driven square in the Sandbox scene

- **ID:** 1.3 | **Milestone:** M3 | **Est. human:** 30 min (try gamepad + keyboard)
- **Refs:** NFR-3, NFR-5; AD-7, AD-5
- **Tests:** EditMode `InputFrameTests` (struct defaults); manual: square moves with gamepad stick and WASD.

As a team member,
I want a `GameControls` asset and a `PlayerInputReader` that drive a square in `Sandbox.unity`,
So that we can see the input pipeline works on controller and keyboard before building combat.

**Acceptance Criteria:**

**Given** `Input/GameControls.inputactions`
**When** it is inspected
**Then** it has maps `Gameplay` (Move, Aim, Dash, Basic, Special, Signature, Ultimate, DirectionLock, HeroSwap, Pause), `UI` and `Debug`, with Gamepad and Keyboard&Mouse control schemes and no hard-coded keys in scripts

**Given** `Sandbox.unity` in Play Mode
**When** the left stick or WASD is used
**Then** a placeholder square moves, driven only through `PlayerInputReader` -> per-Player `InputFrame` (no other script touches `InputAction`)
**And** the reader subscribes in `OnEnable` and unsubscribes in `OnDisable`, with `[DefaultExecutionOrder(-100)]`

---

## Epic 2: Responsive Movement, Aim and DASH

A Player moves, aims, dashes through a dummy attacker and lands Perfect Dodges, all tunable in the Inspector (UJ-4).

### Story 2.1: PlayerRoot/HeroRuntime split and tunable free movement

- **ID:** 2.1 | **Milestone:** M4a | **Est. human:** 25 min
- **Refs:** FR-1, NFR-2, NFR-4; AD-4, AD-5, AD-6
- **Tests:** EditMode `MoveMathTests` (dead-zone, acceleration curve); manual: movement feel, walls.

As a Player,
I want to move my Hero freely with analog speed inside a walled sandbox,
So that movement feels responsive and the structure supports multiple Players and Hero Swap later.

**Acceptance Criteria:**

**Given** `Sandbox.unity` with an `EncounterBootstrap` creating `EncounterContext` and a `PlayerInputManager` (manual join)
**When** Play Mode starts
**Then** Player 0 joins, a `PlayerRoot` registers in `PlayerRegistry` with `PlayerId 0`, and a child `HeroRuntime` is built from a `HeroDefinition` with a `MoveConfig`

**Given** stick deflection below `MoveConfig.deadZone` (default 0.2)
**When** the Hero updates
**Then** it does not move; full deflection reaches max speed (5.5 u/s default) within `accelerationTime` (0.06 s), verified by `MoveMathTests`
**And** the Hero collides with walls and cannot leave the bounds

**Given** the team edits `MoveConfig` max speed during Play Mode
**When** they keep playing
**Then** the new speed applies immediately (configs read live, not cached)

### Story 2.2: Movement aim, Aim Assist and Direction Lock

- **ID:** 2.2 | **Milestone:** M4a | **Est. human:** 30 min
- **Refs:** FR-2, FR-3, FR-4, FR-5 (hook only); AD-7
- **Tests:** EditMode `AimResolverTests` (last-direction memory, cone selection 15 deg in / 30 deg out, lock freeze, assist off).

As a Player,
I want my aim to follow movement, optionally snap to nearby enemies, and freeze while I hold Direction Lock,
So that I can hit what I aim at, including while retreating.

**Acceptance Criteria:**

**Given** the stick returns to neutral
**When** `AimResolver` runs
**Then** Aim Direction keeps the last non-zero movement direction and a placeholder arrow shows it at all times

**Given** Aim Assist on (cone half-angle 25 deg, range 8 u) and a target candidate 15 deg off-axis at 5 u
**When** an aim is resolved for firing
**Then** the direction snaps to that candidate; with Assist off it does not; with no candidate in the cone it is unchanged (`AimResolverTests`)

**Given** Direction Lock enabled and the button held
**When** the Hero walks in another direction
**Then** Aim Direction stays at the value when the button was pressed; release resumes movement aim on the next movement input; with the feature disabled the button does nothing

**Given** `AimConfig`
**When** inspected
**Then** it contains an `AimMode` enum with only `MovementAim` implemented (FR-5 hook; RightStick is not built here)

### Story 2.3: Input buffer and DASH with i-frames and Dash Recovery

- **ID:** 2.3 | **Milestone:** M4a | **Est. human:** 30 min (feel check)
- **Refs:** FR-6, FR-8; AD-7, AD-16 (InvulnerabilityTracker)
- **Tests:** EditMode `InputBufferTests` (window, 0 disables, consume once), `InvulnerabilityTrackerTests` (window registration/expiry), `DashStateTests` (recovery blocks new DASH).

As a Player,
I want a snappy DASH with no cooldown but a short recovery, with my presses buffered,
So that I can evade reliably without spamming invulnerability.

**Acceptance Criteria:**

**Given** `DashConfig` (3 u, 0.15 s, i-frames 0.10 s, recovery 0.12 s)
**When** the Player presses Dash with movement input
**Then** the Hero dashes along the movement direction (Aim Direction if no input), stopping at walls, and `DashStarted` is raised by `DashController`
**And** an i-frame window is registered in `InvulnerabilityTracker` only for the first 0.10 s

**Given** the Hero is in Dash Recovery
**When** Dash is pressed
**Then** it is ignored if buffer = 0, or executes as soon as recovery ends if pressed within the 0.12 s buffer window (`InputBufferTests`)

**Given** `InputConfig.bufferWindow = 0`
**When** inputs are pressed early
**Then** nothing is buffered

### Story 2.4: Time scale service, pending hits and Perfect Dodge against a dummy attacker

- **ID:** 2.4 | **Milestone:** M4a | **Est. human:** 30 min
- **Refs:** FR-7, FR-6; AD-12, AD-16
- **Tests:** EditMode `TimeScaleServiceTests` (lowest scale wins, expiry, pause 0), `PendingHitRegistryTests` (window inside/outside, one per attackId+PlayerId); manual: dash through the dummy.

As a Player,
I want a dummy attacker with a visible wind-up that I can perfectly dodge,
So that timing-based dodges feel rewarding before real enemies exist.

**Acceptance Criteria:**

**Given** a `DummyAttacker` (DevTools) that telegraphs and swings every few seconds and registers a `PendingHit` with `landTime`
**When** the Player starts a DASH within `perfectWindow` (0.08 s) before `landTime` while overlapping the area
**Then** `PerfectDodged` is raised once for that `(attackId, PlayerId)`, a flash plays and a time-slow (0.15 s at 30%) is requested via `TimeScaleService`

**Given** the same DASH started just outside the window
**When** the hit lands during i-frames
**Then** the hit is avoided (`HitAvoided`-equivalent log in this milestone) and no Perfect Dodge is raised

**Given** pause and time-slow requests overlap
**When** `TimeScaleService` resolves them
**Then** the lowest scale wins and only `TimeScaleService` writes `Time.timeScale` (grep check in review)

### Story 2.5: Pause and debug menu v1 with aim and input toggles

- **ID:** 2.5 | **Milestone:** M4a | **Est. human:** 20 min
- **Refs:** FR-29 (partial), FR-3, FR-4, NFR-3; AD-1, AD-12
- **Tests:** Manual: navigate the menu with gamepad only and with keyboard only.

As a tuner,
I want a controller-navigable pause menu with Aim Assist, Direction Lock, input buffer and Perfect Dodge feedback toggles,
So that we can compare settings during a play session without the Inspector.

**Acceptance Criteria:**

**Given** Play Mode
**When** Pause is pressed
**Then** the game pauses through `TimeScaleService` and a uGUI menu (in `GMR.DevTools` for debug items) is navigable with gamepad and keyboard via `InputSystemUIInputModule`
**And** toggles for Aim Assist, Direction Lock, Perfect Dodge flash and time-slow change behavior immediately and persist for the session
**And** Runtime code has no reference to `GMR.DevTools`

---

## Epic 3: Heracles Fights Training Dummies

Heracles' three data-driven abilities hit training dummies with full hit feedback.

### Story 3.1: Health, damage pipeline, combat events and training dummies

- **ID:** 3.1 | **Milestone:** M4b | **Est. human:** 20 min
- **Refs:** FR-9 (foundation), FR-27 (foundation); AD-9, AD-11, AD-16
- **Tests:** EditMode `HealthTests`, `DamageSystemTests` (invulnerable target avoided, knockback x (1 - resistance), FIFO re-entrancy, 256 cap, events raised only by DamageSystem).

As a Player,
I want training dummies that take damage, get knocked back and respawn,
So that every attack has something to hit and numbers are consistent.

**Acceptance Criteria:**

**Given** `DamageSystem.Apply(target, request)` on a dummy with 100 HP and knockback resistance 0.5
**When** a 10-damage request with knockback (4,0) is applied
**Then** HP becomes 90, the resolved impulse is (2,0), `HitLanded` is raised with the request's `SourceId`, and only `ApplyResolved` applies the impulse

**Given** a target with an active invulnerability window
**When** damage is applied
**Then** no HP changes and `HitAvoided` is raised

**Given** a request enqueued during resolution
**When** the frame resolves
**Then** it runs FIFO in the same frame, and the 257th request in a frame is dropped with a `[GMR/Combat]` warning

**Given** `Sandbox.unity`
**When** a dummy reaches 0 HP
**Then** `EnemyDied` is raised and the dummy respawns after a configurable delay (DevTools)

### Story 3.2: Ability slots, AbilityRunner and Thrust hit resolver (Club Strike)

- **ID:** 3.2 | **Milestone:** M4b | **Est. human:** 30 min
- **Refs:** FR-9, FR-8 (ability buffering), FR-6 (DASH cancels recovery), FR-1 (`rootsCaster`), FR-28 (cooldown display stub); AD-8, AD-16
- **Tests:** EditMode `AbilityRunnerTests` (startup/active/recovery phases, cooldown, Basic cooldown forced 0 via validation, DASH cancel during recovery only, buffered press executes when legal).

As a Player,
I want Basic, Special and Signature buttons that run data-driven abilities, starting with Heracles' Club Strike,
So that kits can be assembled from assets without code.

**Acceptance Criteria:**

**Given** `AbilityDefinition` and `HeroDefinition` (`basic`, `special`, `signature`, `ultimate`, `dash`, `move`, `rage`, `chains[]`, `primaryStatus`)
**When** a Basic asset is given a nonzero cooldown
**Then** `OnValidate` forces it to 0

**Given** Heracles' `Ability_Heracles_ClubStrike` (Thrust, Box hit shape)
**When** Basic is pressed facing a dummy in range
**Then** the dummy is hit along the Aim Direction via `Physics2D.Overlap*` with layer masks, and `AbilityUsed` is raised by `AbilityRunner`
**And** the Hero keeps moving unless `rootsCaster` is set

**Given** an ability in its recovery phase
**When** the Player dashes
**Then** recovery is cancelled and the DASH starts

**Given** Special or Signature on cooldown
**When** the button is pressed
**Then** nothing fires, and a minimal on-screen cooldown readout shows remaining seconds

### Story 3.3: Spin and Shot resolvers and the full Heracles kit

- **ID:** 3.3 | **Milestone:** M4b | **Est. human:** 30 min
- **Refs:** FR-9, FR-10; AD-8
- **Tests:** EditMode `ProjectileConfigTests` (validation); manual: kit check against 3 dummies.

As a Player,
I want Thunderbolt (Shot) and Storm Whirl (Spin) alongside Club Strike,
So that Heracles has a complete close-and-ranged kit.

**Acceptance Criteria:**

**Given** `Hero_Heracles` with Club Strike (Basic), Thunderbolt (Special, 3 s), Storm Whirl (Signature, 8 s)
**When** Storm Whirl is used among three dummies inside its radius
**Then** all three are hit (Circle shape around the Hero)

**Given** Thunderbolt
**When** fired
**Then** a projectile (trigger collider, `Projectile` layer) travels along the Aim Direction and hits the first dummy

**Given** the team swaps Thunderbolt into the Basic slot asset field
**When** Play Mode is re-entered
**Then** it works with no code change (and its cooldown is forced to 0)

### Story 3.4: FeedbackRouter: flash, hitstop, knockback, shake, damage numbers, SFX hooks

- **ID:** 3.4 | **Milestone:** M4b | **Est. human:** 30 min (juice tuning)
- **Refs:** FR-27, FR-29 (damage numbers toggle); AD-11, AD-12
- **Tests:** EditMode `FeedbackConfigTests` (hitstop 0 = no time-scale request); manual juice check.

As a Player,
I want hits to feel impactful and being hit to be obvious,
So that combat reads clearly with placeholder shapes.

**Acceptance Criteria:**

**Given** `FeedbackConfig` (hitstop light 0.03 s / heavy 0.08 s, shake, flash, damage numbers, SFX hooks), each with an on/off toggle
**When** a hit lands
**Then** `FeedbackRouter` (subscribed to `CombatEvents` only) flashes the target, requests hitstop via `TimeScaleService`, shows a floating damage number, and shakes the camera on heavy hits
**And** placeholder SFX hooks fire for hit, DASH and Perfect Dodge (silent or blips allowed)

**Given** hitstop set to 0 or damage numbers toggled off (pause menu)
**When** hits land
**Then** no hitstop request is made / no numbers appear

**Given** the Player is damaged by the dummy attacker
**When** `PlayerDamaged` is raised
**Then** the Hero flashes red and a 0.5 s post-hit invulnerability window is registered in `InvulnerabilityTracker`

---

## Epic 4: Combo, RAGE and Ultimate Loop

### Story 4.1: Per-Player ComboCounter with fixed and shrinking windows

- **ID:** 4.1 | **Milestone:** M5a | **Est. human:** 20 min
- **Refs:** FR-13, FR-14, FR-29 (shrink toggle), NFR-2, NFR-7; AD-4, AD-11
- **Tests:** EditMode `ComboCounterTests` (increment, timeout at 2.0 s, reset on damage, avoided hit does not reset, shrinking curve at 30 >= min 0.8 s, only own `SourceId` counted).

As a Player,
I want a Combo counter that grows while I hit without being hit,
So that skilful play is visible and rewarded.

**Acceptance Criteria:**

**Given** a `ComboCounter` for Player 0 with `ComboConfig` Fixed 2.0 s
**When** `HitLanded` events with source Player 0 arrive within the window
**Then** Count increments by 1 per hit (repeat hits on the same enemy count)
**And** events from another `SourceId` are ignored

**Given** Count 18
**When** `PlayerDamaged` for Player 0 is raised, or 2.0 s pass with no hit
**Then** Count resets to 0 and `ComboReset` is raised with the reason (damage / timeout); `HitAvoided` does not reset

**Given** `ComboConfig.mode = Shrinking` (toggleable in the pause menu)
**When** Count is 30
**Then** the window equals the curve value and never goes below the minimum

### Story 4.2: Per-Player RageMeter with fill sources and decay

- **ID:** 4.2 | **Milestone:** M5a | **Est. human:** 20 min
- **Refs:** FR-15, FR-16, FR-7 (Perfect Dodge RAGE), NFR-2, NFR-7; AD-11, AD-16
- **Tests:** EditMode `RageMeterTests` (hit 2%, status tick/chain 0.5%, +5% per 10 Combo, Perfect Dodge 10%, clamp 0-1, grace 1.5 s then 8%/s, full RAGE reaches 0 in grace + 100/8 s, `HitAvoided` grants nothing).

As a Player,
I want RAGE to fill from aggressive, skilful play and drain when I hold back,
So that I am pushed to spend it.

**Acceptance Criteria:**

**Given** a `RageMeter` for Player 0 with `RageConfig` defaults
**When** a direct hit, a Combo milestone (10, 20, ...) and a `PerfectDodged` occur
**Then** RAGE increases by 2%, 5% and 10% respectively, clamped to 100%, and `RageChanged` is raised; reaching 100% raises `RageFull` once

**Given** damage tagged `StatusTick` or `ChainJump`
**When** it lands
**Then** RAGE grows at the status rate (0.5%)

**Given** full RAGE and no further gains
**When** time advances
**Then** decay starts after 1.5 s at 8%/s and RAGE reaches 0 at 1.5 + 12.5 s (+-1 frame)

### Story 4.3: Per-Player combat HUD (HP, Combo, RAGE, cooldowns)

- **ID:** 4.3 | **Milestone:** M5a | **Est. human:** 30 min (readability check at 640x400)
- **Refs:** FR-28, FR-13 (HUD part), NFR-2, NFR-6; AD-4, AD-11
- **Tests:** Manual: HUD readable in Game view at 640x400 and on the Steam Deck screen size (1280x800); no EditMode (view-only).

As a Player,
I want to see my HP, Combo with its draining window bar, RAGE meter and cooldowns,
So that I can read the fight at a glance.

**Acceptance Criteria:**

**Given** a Player joins
**When** the HUD spawns
**Then** one HUD panel is instantiated per Player and bound to that Player's `PlayerCombatState` and Hero (no static lookup)
**And** it shows HP (number + bar), Combo count + draining window bar + a visible reset cue, RAGE meter with a distinct full state, Special/Signature cooldowns, and a Run Seed placeholder field

**Given** the HUD subscribes to `CombatEvents`
**When** the panel is disabled
**Then** it unsubscribes in `OnDisable`

### Story 4.4: Ultimate gate and Heracles' Wrath of Zeus

- **ID:** 4.4 | **Milestone:** M5a | **Est. human:** 25 min
- **Refs:** FR-17 (Heracles; Shock part follows in 5.2); AD-8, AD-12, AD-16
- **Tests:** EditMode `UltimateGateTests` (input ignored below 100%, Consume sets 0, i-frame window registered at startup).

As a Player,
I want to unleash Wrath of Zeus when RAGE is full,
So that building Combo pays off with a big moment.

**Acceptance Criteria:**

**Given** RAGE < 100%
**When** Ultimate is pressed
**Then** nothing fires and a "not ready" cue plays

**Given** RAGE = 100%
**When** Ultimate is pressed
**Then** `Ability_Heracles_WrathOfZeus` (`AllEnemies` hit shape, `Ultimate` tag) hits every dummy in the scene with heavy damage, RAGE is consumed to 0, `UltimateUsed` is raised by `AbilityRunner`, an i-frame window covers startup, and a heavy screen shake plays

---

## Epic 5: Shock, Wet and Conducted Shock

### Story 5.1: Generic Status System core (definitions, container, ticker, visuals)

- **ID:** 5.1 | **Milestone:** M5b | **Est. human:** 25 min
- **Refs:** FR-18, NFR-7; AD-9, AD-10, AD-16
- **Tests:** EditMode `StatusContainerTests` (apply, refresh rule, max stacks, expiry by `Tick(dt)`, source attribution stored, `StatusApplied` raised only by container).

As a designer,
I want statuses defined as assets and processed generically,
So that Burn, Curse, Slow and Rhythm can be added later without touching the core.

**Acceptance Criteria:**

**Given** a `StatusDefinition` with duration 6 s, `maxStacks` 1 and refresh rule `RefreshDuration`
**When** it is applied twice to a dummy 2 s apart
**Then** one instance exists with remaining duration reset to 6 s, and it expires after 6 s of `Tick`

**Given** a damageable with a `StatusContainer`
**When** a status is applied with `SourceId` Player 0
**Then** the instance stores the source, `StatusApplied` is raised, and the entity shows the status tint and an icon above it

**Given** `StatusTicker` in the scene
**When** a frame runs
**Then** each registered container is ticked exactly once

**Given** a code review of the status folder
**When** a reviewer adds a test-only `StatusBehaviour` subclass in the test assembly
**Then** it works without editing `StatusContainer` (`StatusContainerTests.CustomBehaviour_NoCoreChange`)

### Story 5.2: Shock with chain lightning on Heracles' abilities

- **ID:** 5.2 | **Milestone:** M5b | **Est. human:** 30 min
- **Refs:** FR-19, FR-10 (Shock payload), FR-13 (chain hits count), FR-17 (Ultimate applies Shock at max chain), FR-26 (tie-break stream); AD-2, AD-10, AD-16
- **Tests:** EditMode `ChainLightningBehaviourTests` (4 targets in a line spaced < R with N >= 3: all hit once; falloff x0.7 per jump; no revisits; tie-breaks deterministic with a fixed seed; attributed to original Player); manual: lightning line visible.

As a Player playing Heracles,
I want Thunderbolt and Storm Whirl to chain lightning between nearby enemies,
So that hitting groups feels powerful and readable.

**Acceptance Criteria:**

**Given** `Status_Shock` with `ChainLightningBehaviour` (N 3, R 3.5 u, falloff 0.7)
**When** Thunderbolt hits the first of 4 dummies in a line spaced 3 u apart
**Then** all 4 take damage, each once, with jump damage falloff, and a visible lightning line connects each jump

**Given** two candidates at equal distance
**When** the chain picks the next target
**Then** the choice comes from the `Status` RNG stream and is identical across runs with the same seed

**Given** a chain that hits 4 enemies
**When** Combo is read for the applying Player
**Then** it increased by 4 (chain jumps carry the original `SourceId` and the `ChainJump` tag)

**Given** Storm Whirl and Wrath of Zeus
**When** they hit
**Then** they apply Shock; Wrath of Zeus applies it at maximum chain count

### Story 5.3: Wet status and the data-driven Wet-conducts-Shock rule

- **ID:** 5.3 | **Milestone:** M5b | **Est. human:** 25 min
- **Refs:** FR-20, FR-21, FR-18; AD-9, AD-10
- **Tests:** EditMode `StatusInteractionTests` (Wet target: Shock damage x2.0; chain leaving Wet target gets +1 jump and +1.5 u radius; `consumeTargetStatus` false keeps Wet, true removes it; `ConductedShock` raised); manual: debug "Apply Wet to all" key.

As a Player,
I want Wet enemies to conduct lightning harder and farther,
So that the hero-synergy idea can be seen before Theseus exists.

**Acceptance Criteria:**

**Given** `Status_Wet` (6 s, blue tint, no damage) and `Rule_WetConductsShock` in the `StatusRuleSet`
**When** a DevTools key applies Wet to all dummies and Heracles hits one with Thunderbolt
**Then** Shock damage on each Wet dummy is 2.0x its dry damage, the chain reaches at least one more dummy than in the same dry layout, and a distinct blue-white arc plays
**And** `ConductedShock` is raised by `StatusContainer` for each conducted application

**Given** Wet re-applied before expiry
**When** time passes
**Then** duration refreshes; Wet expires after 6 s without refresh and is not consumed by Shock (default)

---

## Epic 6: Enemies That Fight Back

### Story 6.1: EnemyBrain FSM, targeting and the Chaser (Minotaur-man)

- **ID:** 6.1 | **Milestone:** M5c | **Est. human:** 40 min (first real fight)
- **Refs:** FR-24, FR-26 (Ai stream), FR-29 (god mode, enemy freeze), FR-7 (real pending hits); AD-4, AD-12, AD-13, AD-16
- **Tests:** EditMode `NearestPlayerTargetSelectorTests` (nearest living Player, ignores dead), `EnemyFsmTests` (state transitions with injected dt; telegraph >= `minTelegraph`); manual: dodge the charge.

As a Player,
I want a fast Minotaur-man that winds up and charges at me,
So that I am forced to move and can practise Perfect Dodges on a real enemy.

**Acceptance Criteria:**

**Given** `Enemy_Chaser` (`ArchetypeDefinition`: HP 30, speed, knockback resistance, `EnemyAttackDefinition` Charge, telegraph 0.4 s)
**When** it is spawned via a DevTools spawn key in `Sandbox.unity`
**Then** it runs `Spawning -> Approach -> Telegraph -> Attack -> Recover`, picks its target only through `ITargetSelector`, and registers a `PendingHit` when the telegraph starts

**Given** the charge connects outside i-frames
**When** contact damage applies
**Then** it goes through `DamageSystem`, the Player's Combo resets, and the Hero flashes red

**Given** the Chaser is hit
**When** damage resolves
**Then** it flashes, is knocked back per its resistance, and at 0 HP plays a death effect and raises `EnemyDied`

**Given** behavior variation (e.g. approach jitter)
**When** it is drawn
**Then** it uses `Ai.Derive(archetypeId, spawnIndex)`

**Given** the pause menu
**When** god mode or enemy freeze is toggled
**Then** the Player takes no damage / enemies stop acting

### Story 6.2: Shooter (Cretan Archer) with aim-line telegraph

- **ID:** 6.2 | **Milestone:** M5c | **Est. human:** 25 min
- **Refs:** FR-24; AD-13
- **Tests:** EditMode `RangedShotPatternTests` (keeps preferred range, retreats when Player closer than threshold); manual: aim line visible during wind-up.

As a Player,
I want an archer who keeps its distance and fires telegraphed arrows,
So that I must close distance or use Shots.

**Acceptance Criteria:**

**Given** `Enemy_Shooter` (HP 40, preferred range, `RangedShot` pattern)
**When** the Player approaches inside its retreat threshold
**Then** it backs away; outside it holds range

**Given** a shot wind-up (>= 0.4 s)
**When** the telegraph runs
**Then** an aim line is shown (visually distinct from the Chaser's telegraph) and a `PendingHit` is registered for the arrow's projected arrival

### Story 6.3: Tank (Bronze Automaton) with area slam

- **ID:** 6.3 | **Milestone:** M5c | **Est. human:** 25 min
- **Refs:** FR-24; AD-9 (empty `damageModifiers`), AD-13
- **Tests:** EditMode `SlamPatternTests` (area overlap and telegraph 0.8 s); manual: TTK roughly 6-10 s with Heracles focused attack (SM-4 sanity check).

As a Player,
I want a slow, heavy automaton with a big telegraphed slam,
So that I need a long Combo or a status synergy to bring it down.

**Acceptance Criteria:**

**Given** `Enemy_Tank` (HP 160, high knockback resistance, `Slam` pattern, telegraph 0.8 s, area shown on the ground)
**When** it slams
**Then** every Player inside the area outside i-frames takes damage, and the area telegraph is visually distinct from the other two Archetypes

**Given** Heracles' kit
**When** the Tank is hit
**Then** it barely moves from knockback, and `damageModifiers` is empty by default (armor toggle exists as data only)

---

## Epic 7: Seeded Arena Encounter

### Story 7.1: WaveBuilder from EncounterDefinition with the Waves stream

- **ID:** 7.1 | **Milestone:** M5d | **Est. human:** 20 min
- **Refs:** FR-25, FR-26, NFR-1, NFR-7; AD-2, AD-13
- **Tests:** EditMode `WaveBuilderTests` (same seed -> same waves; budget respected; max simultaneous 8; drawing from `Combat` does not change result).

As a designer,
I want wave compositions generated from a budget and weighted Archetype table using the Run Seed,
So that encounters vary but are reproducible.

**Acceptance Criteria:**

**Given** `Encounter_Prototype` (3 waves, budget per wave, weighted table Chaser/Shooter/Tank, max simultaneous 8, timeout)
**When** `WaveBuilder.Build(def, rng.Stream(Waves))` runs twice with seed 42
**Then** both results are identical and every wave's cost <= its budget

**Given** 500 draws from the `Combat` stream before building
**When** the waves are built with the same seed
**Then** the result is unchanged

### Story 7.2: Arena scene, WaveSpawner and spawn telegraphs

- **ID:** 7.2 | **Milestone:** M5d | **Est. human:** 35 min (first full Encounter playthrough)
- **Refs:** FR-25, FR-26 (Spawns stream); AD-13, AD-16
- **Tests:** PlayMode `ArenaSmokeTest` (load `Arena_Prototype` with fixed seed, run 5 s with no input, no exceptions, expected wave-1 composition).

As a Player,
I want to fight three waves in a walled arena with pillars,
So that I can play a full Encounter.

**Acceptance Criteria:**

**Given** `Arena_Prototype.unity` (640x400 room, walls, pillars, spawn markers)
**When** the Encounter starts
**Then** `WaveSpawner` spawns wave 1 at markers chosen from the `Spawns` stream, raising `WaveStarted` and `EnemySpawned`

**Given** a spawning enemy
**When** its spawn telegraph plays
**Then** it is invulnerable and harmless until spawning ends

**Given** the current wave is cleared or its timeout elapses
**When** the spawner updates
**Then** `WaveCleared` is raised and the next wave starts

### Story 7.3: Encounter end, results panel, restart and seed controls

- **ID:** 7.3 | **Milestone:** M5d | **Est. human:** 25 min
- **Refs:** FR-25, FR-26, FR-28 (Run Seed on HUD), FR-29 (seed entry, restart); AD-16
- **Tests:** Manual: replay same seed shows the same wave 1 spawn positions; EditMode `EncounterResultTests` (aggregation of clear time, max Combo, damage taken, Perfect Dodges, Ultimates).

As a Player,
I want a results panel when I win or die, with restart and same-seed replay,
So that I can compare runs (UJ-4).

**Acceptance Criteria:**

**Given** the last wave cleared or the Player's HP reaches 0
**When** `EncounterBootstrap` raises `EncounterEnded`
**Then** a results panel shows clear time, max Combo, damage taken, Perfect Dodges, Ultimates used and the Run Seed, with Restart and "Replay same seed"

**Given** the HUD
**When** an Encounter runs
**Then** the Run Seed is visible

**Given** the debug menu
**When** a seed is typed and Restart is chosen
**Then** the Encounter uses that seed; two runs with the same seed and no input give identical wave compositions and spawn positions

### Story 7.4: Spawn Player 2 debug command and SecondPlayerSmokeTest

- **ID:** 7.4 | **Milestone:** M5d | **Est. human:** 25 min (plug in a second gamepad)
- **Refs:** NFR-2, FR-13/FR-15 per-Player; AD-4
- **Tests:** PlayMode `SecondPlayerSmokeTest` (virtual gamepad joins; two registered Players with independent Combo). Slip rule: if M5d runs long, this story moves to M5e (architecture note).

As the future team,
I want to prove a second Player can join with zero combat-code changes,
So that co-op stays possible.

**Acceptance Criteria:**

**Given** the Arena running with one Player
**When** the DevTools command "Spawn Player 2" runs with a second gamepad connected
**Then** `PlayerInputManager.JoinPlayer` adds Player 1 with its own Hero, HUD panel, Combo and RAGE, and enemies target the nearest living Player

**Given** the change set for this story
**When** reviewed
**Then** no file under `Scripts/Runtime/{Combat,Abilities,Status,Enemies}` or `Scripts/Core` was modified

---

## Epic 8: Theseus, Ability Chains and Hero Swap

### Story 8.1: Theseus kit (Trident Thrust, Riptide Bolt, Maelstrom)

- **ID:** 8.1 | **Milestone:** M5e | **Est. human:** 30 min
- **Refs:** FR-11, FR-9; AD-8
- **Tests:** EditMode `PiercingProjectileTests` (pierce count); manual: knockback visibly larger than Heracles' (Theseus selected via the Inspector `HeroDefinition` field until 8.4).

As a Player,
I want Theseus with a long trident thrust, a piercing water bolt and a pushing whirlpool,
So that the second Hero plays differently and applies Wet.

**Acceptance Criteria:**

**Given** `Hero_Theseus` (speed 6.0, lower damage, higher knockback) with Trident Thrust (Basic), Riptide Bolt (Special, piercing, Wet), Maelstrom (Signature, outward push, Wet)
**When** each ability hits enemies
**Then** Riptide Bolt and Maelstrom apply `Status_Wet`, Riptide Bolt passes through multiple enemies, and Maelstrom pushes nearby enemies outward hard

**Given** the same enemy group hit by Heracles' Club Strike and by Theseus' Trident Thrust
**When** knockback resolves
**Then** Theseus separates enemies visibly more (knockback values in assets)

### Story 8.2: Theseus' Ultimate, Poseidon's Flood

- **ID:** 8.2 | **Milestone:** M5e | **Est. human:** 15 min
- **Refs:** FR-17 (Theseus); AD-8 (`ExpandingWave` hit shape)
- **Tests:** EditMode `ExpandingWaveShapeTests` (radius over time); manual.

As a Player playing Theseus,
I want a flood that sweeps the arena outward,
So that my Ultimate sets up Wet on everything.

**Acceptance Criteria:**

**Given** Theseus with full RAGE
**When** Ultimate is pressed
**Then** an expanding wave from Theseus damages every enemy it reaches, applies Wet to each and knocks them back strongly, and RAGE is consumed
**And** the "not ready" and startup i-frame rules from Story 4.4 apply unchanged

### Story 8.3: Data-driven Ability Chains (Thunderhead and Undertow)

- **ID:** 8.3 | **Milestone:** M5e | **Est. human:** 25 min
- **Refs:** FR-12; AD-8
- **Tests:** EditMode `ChainTrackerTests` (follow-up inside 1.0 s window triggers; outside does not; armed state exposed for cue; `ChainTriggered` raised).

As a Player,
I want specific ability sequences to trigger bonus effects with a visible "armed" cue,
So that the kits reward deliberate play.

**Acceptance Criteria:**

**Given** `Chain_Heracles_Thunderhead` (Storm Whirl -> Thunderbolt, 1.0 s, `SplitProjectileBonus` x3 fan)
**When** Thunderbolt starts within 1.0 s after Storm Whirl
**Then** it fires 3 bolts in a fan and `ChainTriggered` is raised; after the window it fires one bolt

**Given** `Chain_Theseus_Undertow` (Maelstrom -> Trident Thrust, `ReverseKnockbackBonus`)
**When** the follow-up lands in the window
**Then** enemies in front are pulled toward Theseus

**Given** the window is open
**When** the HUD/Hero renders
**Then** a visible armed cue shows; a new chain using an existing bonus type needs only a new asset

### Story 8.4: Hero Select screen and quick restart

- **ID:** 8.4 | **Milestone:** M5e | **Est. human:** 20 min
- **Refs:** FR-22, NFR-3
- **Tests:** Manual: select with gamepad only and keyboard only; quick restart repeats the last Hero.

As a Player,
I want to choose Heracles or Theseus before each Encounter,
So that I can play either Hero.

**Acceptance Criteria:**

**Given** the Arena scene start
**When** Hero Select shows
**Then** both Heroes are selectable with controller and keyboard, and the chosen `HeroDefinition` builds the `HeroRuntime`

**Given** the results panel
**When** Restart is chosen
**Then** Hero Select returns; "Quick restart" repeats the last Hero without the screen

### Story 8.5: In-place Hero Swap with carry-over config

- **ID:** 8.5 | **Milestone:** M5e | **Est. human:** 25 min (synergy playtest, UJ-2)
- **Refs:** FR-23, FR-28 (active Hero, swap cooldown), FR-29 (swap toggle); AD-6, AD-16
- **Tests:** EditMode `HeroSwapRulesTests` (blocked during DASH/Ultimate/cooldown; carry flags applied to Combo, RAGE, HP fraction; incoming cooldowns ready); manual: Wet still on enemies right after swapping to Heracles.

As a Player,
I want to swap to the other Hero mid-fight,
So that I can soak enemies with Theseus and electrify them with Heracles.

**Acceptance Criteria:**

**Given** `HeroSwapConfig` (enabled, cooldown 4 s, carryCombo/Rage/HpFraction true)
**When** HeroSwap is pressed outside DASH and Ultimate
**Then** `HeroSwapper` destroys the current `HeroRuntime` and builds the other at the same position, copies `Health.Fraction`, keeps Combo and RAGE, starts incoming cooldowns ready, and raises `HeroSwapped`
**And** all enemy statuses, the Arena and wave progress are unchanged

**Given** swap pressed during DASH, Ultimate or swap cooldown, or with swap disabled in the pause menu
**When** processed
**Then** nothing happens

**Given** the HUD
**When** playing
**Then** it shows the active Hero and the swap cooldown

---

## Epic 9: Playtest Telemetry and the "Is It Fun" Gate

### Story 9.1: TelemetryRecorder writing per-session JSONL

- **ID:** 9.1 | **Milestone:** M5f | **Est. human:** 30 min
- **Refs:** FR-30, FR-7, FR-16, FR-21, FR-23 (telemetry consequences); AD-15, AD-3 (UtcNow allow-list)
- **Tests:** EditMode `TelemetryAggregatorTests` (TTK spawn-to-death and first-hit-to-death per Archetype, combo resets with reason, i-frame share, RAGE-full vs. used-before-decay, conducted shocks within N s after swap); manual: inspect a file after one Encounter.

As the team,
I want every session to write a local telemetry file,
So that we can measure the Success Metrics.

**Acceptance Criteria:**

**Given** a play session
**When** an Encounter ends or the game quits
**Then** one file `persistentDataPath/telemetry/session-<yyyyMMdd-HHmmss>Z.jsonl` holds lines `{schemaVersion, t, frame, type, ...}` and one `encounter_summary` per Encounter with all FR-30 aggregates and the RunSeed
**And** `TelemetryRecorder` only subscribes to `CombatEvents` (no gameplay code calls it) and makes no network calls

### Story 9.2: Combat Intensity tracker and results summary

- **ID:** 9.2 | **Milestone:** M5f | **Est. human:** 20 min
- **Refs:** FR-31, FR-30 (summary on results panel); AD-14
- **Tests:** EditMode `CombatIntensityTrackerTests` (weights, smoothing, clamp 0-1, `Max` aggregation across two Players).

As the team,
I want a combat intensity value logged now and a telemetry summary on the results panel,
So that M6 music has a tested input and testers see their stats.

**Acceptance Criteria:**

**Given** `IntensityConfig` weights for enemy count, RAGE and HP plus smoothing
**When** combat state changes
**Then** `ICombatIntensitySource.Current` moves smoothly within 0-1 and is logged to telemetry periodically
**And** the results panel shows a short telemetry summary (combo max, dashes, perfect dodges, ultimates, conducted shocks)

### Story 9.3: Performance stress check and playtest build

- **ID:** 9.3 | **Milestone:** M5f | **Est. human:** 35 min (copy build to Steam Deck, run stress scene)
- **Refs:** NFR-5, NFR-9
- **Tests:** Manual: DevTools stress command (15 enemies, 30 projectiles, full Shock chains) holds 60 FPS on Steam Deck at 1280x800; profiler capture attached to the gate report.

As the team,
I want a Windows development build that runs on PC and Steam Deck at 60 FPS under load,
So that playtests measure the game, not the frame rate.

**Acceptance Criteria:**

**Given** a Windows development build
**When** run on the Steam Deck with the stress command
**Then** the FPS readout stays at 60 (or the gate report lists the hotspot and proposes pooling per the Deferred list)
**And** a playtest checklist (questionnaire Q1-Q10 from PRD 7.4, facilitator notes) is saved next to the build

### Story 9.4: Team playtest and tuning pass

- **ID:** 9.4 | **Milestone:** M5f | **Est. human:** 40 min (both team members, 3 Encounters per Hero)
- **Refs:** SM-1..SM-11, NFR-4
- **Tests:** Human playtest; telemetry files produced.

As the team,
I want to play several Encounters with each Hero and apply quick tuning changes in Tuning Assets,
So that the build is in shape before external testers.

**Acceptance Criteria:**

**Given** the playtest build
**When** each team member plays >= 3 Encounters per Hero
**Then** telemetry files exist for each session, and the agent produces a metric summary vs. SM-4..SM-11 and SM-C1..C4 from them
**And** any tuning change is made in assets only (no code), and listed in the gate report

### Story 9.5: External playtests and the gate verdict

- **ID:** 9.5 | **Milestone:** M5g | **Est. human:** 120 min (3 testers x ~25 min facilitation + 45 min review)
- **Refs:** SM-1..SM-11, SM-C1..C4, PRD 7.3 gate rule
- **Tests:** Human playtest; agent-produced metrics report.

As the team,
I want at least 3 external testers to play and answer the questionnaire,
So that we get an honest "is combat fun" verdict.

**Acceptance Criteria:**

**Given** >= 3 external testers, each playing >= 3 Encounters per Hero
**When** questionnaires and telemetry are collected
**Then** the agent reports each SM against its threshold, applies the gate rule (SM-1, SM-2, SM-3 + >= 5 of SM-4..SM-11), and proposes exactly 3 tuning changes
**And** the report states whether SM-6 failed (which triggers Story 9.6)

### Story 9.6 (conditional): Right-stick / mouse aim mode

- **ID:** 9.6 | **Milestone:** only if SM-6 fails - scheduled into the post-gate tuning session | **Est. human:** 25 min
- **Refs:** FR-5; AD-7 (`AimConfig.mode`)
- **Tests:** EditMode `AimResolverTests.RightStickMode`.

As a Player who struggles with movement aim,
I want to aim with the right stick or mouse,
So that I can hit what I aim at.

**Acceptance Criteria:**

**Given** `AimConfig.mode = RightStick` selected in the pause menu
**When** the right stick (or mouse) points in a direction
**Then** abilities fire in that direction while movement remains on the left stick; Aim Assist still applies if on

---

## Epic 10: Dynamic Combat Music

### Story 10.1: Generate four independent placeholder tracks with ACE-Step

- **ID:** 10.1 | **Milestone:** M6-pre (separate session before M6) | **Est. human:** 90-120 min (generation + listening iterations)
- **Refs:** FR-32 (placeholder content), kickoff M6
- **Tests:** Manual listening; `manifest.md` complete.

As the team,
I want four distinct tracks (calm, combat, advanced-combat, boss) that differ clearly in tempo, key and feel,
So that M6 can crossfade between them and each state change is audible.

**Acceptance Criteria:**

**Given** ACE-Step 1.5 (tier1, turbo INT8, no LM, `[Instrumental]`)
**When** four tracks are generated with detailed English captions (instruments, mood, BPM, key); test values: calm 90 BPM D minor, combat 150 BPM D minor, advanced-combat 176 BPM D minor (distinct added element, e.g. faster tempo), boss 160 BPM C minor
**Then** files `AI_PLACEHOLDER_calm/_combat/_advanced/_boss.*` sit in `game/Assets/Audio/Placeholder/`, each loops cleanly on its own, they need not share BPM, key or length, and `manifest.md` records caption, seed, BPM and key for each
**And** the manifest states the files are placeholders, not for marketing without approval
**Flag:** if listening iterations exceed the session, stop after one approved set; further variants are a separate session.

### Story 10.2: MusicStateMixer with independent looping tracks

- **ID:** 10.2 | **Milestone:** M6 | **Est. human:** 30 min
- **Refs:** FR-32; AD-14
- **Tests:** PlayMode `MusicLoopTest` (four sources each loop independently; after a crossfade the outgoing source is silent and stopped/paused); manual listening.

As a Player,
I want the four tracks to loop independently through an Audio Mixer with smooth crossfades,
So that state changes sound clean and clearly different.

**Acceptance Criteria:**

**Given** an Audio Mixer with groups Calm, Combat, Advanced, Boss under Music
**When** the Arena starts
**Then** `MusicStateMixer` has four looping sources, one per track, with no shared tempo, key or DSP-time sync required, and a requested state crossfades from the current track to the target track over the configured fade time without clicks

### Story 10.3: State selection and tuning by ear

- **ID:** 10.3 | **Milestone:** M6 | **Est. human:** 40 min (listening, tuning thresholds)
- **Refs:** FR-31, FR-32; AD-14
- **Tests:** EditMode `MusicStateResolverTests` (living enemies, intensity, boss flag -> target state: calm with no enemies, combat vs advanced by threshold with hysteresis, boss overrides when flagged); manual: hear transitions during an Encounter.

As a Player,
I want the music to switch to a more intense track as the fight intensifies, a boss track for bosses, and calm after a wave,
So that the fight feels alive.

**Acceptance Criteria:**

**Given** `MusicConfig` thresholds and fade times
**When** the resolved state changes (no enemies alive -> calm; `ICombatIntensitySource.Current` crossing the combat/advanced threshold, with hysteresis; encounter boss flag set -> boss)
**Then** the mixer crossfades to the matching track over the configured time, with no clicks
**And** `MusicStateMixer` depends only on `ICombatIntensitySource`, a living-enemy count and the `EncounterDefinition` boss flag, not on combat internals
**And** thresholds and fade times can be tuned live in Play Mode

---

## Epic 11: Prototype Wrap-up

### Story 11.1: Retrospective and next plan

- **ID:** 11.1 | **Milestone:** M7 | **Est. human:** 45 min
- **Refs:** kickoff M7, NFR-8
- **Tests:** n/a (document reviewed at the gate).

As the team,
I want actual vs. estimated time per milestone, combat learnings and a proposal for the Crete vertical slice,
So that we plan the next phase realistically.

**Acceptance Criteria:**

**Given** gate reports from M3-M6 and git history
**When** the agent compiles the wrap-up
**Then** it lists estimated vs. actual human time per milestone, key combat learnings tied to SM results, open PRD questions with recommendations, and a proposed first milestone for the vertical slice (GDD 17.2)

---

## Milestone Plan

Human time = story estimates + gate overhead (reading the report, Unity open, commit approval). Cap: 180 min. No milestone exceeds the cap after the splits below.

| Milestone | Stories | Est. human time | What you can run / see / hear at the end |
| --- | --- | --- | --- |
| **M3** Skeleton | 1.1, 1.2, 1.3 | 100 + 15 = **~115 min** | Project opens on both machines; EditMode tests green from CLI; a square moves with gamepad/WASD in `Sandbox`. |
| **M4a** Movement, aim, DASH | 2.1-2.5 | 135 + 20 = **~155 min** | Hero moves, aim arrow, Assist/Lock toggles in pause menu, DASH with i-frames, Perfect Dodge flash + slow-mo vs. a dummy attacker. |
| **M4b** Heracles vs. dummies | 3.1-3.4 | 110 + 20 = **~130 min** | All three Heracles abilities hitting dummies with hitstop, flash, numbers, shake; DASH-cancel. |
| **M5a** Combo, RAGE, Ultimate | 4.1-4.4 | 95 + 15 = **~110 min** | Combo counter and RAGE meter on HUD; RAGE decays; Wrath of Zeus wipes the dummies. |
| **M5b** Statuses | 5.1-5.3 | 80 + 15 = **~95 min** | Lightning chains across dummies; debug-Wet dummies show conducted blue-white chains and double damage. |
| **M5c** Enemies | 6.1-6.3 | 90 + 20 = **~110 min** | Chaser, Shooter and Tank fight back with distinct telegraphs; god mode/freeze toggles. |
| **M5d** Seeded Arena | 7.1-7.4 | 105 + 20 = **~125 min** | Full 3-wave Encounter in the Arena, results panel, restart, same-seed replay, Player 2 joins on a second pad; PlayMode smoke tests green. |
| **M5e** Theseus + Swap | 8.1-8.5 | 115 + 25 = **~140 min** | Hero Select, Theseus kit + Ultimate, both chains, Hero Swap: soak with Theseus, swap, electrify with Heracles (UJ-2). |
| **M5f** Telemetry + team playtest | 9.1-9.4 | 125 + 10 = **~135 min** | Telemetry JSONL per session, intensity logged, Steam Deck build at 60 FPS, team metrics summary. |
| **M5g** External gate | 9.5 (+ 9.6 if SM-6 fails, in the following session) | **~120 min** | "Is it fun" verdict report + 3 tuning changes. Testers' sessions are calendar time; facilitation is counted here. |
| **M6-pre** Music generation | 10.1 | **~90-120 min** | Four independent AI placeholder tracks + manifest, listenable. Flagged: split again if listening iterations run long. |
| **M6** Dynamic music | 10.2, 10.3 | 70 + 20 = **~90 min** | Hear the music shift calm -> combat -> advanced-combat (and boss) during an Encounter. |
| **M7** Wrap-up | 11.1 | 45 + 15 = **~60 min** | Retrospective doc and vertical-slice proposal. |

**Splits made vs. the architecture proposal:** the architecture's M5a (combat core, 2-3 h, at risk) is split into **M5a** (Combo/RAGE/Ultimate) and **M5b** (Status System/Shock/Wet), because statuses plus HUD plus tests exceeded ~3 h of review. Architecture M5b becomes **M5c** (enemies) + **M5d** (arena/waves/Player 2), because the architecture flagged it at 3 h "tight". Architecture M5d becomes **M5f** (build + team playtest) + **M5g** (external playtests), because facilitation alone is ~75 min. M6 music generation is its own session (**M6-pre**) per the architecture flag. **Total:** 13 sessions, ~25 h of human time, about 13 weeks at one session per week.

## Traceability Check

Every PRD FR maps to at least one story (see FR Coverage Map). Status:

- **Covered:** FR-1 to FR-4, FR-6 to FR-32.
- **Explicitly deferred (conditional):** **FR-5** right-stick/mouse aim (PRD priority Could). Only the `AimMode` hook ships in Story 2.2; Story 9.6 is built only if SM-6 fails at the M5g gate.
- **Partially deferred by PRD non-goals (not FR gaps):** rebinding UI (AD-7), object pooling (only if 9.3 shows GC spikes), Tank armor (data toggle only, Story 6.3), pits/hazards.
- **NFRs:** all ten mapped (see NFR table). NFR-5 is verified once in Story 9.3; NFR-4 is a standing criterion of every story.

Validation (Step 4, headless): every story builds only on earlier stories (no forward dependencies; Story 4.4 ships Wrath of Zeus without Shock, and 5.2 adds it); no starter template applies (fresh CLI project, Story 1.1); data assets are created by the first story that needs them; epics are organized by player-visible value; file churn across Epics 3/4/5/8 on `AbilityRunner`/`HeroDefinition` is accepted because each epic ends a feedback loop (feel check) that can change the next.

## Assumptions (headless decisions)

1. Output path is `{planning_artifacts}/epics.md` per the skill; config resolved by hand (`uv`/`python` resolver not needed; `_bmad/bmm/config.yaml`).
2. Human-minute estimates assume AI agents write all code and tests; human time covers review, opening Unity, play checks and approvals. Gate overhead of 10-25 min per milestone is added.
3. Chain-jump damage fills RAGE at the status rate (0.5%) while still counting fully toward Combo (PRD FR-13 assumption), to respect SM-C2.
4. Until Story 8.4, the active Hero is chosen through the `HeroDefinition` field on `PlayerRoot` in the Inspector.
5. Wet is testable before Theseus via a DevTools "apply Wet" key (Story 5.3), so the synergy can be seen at M5b.
6. Telemetry (M5f) is built after the systems it records; event types are defined from M4a onward so no gameplay code changes are needed later.
7. External playtest facilitation counts as human session time (M5g); finding testers is calendar time.
8. M6 music generation is a separate session (M6-pre), per the architecture flag and the user's "music generated beforehand".
9. Ability, chain and Ultimate designs follow the PRD proposals; PRD open question 7 (approval of names/designs) is still open for the human.
10. M7 wrap-up is included as Epic 11 for completeness, though it delivers no player-facing feature.

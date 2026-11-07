# Epic 4 Context: Combo, RAGE and Ultimate Loop

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

A Player builds a Combo by landing hits without being hit, fills RAGE from aggressive and skilful play (hits, Combo milestones, Perfect Dodges), watches it drain when holding back, and spends full RAGE on Heracles' Ultimate, Wrath of Zeus. Everything is shown on a per-Player combat HUD. This closes the core reward loop (UJ-1) that the "is it fun" gate later measures (SM-5 Combo, SM-8 RAGE loop).

## Stories

- Story 4.1: Per-Player ComboCounter with fixed and shrinking windows
- Story 4.2: Per-Player RageMeter with fill sources and decay
- Story 4.3: Per-Player combat HUD (HP, Combo, RAGE, cooldowns)
- Story 4.4: Ultimate gate and Heracles' Wrath of Zeus

## Requirements & Constraints

- Combo: +1 per hit the Player's abilities, Ultimate or statuses deal (status ticks and chain hits count fully; repeat hits on the same enemy count). Resets to 0 when no hit lands within the Combo Window or when the Player takes damage; avoided hits (i-frames) never reset it. Fixed window default 2.0 s. Shrinking mode (pause-menu toggle): window shortens with Combo along a curve (2.0 s at 0 to 1.0 s at 50), never below 0.8 s.
- RAGE (0-1 in code, clamped): direct hit +2%, status tick / chain jump +0.5%, every 10 Combo +5%, Perfect Dodge +10%; `HitAvoided` grants nothing. Decay starts 1.5 s after the last gain at 8%/s, also when full: full RAGE reaches 0 in 1.5 + 12.5 s. Amounts are per-Hero tuning (`HeroDefinition.rage`).
- Ultimate: usable only at 100% RAGE; consumes all RAGE; below 100% the press does nothing and a "not ready" cue plays. Hero has i-frames during Ultimate startup. Heracles' Wrath of Zeus: lightning strikes every enemy in the arena with heavy damage (Shock at max chain count arrives in Epic 5; only the payload hook now), heavy shake.
- HUD per Player: HP (number + bar), Combo + draining window bar + visible reset cue, RAGE meter with a distinct full state, Special/Signature cooldowns, Run Seed placeholder. Readable at 640x400 and 1280x800. Active Hero and swap cooldown come with Epic 8.
- Telemetry for RAGE-full and wasted RAGE comes in Epic 9; events must carry what it needs.
- All numbers in ScriptableObject tuning, read live; placeholder shapes; N-player safe (no statics); BannedApiTests green.

## Technical Decisions

- `PlayerRoot` owns person state: `PlayerCombatState` holds Combo and RAGE (no HP). `HeroRuntime` (replaceable on swap) owns abilities, cooldowns, HP.
- `ComboCounter(PlayerId, ComboConfig)` and `RageMeter(PlayerId, RageConfig)` are Core classes, one per Player, ticked with `dt`, reacting only to events whose `SourceId` is their Player. `ComboConfig` has `ComboMode` Fixed/Shrinking + curve; `RageConfig` holds fill amounts and decay.
- Sole raisers: `ComboChanged`/`ComboReset` by ComboCounter, `RageChanged`/`RageFull` by RageMeter, `UltimateUsed` by AbilityRunner. Events are typed past-tense readonly structs on the per-encounter `CombatEvents`; HUD/feedback are read-only subscribers that unsubscribe in `OnDisable`.
- Ultimate is an ordinary `AbilityDefinition` (hit shape `AllEnemies`, `Ultimate` damage tag) gated by `RageMeter.IsFull`. Ultimate startup i-frames are registered as a timed window on the `InvulnerabilityTracker` (only owner of invulnerability).
- `ICombatIntensitySource` is defined in Core; its tracker ships with telemetry (Epic 9) and is consumed by music (Epic 10).
- UI: uGUI + TextMeshPro. Update order: input -100, gameplay 0, HUD/feedback +100. Gameplay uses scaled time, UI unscaled.

## Cross-Story Dependencies

- 4.1 and 4.2 consume `HitLanded`, `PlayerDamaged`, `PerfectDodged` from Epics 2/3; 4.2 consumes Combo milestones from 4.1.
- 4.3 renders 4.1/4.2 state plus Hero HP and ability cooldowns; replaces the Epic 3 minimal cooldown readout.
- 4.4 needs 4.2 (gate/consume) and shows on 4.3. Epic 5 adds Shock to Wrath of Zeus and makes chain hits count; Epic 8 reuses the gate for Poseidon's Flood and carries Combo/RAGE across Hero Swap.

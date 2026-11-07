---
title: 'Epic 5 (M5b): deterministic test clock, Status System, Shock chain lightning, Wet + Conducted Shock'
type: 'feature'
created: '2026-11-04'
status: 'done'
route: 'dispatch'
baseline_commit: 'cd633f4d54b8131b2ae8289340cc7dc8ce570b0e'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-5-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** (1) The Sandbox PlayMode smoke test fails intermittently (deferred from M5a). (2) Statuses are a no-op stub: Shock does nothing, Wet does not exist, so the Wet + Shock synergy cannot be seen (stories 5.1-5.3).

**Approach:** (1) Make the test timeline machine-speed independent: one frame-consistent unscaled clock for TimeScaleService (capture delta when `captureDeltaTime` is set), no wall-clock waits in the test. (2) Core generic Status System (definition, behaviours, container, rules, ticker), Shock = `ChainLightningBehaviour`, Wet asset + `Rule_WetConductsShock`, placeholder visuals, DevTools "Apply Wet to all" (key + pause-menu button).

## Boundaries & Constraints

**Always:** approved decisions — Wet not consumed by Shock by default (`consumeTargetStatus` false, data toggle); Wet: Shock damage x2, +1 jump, +1.5 u radius per jump leaving a Wet target; status ticks and chain jumps carry `StatusTick`/`ChainJump` (Combo +1, RAGE status rate via the M5a wiring); chain tie-breaks from `RunRng` `Status` stream. All damage through `DamageSystem` (fold: status behaviours then rules; flat before multipliers; `ChainParams` only mutated in the fold). Sole raisers: `StatusApplied`/`StatusExpired`/`ConductedShock` by `StatusContainer`, `ChainJumped` by the chain. `StatusTicker` is the only ticker. Numbers in SOs; no statics; BannedApiTests green; asmdef layering; builder regenerates.

**Never:** retries/sleeps to hide flakiness; Wet logic in Shock code; `UnityEngine.Random`; telemetry (Epic 9); Theseus kit (Epic 8); commits.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected |
|---|---|---|
| Refresh | 6 s, max 1, RefreshDuration; apply at 0 and 2 | 1 instance, remaining 6; expires after 6 s of Tick |
| Stacks | AddStack, max 3; apply x5 | stacks 3 |
| Source | apply from P0 | instance.Source P0, StatusApplied once |
| Custom behaviour | test-only StatusBehaviour subclass | OnApply/OnTick/OnExpire run, no core edit |
| Chain line | 4 targets 3 u apart, N 3, R 3.5, falloff 0.7 | all hit once; jumps 0.7^k x damage; ChainJump tag; source P0 |
| No revisit | targets in a triangle | each hit at most once per chain |
| Tie | two candidates equidistant, same seed twice | same pick |
| Wet x2 | Shock hit on Wet target | damage x2, ConductedShock raised |
| Wet chain | 5 in a line, 4 u apart | dry: 1 hit; all Wet: 5 hit |
| Consume | consumeTargetStatus true | Wet removed; false: kept |
| Max chain | payload MaxChain | chain uses the empowered jump count |

</frozen-after-approval>

## Code Map

- `Scripts/Core/Encounter/TimeScaleService.cs`, `Runtime/Encounter/TimeScaleServiceDriver.cs`, `Runtime/Feedback/{FeedbackRouter,PerfectDodgeFeedback}.cs` -- clock: driver advances `TimeScaleService.UnscaledNow`; requesters stamp with it.
- `Tests/PlayMode/SandboxSmokeTest.cs` -- replace `WaitForSecondsRealtime`; add M5b steps.
- `Scripts/Core/Combat/{DamageSystem,DamageResult,IDamageable,IStatusApplier,StatusPayload,CombatEvents}.cs` -- fold + chain bonus in result, applier gets the result, `IDamageable.Statuses`.
- `Scripts/Core/Status/*` -- new system; `StatusDefinition` extended (keep id/displayName/tint).
- `Scripts/Core/Encounter/EncounterContext.cs` -- `StatusServices`, `StatusRegistry`, applier wiring.
- `Scripts/Core/Abilities/AbilityDefinition.cs` -- `_statusMaxPotency` (Wrath of Zeus; payload `MaxPotency`).
- `Scripts/DevTools/TrainingDummy.cs`, `Runtime/Heroes/HeroRuntime.cs` -- own a StatusContainer, register for ticking, clear on death.
- `Scripts/Runtime/Status/{StatusTicker,StatusVisuals}.cs`, `Runtime/Feedback/ChainLightningFeedback.cs` -- ticker, tint + icons, lightning lines; "CONDUCTED!" popup in FeedbackRouter.
- `Scripts/Core/Controls/InputFrame.cs`, `Runtime/Controls/PlayerInputReader.cs`, `Input/GameControls.inputactions` -- Debug `ApplyWetToAll` (F2 / D-pad up).
- `Scripts/DevTools/DebugStatusTools.cs`, `DebugTogglePanel` menu button -- Apply Wet to all.
- `Scripts/Editor/SandboxBuilder.cs` -- assets (Status_Shock/Wet, Behaviour_ChainLightning_Shock, Rule_WetConductsShock, StatusRuleSet_Default), ticker, visuals, chain lane of 5 dummies, menu row, hint.

## Tasks & Acceptance

**Execution:**
- [x] Clock fix + smoke test without wall-clock waits; 10 consecutive green PlayMode runs.
- [x] Core status system, chain, rules, events, damage fold.
- [x] Runtime ticker/visuals/lightning/popup; DevTools Wet; builder + regenerate.
- [x] EditMode `StatusContainerTests`, `ChainLightningBehaviourTests`, `StatusInteractionTests` (matrix rows); fix fakes for `IDamageable.Statuses`.
- [x] PlayMode: ticker once per frame, Thunderbolt chain + Combo, Apply Wet button, Conducted x2 + longer chain.

**Acceptance Criteria:**
- Given Sandbox, when Thunderbolt hits the first dummy of the chain lane, then lightning lines connect each jump and the HUD Combo rises by the number of dummies hit.
- Given "Apply Wet to all" (F2 / D-pad up / menu), when Thunderbolt hits, then dummies show blue tint + icon, "CONDUCTED!" pops, the arc is blue-white and reaches more dummies.

## Design Notes

Chain = `ChainRun` (per chain: visited set, remaining jumps, jump index). Jump requests carry `StatusPayload(Shock, chain: run)`; when Shock is applied to the jumped target (after its fold put the Wet bonus into `DamageResult.Chain`), `ChainLightningBehaviour.OnApply` continues the same run instead of starting a new one. FIFO queue = no recursion. Jump tags: `Shock|ChainJump` plus `Ultimate` inherited (an Ultimate chain never refills RAGE).

## Verification

**Commands:**
- `unity run game -- -executeMethod GMR.Editor.SandboxBuilder.BuildAll` -- exit 0.
- `unity test game --mode EditMode` -- all green; `--mode PlayMode` x10 -- all green.

## Implementation Notes

- Implemented directly by the build agent (delegated subagent; checkpoints treated as pre-approved by the caller's brief, as in M4a-M5a). Not committed.
- Flaky root cause: TimeScaleService expired hitstop / Perfect-Dodge slow-mo on wall-clock `Time.unscaledTime` while the test fixes scaled time with `captureFramerate` (batchmode frames ~0.3 ms: a 0.03 s hitstop spanned ~100 frames on one run and a few on another), and the test waited `WaitForSecondsRealtime(0.3)` (0.2-16 s of game time). The scaled timeline, dummy positions/attacker phase and frame-counted waits therefore depended on machine speed and load. Fix: `TimeScaleService.UnscaledNow/Advance` advanced once per frame by `FrameClock.UnscaledDeltaTime` (capture delta when capturing), used by every requester and the cosmetic timers; test waits for the slow-mo by frames. Proof: a simulated 10x-faster machine (10x hitstop durations) failed deterministically before the fix and passes after; the original intermittent assert could not be reproduced on demand (12/12 + stress green before the fix).
- Deviations: Sandbox gets a 5-dummy chain lane (y -4.8); smoke test picks the triangle dummies by name and Wrath counts all 8. `StatusPayload` carries `MaxPotency` and the running `ChainRun` (chain continues on landing). Jumps inherit the Ultimate tag. Hard cap `_maxTotalJumps` 12 (Wet +1 per hop is otherwise unbounded). Debug Wet key ignored while paused. PlayMode asmdef references GMR.DevTools.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence / route |
|---|---|---|
| D-pad Wet assert vacuous (menu already set count) | medium | Patched: Wet removed before the tap, then re-asserted |
| Mid-chain Wet bonus / 4 u matrix row untested | medium | Patched: FiveInALine_4uApart_Dry1Hit_AllWet5Hits |
| Wrath max potency not tested via ability/asset | medium | Patched: StatusPayload MaxPotency + WrathOfZeus asset test |
| Wet chain unbounded (+1 per hop) | medium | Patched: `_maxTotalJumps` cap + test |
| Tie-break depends on registration order | medium | Patched: tied sorted by SourceId + test |
| Chain picks enemy without StatusContainer, ends silently | medium | Patched: filtered + test |
| Chain ends on avoided/dead/dropped jump | low | Deferred (unreachable with dummies) |
| D-pad up also navigates pause menu -> Wet | low | Patched: ignored while paused |
| SFX cues not throttled | low | Patched: once per frame per status cue |
| Shock icon blinks whole life (0.6 s) | low | Patched: blink from min(1 s, 30%) |
| InputFrame flag untested | low | Patched: DebugApplyWetPressed test |
| Builder re-applies upgrades each build | low | Deferred |
| Hero statuses not cleared/visible; disable not handled | low | Deferred (no Hero statuses yet) |
| Fold mutation / flat-before-multiplier across behaviours | low | Deferred (documented contract; only rules modify today) |
| ConductedShock lacks dealt amount | low | Deferred to Epic 9 |
| No pooling / perf check for 15 enemies | low | Deferred |
| Break-clears-statuses, F2, popup not PlayMode-checked | low | Deferred |
| HUD pulse still wall clock | low | Rejected: cosmetic, not in any timing path |
| Spec drift (names, unchecked tasks) | low | Patched: this spec |

---
title: 'Epic 4 (M5a): ComboCounter, RageMeter, per-Player combat HUD, Ultimate gate + Wrath of Zeus'
type: 'feature'
created: '2026-11-03'
status: 'done'
route: 'dispatch'
baseline_commit: '73d1d467b843347673f94e55e6f9e4402e907785'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-4-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Hits, damage and Perfect Dodges happen but nothing rewards them: no Combo, no RAGE, no Ultimate, and only a minimal top-left text readout (stories 4.1–4.4).

**Approach:** Core: `ComboCounter`, `RageMeter`, `PlayerCombatState` (binds CombatEvents, filters own SourceId), `UltimateGate`, `ComboConfig`/`RageConfig`, new events (`ComboChanged`, `ComboReset`, `RageChanged`, `RageFull`, `UltimateUsed`, `UltimateNotReady`). Runtime: PlayerRoot hosts PlayerCombatState; Ultimate slot in AbilityLoadout; `AllEnemiesHitResolver`; Ultimate feedback (bolts, flash, strong hitstop/shake, not-ready cue); uGUI `CombatHud` spawning one `PlayerHudPanel` per Player, replacing `AbilityCooldownReadout`. DevTools: "Fill RAGE" button, shrinking-window and RAGE-decay toggles. Builder regenerates assets/scene.

## Boundaries & Constraints

**Always:** per-Player instances, no statics; single owners (combo/rage events by their meters, `UltimateUsed` by AbilityRunner, invulnerability only via tracker windows); numbers in `*Config`/`*Definition` read live; SOs never written at runtime; Core free of Time/Physics/MonoBehaviour; BannedApiTests green; Ultimate input = existing `Ultimate` action (RT / R).

**Never:** Shock logic (payload goes to the stub IStatusApplier); ICombatIntensitySource/tracker (Epic 9); telemetry; Hero Swap carry-over; real art/audio; commits.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected |
|---|---|---|
| Combo hit | HitLanded src P0 | Count+1, window reset, ComboChanged |
| Foreign hit | HitLanded src P1 / enemy | ignored |
| Timeout | Fixed 2.0 s, no hit for 2.0 s | Count 0, ComboReset(Timeout) |
| Damage | PlayerDamaged P0, Count 18 | Count 0, ComboReset(Damage) |
| Avoided | HitAvoided on P0 | Count kept, RAGE unchanged |
| Shrinking | Count 30 | window = curve(30) ≥ 0.8 |
| RAGE gains | hit / StatusTick or ChainJump / 10th combo / PerfectDodged | +2% / +0.5% / +5% / +10%, clamp 1, RageFull once |
| Decay | full, no gain | 1.5 s grace then 8%/s → 0 at 14.0 s |
| Gate below full | RAGE 0.99, press | no fire, UltimateNotReady, press consumed |
| Gate full | RAGE 1, press | AbilityUsed+UltimateUsed, RAGE 0, UltimateStartup window = startup |

</frozen-after-approval>

## Code Map

- `Scripts/Core/Combat/CombatEvents.cs` -- add events; internal raisers.
- `Scripts/Core/Abilities/{AbilityRunner,AbilityLoadout,AbilityDefinition}.cs` -- UltimateUsed on Ultimate slot; loadout gets an Ultimate runner (not in buffered press order); allow AllEnemies in Validate.
- `Scripts/Core/Heroes/HeroDefinition.cs` -- `_rage` RageConfig.
- `Scripts/Core/Encounter/SessionToggles.cs` -- `ShrinkingComboWindow`, `RageDecay`.
- `Scripts/Runtime/Players/PlayerRoot.cs` -- owns/ticks PlayerCombatState (scaled dt).
- `Scripts/Runtime/Abilities/AbilityController.cs` -- Ultimate press → UltimateGate before other slots; AllEnemies resolver.
- `Scripts/Runtime/Feedback/{FeedbackRouter,SfxCue}.cs` + `FeedbackConfig` -- ultimate hitstop/shake/flash, not-ready cue.
- `Scripts/Runtime/UI/AbilityCooldownReadout.cs` -- delete (replaced by HUD).
- `Scripts/DevTools/DebugTogglePanel.cs` -- new toggles + Fill RAGE.
- `Scripts/Editor/SandboxBuilder.cs` -- Config_Combo, Config_Rage_Heracles, Ability_Heracles_WrathOfZeus, HUD template, menu rows, hint.

## Tasks & Acceptance

**Execution:**
- [x] `Scripts/Core/{Combo,Rage,Abilities,Combat}/*` -- counters, configs, gate, events.
- [x] `Scripts/Runtime/{Players,Abilities,Feedback,UI}/*` -- hosting, resolver, feedback, HUD.
- [x] `Scripts/DevTools/DebugTogglePanel.cs` + builder -- cheat, toggles, regenerate.
- [x] `Tests/EditMode/{ComboCounter,RageMeter,UltimateGate}Tests.cs` -- matrix rows.
- [x] `Tests/PlayMode/SandboxSmokeTest.cs` -- combo on dummies, damage reset, not-ready, Fill RAGE, Wrath hits all 3 dummies, RAGE 0, HUD panel bound.

**Acceptance Criteria:**
- Given Sandbox, when the Player lands hits, then the HUD panel shows the Combo with a draining bar, RAGE rising, HP and three cooldowns; readable at 640×400 and 1280×800.
- Given full RAGE, when RT / R is pressed, then every dummy takes Wrath of Zeus damage with heavy shake and RAGE drops to 0.

## Design Notes

Gains (any positive Add, even when clamped at 1) restart the decay grace, so RAGE stays full while fighting. Ultimate hits count for Combo but grant RAGE at `ultimateHitGain` (default 0) so an Ultimate cannot refill itself. Combo milestones fire when Count % every == 0.

## Verification

**Commands:**
- `unity run game -- -executeMethod GMR.Editor.SandboxBuilder.BuildAll` -- exit 0.
- `unity test game --mode EditMode` / `--mode PlayMode` -- all green.

## Implementation Notes

- Implemented directly by the build agent (a delegated subagent; checkpoints treated as pre-approved by the caller's detailed brief, as in M4a/M4b). Not committed (caller instruction).
- Events: ComboChanged/ComboReset (ComboCounter), RageChanged/RageFull (RageMeter), UltimateUsed (AbilityRunner, Ultimate slot), new UltimateNotReady (owner: UltimateGate).
- Core `PlayerCombatState` (Core/Players) owns Combo + RAGE, binds CombatEvents, hosted and ticked (scaled dt) by PlayerRoot; RageConfig comes from `HeroDefinition.rage`, ComboConfig from PlayerRoot.
- Gate order: busy/cooldown first (Waiting), then RAGE (NotReady), then consume + UltimateStartup i-frames + start (subscribers see RAGE 0). AbilityController queues a press while the Hero is busy so a full-RAGE press never expires in the buffer.
- RAGE gains snap to 1 within 1e-4 (50 x 2% float sum). Ultimate hits give UltimateHitGain (0) and no Combo-milestone bonus.
- HUD uses legacy uGUI Text (project pattern; TMP Essentials not imported). `AbilityCooldownReadout` deleted.
- Ultimate input unchanged: GameControls `Ultimate` = RT / R (no conflicts).
- ICombatIntensitySource not added: its tracker ships with telemetry (Epic 9).
- PlayMode: two early runs failed intermittently at pre-existing M4b asserts (Thunderbolt press / Club Strike press) and once at the new frame-counted Ultimate wait; the new wait is now scaled-time based and assert messages carry diagnostics; 7 consecutive green runs afterwards.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence / route |
|---|---|---|
| RAGE float sum never reaches 1 (50 x 0.02f) | medium | Patched: snap in RageMeter.Add + FiftyHits_FillRageExactly |
| Ultimate hits crossing a Combo milestone refill RAGE | medium | Patched: no milestone bonus for UltimateHit + test |
| Full-RAGE press lost when busy outlasts buffer / decays to NotReady | medium | Patched: AbilityController queue; gate checks busy before RAGE + tests |
| UltimateUsed raised before RAGE consumed / i-frames registered | low | Patched: consume + register before TryStart + test |
| Ultimate runner ticked twice in a frame | false | Tick loop runs before TryUltimate in Update; same pattern as buffered starts |
| No validation that Ultimate asset has the Ultimate tag | low | Patched: HeroDefinition.OnValidate warning |
| Missing RageConfig on Hero rebuild silently kept | low | Patched: LogError |
| Combo/RAGE Clear() never called | false | No restart path exists yet (Epic 7) |
| Ultimate flash lacks session toggle | low | Rejected: config toggle exists; comfort pass later |
| Ultimate shake per hit vs tooltip | low | Patched tooltip |
| HUD has no Ultimate slot vs loadout doc | low | Patched doc (RAGE meter is the Ultimate readout) |
| Hard-coded "RT / R" hint | low | Rejected: rebinding UI deferred by architecture |
| HUD title string allocated per frame | low | Patched: rebuilt on Hero change only |
| RAGE-decay toggle default from starting Hero config | low | Rejected: one Hero until Epic 8 |
| Charge ring animates on unscaled time | low | Rejected: cosmetic |
| N hitstop/shake requests per Ultimate | low | Rejected: same duration requests collapse (min scale); harmless |
| Smoke test leaks handlers / exact 3 count | false | Context dies with the scene; attackers are not IDamageable |
| Gap: Combo timeout assert vacuous | gap | Patched: real timeout step after x3 |
| Gap: session toggles through PlayerRoot | gap | Patched: smoke step + ClearOverrides test |
| Gap: queued full-RAGE press | gap | Patched: smoke step (press during Club Strike) |
| Gap: Ultimate hitstop | gap | Patched: UltimateHit_UsesUltimateHitstop |
| Gap: new SFX cues | gap | Patched: cues asserted in smoke |
| Gap: AllEnemies once-per-activation not pinned | low | Rejected: AbilityCast dedupe covered by existing tests |
| Missing tests for AllEnemiesHitResolver / Hero rebuild RageConfig | low | Deferred |

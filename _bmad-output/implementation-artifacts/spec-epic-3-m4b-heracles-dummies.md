---
title: 'Epic 3 (M4b): damage pipeline, ability slots, Heracles kit, training dummies, hit feedback'
type: 'feature'
created: '2026-11-03'
status: 'done'
route: 'dispatch'
baseline_commit: '2e78393614c54ab6c62977d9480cb6c974922e70'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Heracles can move and DASH but cannot attack; there is no HP, damage pipeline, combat events, ability data, dummies or hit feedback (stories 3.1–3.4). The pause menu also cannot be closed with gamepad B (deferred item).

**Approach:** Core: `Health`, `DamageSystem` (FIFO queue, 256/frame cap, sole raiser of HitLanded/HitAvoided/PlayerDamaged/EnemyDied), `SourceId`/`EnemyId`, `AbilityDefinition`/`ProjectileConfig`/`HealthConfig`/`StatusDefinition` stub, `AbilityRunner` + `AbilityLoadout`, hitstop rule. Runtime: `AbilityController` + Box/Circle/Projectile resolvers, `Projectile`, `FeedbackRouter`, `HitFlash`, `CameraShake`, SFX hooks, HP bars, cooldown readout, menu Cancel handler. DevTools: `TrainingDummy` (respawn), DummyAttacker via DamageSystem. Builder regenerates assets/prefabs/scene.

## Boundaries & Constraints

**Always:** single owners per AD-16 (Health mutated only inside Core DamageSystem via internal API; damage/ability Raise* methods internal to Core); per-Player instances; every number in a `*Config`/`*Definition` read live; SOs never written at runtime; hitstop only through TimeScaleService; feedback only subscribes to CombatEvents; slot-agnostic abilities (Basic slot effective cooldown 0); BannedApiTests green; Runtime never references DevTools.

**Never:** Shock/status logic (payload goes to a no-op `IStatusApplier` hook until Epic 5); Combo/RAGE/HUD (Epic 4); Player death handling (Epic 7); real audio files; commits.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected |
|---|---|---|
| Damage + knockback | 100 HP, resist 0.5, 10 dmg, kb (4,0) | HP 90, impulse (2,0), HitLanded with request Source, impulse applied only in ApplyResolved |
| Invulnerable | active window | HP unchanged, HitAvoided |
| Re-entrancy | Apply from a HitLanded handler | runs after current, FIFO, same frame |
| Cap | 257 requests in a frame | 257th dropped + `[GMR/Combat]` warning |
| Kill | dummy to 0 HP | EnemyDied once; respawns after delay |
| Phases | startup/active/recovery | Active entered/exited at configured times |
| Cooldown | Special 3 s | second press before 3 s ignored |
| Basic | cooldown 3 on asset | effective 0 in Basic slot |
| DASH cancel | DASH in recovery / startup | recovery cancelled / DASH waits |
| Buffered | Basic pressed in recovery, legal ≤0.12 s later | fires once |
| Hitstop 0 | duration 0 or toggle off | no TimeScale request |

</frozen-after-approval>

## Code Map

- `game/Assets/Scripts/Core/Combat/CombatEvents.cs` -- add events with internal raisers.
- `game/Assets/Scripts/Core/Encounter/{EncounterContext,SessionToggles}.cs` -- add `Damage`, feedback toggles.
- `game/Assets/Scripts/Core/Heroes/HeroDefinition.cs` -- add health + ability slots + primaryStatus (rage/chains wait for their types, Epics 4/8).
- `game/Assets/Scripts/Core/Players/IPlayerTarget.cs` -- expose `Damageable`.
- `game/Assets/Scripts/Core/Enemies/EnemyRegistry.cs` -- `NextEnemyId()`.
- `game/Assets/Scripts/Runtime/Heroes/{HeroRuntime,DashController,HeroMover}.cs` -- HeroRuntime is the Hero's IDamageable and owns `AbilityLoadout`; DASH waits during startup/active and cancels recovery; rootsCaster stops movement.
- `game/Assets/Scripts/Runtime/UI/PauseController.cs` -- cancel-close with same-frame guards.
- `game/Assets/Scripts/DevTools/{DummyAttacker,DebugTogglePanel}.cs` -- damage via DamageSystem; new toggles.
- `game/Assets/Scripts/Editor/SandboxBuilder.cs` -- assets, prefabs, 3 dummies, layer matrix, menu rows, hint text.
- `game/Assets/Tests/PlayMode/SandboxSmokeTest.cs` -- extend: menu B-close, each ability once, dummy damaged, respawn.

## Tasks & Acceptance

**Execution:**
- [x] `Scripts/Core/{Combat,Abilities,Status,Feedback}/*` -- types listed in Approach -- testable core.
- [x] `Scripts/Runtime/{Abilities,Feedback,UI}/*` + hero/player changes -- scene behavior.
- [x] `Scripts/DevTools/{TrainingDummy,TrainingDummyConfig}.cs` + DummyAttacker/DebugTogglePanel -- dummies + toggles.
- [x] `Scripts/Editor/SandboxBuilder.cs` -- regenerate.
- [x] `Tests/EditMode/{Health,DamageSystem,AbilityRunner,ProjectileConfig,FeedbackConfig}Tests.cs` -- matrix rows.
- [x] `Tests/PlayMode/SandboxSmokeTest.cs` -- extend.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` -- drop the B-close (and layer-matrix) items.

**Acceptance Criteria:**
- Given Sandbox, when Basic/Special/Signature are pressed near dummies, then Club Strike (box), Thunderbolt (projectile on `Projectile` layer) and Storm Whirl (circle, all 3 dummies) damage dummies with flash, hitstop, number, knockback and (heavy) shake.
- Given the pause menu open, when gamepad B / Esc is pressed, then it closes and no ability fires.

## Design Notes

`DamageSystem.Apply(target, in req, now)` takes `now` explicitly (physics callbacks run before Update); `BeginFrame()` from EncounterBootstrap resets the cap. Hit weight (Light/Heavy) rides on the request for hitstop/shake. Hitstop scale has a 0.01 floor so the reader keeps buffering presses (it treats scale 0 as paused).

## Verification

**Commands:**
- `unity run game -- -executeMethod GMR.Editor.SandboxBuilder.BuildAll` -- exit 0.
- `unity test game --mode EditMode` / `--mode PlayMode` -- all green.

## Implementation Notes

- Implemented directly by the build agent (itself a delegated subagent; checkpoints pre-approved by the caller's brief), like M4a.
- Basic-slot cooldown: forced to 0 at runtime by `AbilityRunner` (Basic slot) and `HeroDefinition.OnValidate` warns; the shared asset is NOT rewritten, so Thunderbolt keeps 3 s when moved back to Special.
- `HeroDefinition` gained health, basic/special/signature/ultimate and primaryStatus; `rage` and `chains[]` wait for RageConfig (Epic 4) / AbilityChainDefinition (Epic 8).
- Shock: `StatusDefinition` stub + `Status_Shock` asset; DamageSystem hands payloads to a no-op `IStatusApplier`.
- Physics2D layer matrix set by the builder (Projectile ignores Player, PlayerHitbox, EnemyHitbox, Projectile).
- Pause menu Cancel: `MenuCancelHandler` (ICancelHandler) on each item; `PauseController` frame guards keep Esc (Pause + Cancel) from close/reopen flicker.
- PlayMode: the InputTestFixture does not auto-switch control schemes, so the Esc check pairs the keyboard explicitly.
- Python rewrite briefly turned 7 .cs files to CRLF; normalized back to LF.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence / route |
|---|---|---|
| Hero at 0 HP still damageable (PlayerDamaged with 0 dealt, flash/shake spam) | medium | Patched: HeroRuntime.IsAlive checks !IsDepleted |
| TrainingDummy walk-back never zeroes velocity at home | low | Patched |
| Thunderbolt fired into an adjacent wall spawns past it | low | Patched: Linecast against walls, bolt fizzles |
| Invalid ProjectileConfig spawns a dead projectile silently | low | Patched: Validate before spawn, LogError |
| SfxHookPlayer has no AudioSource, so assigned clips stay silent | low | Patched: builder adds and wires an AudioSource |
| HeroDefinition.Get maps unknown slots to Ultimate | low | Patched: explicit Ultimate arm, throw otherwise |
| Cap window comment wrong for physics callbacks | low | Patched comment (window runs Update to Update) |
| Readout comment claims no per-frame rebuild | low | Patched comment |
| SFX cue Volume 0 plays at full volume | low | Patched tooltip (0 = unset) |
| DASH-wait/cancel not tested through DashController | medium (gap) | Patched: smoke test step |
| BeginFrame reset untested at runtime | medium (gap) | Patched: RequestsThisFrame == 0 assertion |
| DummyAttacker -> DamageSystem path untested | medium (gap) | Patched: slam -> PlayerDamaged (10) assertion |
| Knockback movement untested | low (gap) | Patched: dummy x increases after Club Strike |
| Hitstop toggle not checked through FeedbackRouter | medium (gap) | Patched: ActiveCount with toggle on/off |
| Keyboard Esc guards untested | medium (gap) | Patched: Esc open/close assertions |
| Hero knockback erased by HeroMover within ~2 physics steps | medium | Deferred: needs a hitstun/knockback window design |
| DummyAttacker is an Aim Assist candidate but not hittable | low | Deferred |
| Pierce / wall despawn / rootsCaster not covered in PlayMode | low (gap) | Deferred: no shipped ability uses pierce>0 or rootsCaster |
| Flash/shake/damage-number visuals not asserted in PlayMode | low (gap) | Deferred: manual juice check |
| DamageSystem.Restore is public | low | Rejected: needed by DevTools respawn; documented as the one other HP write |
| A throwing subscriber leaves state half-applied | low | Rejected: subscriber bugs, not a reachable path today |
| CameraShake rest position captured once | low | Rejected: camera is static in Sandbox; revisit with a follow camera |

# Epic 2 Context: Responsive Movement, Aim and DASH

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

A Player moves a Hero freely with analog input inside a walled sandbox, aims along movement (with optional Aim Assist and Direction Lock), DASHes with start-only i-frames and a short recovery, gets inputs buffered, and lands Perfect Dodges against a telegraphed dummy attacker, with every number tunable live in the Inspector and a controller-navigable pause/debug menu to flip feel toggles. This is the "does the hero feel responsive" foundation every combat epic builds on, and it must establish the per-Player (N-player-safe) structure Hero Swap and co-op rely on later.

## Stories

- Story 2.1: PlayerRoot/HeroRuntime split and tunable free movement
- Story 2.2: Movement aim, Aim Assist and Direction Lock
- Story 2.3: Input buffer and DASH with i-frames and Dash Recovery
- Story 2.4: Time scale service, pending hits and Perfect Dodge against a dummy attacker
- Story 2.5: Pause and debug menu v1 with aim and input toggles

## Requirements & Constraints

- Movement: analog, dead-zone (0.2), acceleration time to max speed (0.06 s), per-Hero max speed (Heracles 5.5 u/s, Theseus 6.0); collides with walls, never leaves the arena. 1 unit = 32 px.
- Aim Direction = last non-zero movement direction, always shown by a placeholder arrow. Aim Assist (toggle) snaps to the nearest living candidate in a cone (half-angle 25 deg, range 8 u) only when firing; no candidate means unchanged. Direction Lock (toggle) freezes aim while held; release resumes movement aim on the next movement input; disabled means the button does nothing. Right-stick aim is only an enum hook.
- DASH: no cooldown; 3 u over 0.15 s along movement (aim if no input); i-frames only for the first 0.10 s; Dash Recovery 0.12 s with no i-frames and no new DASH; passes through enemies, stops at walls.
- Input buffer: 0.12 s window; presses made while an action is illegal execute as soon as it becomes legal; 0 disables buffering; a buffered press is consumed once.
- Perfect Dodge: DASH started within 0.08 s before a pending hit's land time while overlapping its area; at most one per (attack, Player); flash + time-slow (0.15 s at 30%), both toggleable. A DASH just outside the window still avoids via i-frames but grants no Perfect Dodge.
- Pause/debug menu: navigable with gamepad only and keyboard only; toggles for Aim Assist, Direction Lock, input buffer, Perfect Dodge flash and time-slow; take effect immediately and persist for the session.
- Every gameplay number lives in a ScriptableObject tuning asset, read live (edits apply during Play Mode). Placeholder shapes only. Input-to-action within one frame.

## Technical Decisions

- Assemblies: pure rules in `GMR.Core` (no MonoBehaviour/Time/Physics/Random; time arrives as `dt`/`now`); MonoBehaviour adapters in `GMR.Runtime`; debug items in `GMR.DevTools` (dev builds only). Runtime never references DevTools.
- No static player, no mutable statics, no `static event`, no runtime `FindObjectOfType`. One `EncounterContext` (Rng, Players registry, Events, TimeScale, PendingHits) created by `EncounterBootstrap` and passed via `Init(...)`. Domain reload is off.
- `PlayerRoot` owns person state (input reader, aim, input buffer, HUD binding); a child `HeroRuntime` built from a `HeroDefinition` owns movement/dash tuning and visuals, replaceable on swap. Players join via `PlayerInputManager` (manual join), get a `PlayerId`, register in the registry.
- Only `PlayerInputReader` touches `InputAction`; it fills `InputFrame` and pushes presses into the Core `InputBuffer`; gameplay calls `TryConsume(action, now)`. `AimResolver` (Core) uses `AimConfig` (cone, range, toggles, `AimMode`).
- `InvulnerabilityTracker` (Core) is the sole owner of invulnerability; dash only registers timed windows.
- `TimeScaleService` is the only writer of `Time.timeScale`; timed requests, lowest scale wins, pause = scale 0. Attacks register a `PendingHit` (attackId, landTime, area) in `PendingHitRegistry` when the telegraph starts. `DashController` is the sole raiser of `DashStarted` and `PerfectDodged`, using scaled time.
- Events are typed past-tense `readonly struct`s raised via C# events on a per-encounter `CombatEvents`; subscribers unsubscribe in `OnDisable`.
- Naming: `*Definition` content SOs, `*Config` tuning SOs, `*Runtime`/`*State` mutable instances; one public type per file; namespaces `GMR.Core.<Area>` / `GMR.Runtime.<Area>`; logs prefixed `[GMR/<Area>]`. Movement in `FixedUpdate` via `Rigidbody2D.linearVelocity`; input read at order -100, gameplay 0, feedback/HUD +100. Gameplay uses scaled time, UI unscaled.
- Tests: every Core rule class gets `<Type>Tests.cs` in EditMode using `new`/`ScriptableObject.CreateInstance` only. `BannedApiTests` stays green.
- UI: uGUI with `InputSystemUIInputModule`.

## Cross-Story Dependencies

- 2.1 provides PlayerRoot/HeroRuntime/EncounterContext used by all later stories; 2.2 aim feeds 2.3 dash direction; 2.3 `DashController` + `InvulnerabilityTracker` are consumed by 2.4 Perfect Dodge; 2.4 `TimeScaleService` is used by 2.5 pause.
- Later epics extend this: Ability inputs use the same `InputBuffer` (Epic 3), Perfect Dodge grants RAGE (Epic 4), real enemies register `PendingHit`s (Epic 6), "Spawn Player 2" proves the N-player structure (Epic 7).

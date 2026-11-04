- source_spec: `_bmad-output/implementation-artifacts/spec-epic-2-m4a-movement-aim-dash.md`
  summary: Verify per-Player device pairing (two PlayerInputs get separate action copies) when Spawn Player 2 lands.
  evidence: Smoke test joins one Player only; PlayerInput.actions duplication for later players is unverified in-repo.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-2-m4a-movement-aim-dash.md`
  summary: Keep enemy pass-through until the Hero no longer overlaps an enemy after a DASH; make DASH distance exact per fixed step.
  evidence: Physics depenetration pops the Hero; travel is speed x whole fixed steps (about +/-0.4 u).
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-2-m4a-movement-aim-dash.md`
  summary: Pause menu: add "reset toggles to config", PlayMode check that each toggle changes its reader.
  evidence: Review findings (closing on UI Cancel / B was done in M4b).
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-3-m4b-heracles-dummies.md`
  summary: Hero knockback is erased by HeroMover within about two physics steps; add a short knockback/hitstun window.
  evidence: HeroMover sets linearVelocity every FixedUpdate (decel 0.06 s), so DummyAttacker knockback on the Hero is barely visible.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-3-m4b-heracles-dummies.md`
  summary: DummyAttacker is an Aim Assist candidate but not damageable; exclude non-damageables from ability assist or make it hittable.
  evidence: Club Strike / Thunderbolt can snap onto it and do nothing; the bolt passes through it.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-3-m4b-heracles-dummies.md`
  summary: PlayMode coverage for projectile pierce > 0, wall despawn and rootsCaster.
  evidence: No shipped ability uses them yet; only Core-level tests exist.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-3-m4b-heracles-dummies.md`
  summary: PlayMode assertions for hit flash, camera shake and damage-number popups.
  evidence: Covered by the manual juice check only; FeedbackRouter hitstop toggle is tested.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-4-m5a-combo-rage-ultimate.md`
  summary: Investigate intermittent SandboxSmokeTest failures where a gamepad ability press (Thunderbolt Y, Club Strike X) does not start the ability.
  evidence: Seen in 2 of ~11 batchmode runs at pre-existing M4b asserts; hitstop runs on wall-clock unscaled time while the test fixes scaled time with captureFramerate, so frame-counted waits vary with machine speed. Assert messages now print a Diag() line.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-4-m5a-combo-rage-ultimate.md`
  summary: EditMode tests for AllEnemiesHitResolver (dead / non-damageable enemies skipped, registry changes mid-hit) and for RageConfig swap on Hero rebuild.
  evidence: Covered only indirectly by the PlayMode Wrath of Zeus step.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-4-m5a-combo-rage-ultimate.md`
  summary: Manual readability check of the per-Player HUD at 640x400 and 1280x800 (Story 4.3 is a manual-test story), plus a session toggle for the Ultimate screen flash.
  evidence: Batchmode cannot screenshot the overlay canvas; layout sized for the 640x400 reference.

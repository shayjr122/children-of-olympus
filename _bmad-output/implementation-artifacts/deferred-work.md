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

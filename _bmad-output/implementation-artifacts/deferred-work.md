- source_spec: `_bmad-output/implementation-artifacts/spec-epic-2-m4a-movement-aim-dash.md`
  summary: Verify per-Player device pairing (two PlayerInputs get separate action copies) when Spawn Player 2 lands.
  evidence: Smoke test joins one Player only; PlayerInput.actions duplication for later players is unverified in-repo.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-2-m4a-movement-aim-dash.md`
  summary: Keep enemy pass-through until the Hero no longer overlaps an enemy after a DASH; make DASH distance exact per fixed step.
  evidence: Physics depenetration pops the Hero; travel is speed x whole fixed steps (about +/-0.4 u).
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-2-m4a-movement-aim-dash.md`
  summary: Configure the Physics2D layer matrix when hitbox/projectile layers get used (Epic 3).
  evidence: Builder adds layers but every pair collides by default.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-2-m4a-movement-aim-dash.md`
  summary: Pause menu: close on UI Cancel (B), add "reset toggles to config", PlayMode check that each toggle changes its reader.
  evidence: Review findings; only Start/Select/Resume close the menu today.

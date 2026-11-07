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
  summary: EditMode tests for AllEnemiesHitResolver (dead / non-damageable enemies skipped, registry changes mid-hit) and for RageConfig swap on Hero rebuild.
  evidence: Covered only indirectly by the PlayMode Wrath of Zeus step.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-4-m5a-combo-rage-ultimate.md`
  summary: Manual readability check of the per-Player HUD at 640x400 and 1280x800 (Story 4.3 is a manual-test story), plus a session toggle for the Ultimate screen flash.
  evidence: Batchmode cannot screenshot the overlay canvas; layout sized for the 640x400 reference.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-5-m5b-status-shock-wet.md`
  summary: Builder re-applies M5b upgrades on every BuildAll (Wrath `_statusMaxPotency` true; Status_Shock defaults when its behaviours list is empty); add a one-time migration marker.
  evidence: A designer turning max potency off or emptying Shock behaviours is overwritten by the next rebuild.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-5-m5b-status-shock-wet.md`
  summary: Hero statuses: clear on Hero death/rebuild (raise StatusExpired), StatusVisuals on the Hero prefab, unregister on disable.
  evidence: No status applies to Heroes yet; HeroRuntime only registers/unregisters its container.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-5-m5b-status-shock-wet.md`
  summary: Performance for 15 enemies + full chains on Steam Deck: pool ChainRun/lightning segment GameObjects and profile Wrath of Zeus at max potency.
  evidence: Each Shock application allocates a ChainRun; each jump creates 5-10 SpriteRenderer GameObjects.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-5-m5b-status-shock-wet.md`
  summary: Chain ends silently when a queued jump is avoided (i-frames), its target died first, or the 256/frame cap drops it; consider re-picking from the last target.
  evidence: Dummies have no post-hit i-frames, so not reachable in the Sandbox today; enemies (Epic 6) may have spawn invulnerability.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-5-m5b-status-shock-wet.md`
  summary: ConductedShock is raised inside the fold (before HP); add the dealt amount / kill flag if Epic 9 telemetry needs it.
  evidence: Event carries source, target, tag, multiplier, hit id only.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-5-m5b-status-shock-wet.md`
  summary: PlayMode checks for a Wet dummy losing its statuses on break, the F2 key, and the CONDUCTED! popup itself (only its SFX cue is asserted).
  evidence: Clear() primitive and the D-pad path are tested; manual check covers the rest.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-5-m5b-status-shock-wet.md`
  summary: Fold order inside StatusBehaviour.ModifyIncoming (flat before multiplier across behaviours) is a convention, not enforced; split flat/multiplier contributions when a second modifying behaviour lands.
  evidence: Only interaction rules modify damage today, and the rule fold applies flat chain adds before multipliers.

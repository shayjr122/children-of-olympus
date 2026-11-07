# Epic 3 Context: Heracles Fights Training Dummies

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

A Player uses Heracles' three data-driven abilities (Club Strike, Thunderbolt, Storm Whirl) against training dummies in the Sandbox, with a single damage pipeline, typed combat events and full placeholder hit feedback (flash, hitstop, knockback, shake, damage numbers, SFX hooks). It proves that kits are assembled from assets without code and gives every later combat epic (Combo/RAGE, statuses, enemies) one consistent place where damage and its events happen.

## Stories

- Story 3.1: Health, damage pipeline, combat events and training dummies
- Story 3.2: Ability slots, AbilityRunner and Thrust hit resolver (Club Strike)
- Story 3.3: Spin and Shot resolvers and the full Heracles kit
- Story 3.4: FeedbackRouter: flash, hitstop, knockback, shake, damage numbers, SFX hooks

## Requirements & Constraints

- Ability assets define attack type (Spin/Shot/Thrust, a tag), hit shape, damage, range/area, projectile parameters, knockback, cooldown, startup/active/recovery timings, optional status payload, and whether it roots the caster. Slots are agnostic: any ability works in any slot after re-entering Play Mode with no code change; the Basic slot forces cooldown 0.
- Spin hits every enemy within its radius around the Hero; Shot spawns a projectile along the Aim Direction that hits the first enemy; Thrust hits a short narrow box in front of the Hero along the Aim Direction.
- Heracles: Basic Club Strike (Thrust, close, heavy), Special Thunderbolt (Shot, applies Shock, 3 s cooldown), Signature Storm Whirl (Spin, high damage, applies Shock, 8 s cooldown). Shock itself is built in Epic 5; here only the payload hook exists. Reference: Heracles Basic about 10 damage.
- Ability inputs are buffered like DASH (buffer 0 disables). DASH cancels an ability only during its recovery. The Hero keeps moving during abilities unless the ability roots the caster. Special/Signature cooldown remaining is shown on screen (minimal readout).
- Hit feedback: target flash, hitstop (light 0.03 s / heavy 0.08 s; 0 disables), knockback, floating damage numbers (toggleable in the pause menu), camera shake on heavy hits, SFX hooks for hit, DASH and Perfect Dodge (silent allowed). Player damage: red flash plus 0.5 s post-hit invulnerability. Every intensity is tunable and individually toggleable.
- Training dummies take damage, show HP, get knocked back and respawn after a configurable delay.
- Every number lives in ScriptableObject tuning assets read live; placeholder shapes only; N-player safe; seeded RNG only.

## Technical Decisions

- All damage goes through `DamageSystem.Apply(IDamageable target, in DamageRequest req)`: check invulnerability, fold modifiers (statuses come later), apply HP, emit `HitLanded` or `PlayerDamaged` / `HitAvoided`; `EnemyDied` on kill. `Apply` enqueues; requests raised during resolution run FIFO in the same frame, capped at 256 per frame (the rest dropped with a `[GMR/Combat]` warning).
- Single owners: HP lives in Core `Health` (one per damageable), mutated only by `DamageSystem`. Knockback impulse = `req.Knockback x (1 - KnockbackResistance)` computed into `DamageResult`; only the target's `ApplyResolved` applies it (Rigidbody2D impulse). Invulnerability only via `InvulnerabilityTracker` windows. `AbilityUsed` raised only by `AbilityRunner`. `HitLanded`/`HitAvoided`/`PlayerDamaged`/`EnemyDied` only by `DamageSystem`.
- Key types: `DamageTags` [Flags] (Physical, Shock, Water, StatusTick, Ultimate, ChainJump); `DamageRequest` (Source, Amount, Tags, Knockback, Status payload, HitId); `DamageResult` (Dealt, Killed, Avoided, KnockbackImpulse, Tags, Source); `IDamageable` (Id, IsAlive, IsInvulnerable, Health, KnockbackResistance, ApplyResolved); `SourceId` tagged union of `PlayerId`/`EnemyId`.
- `AbilityDefinition` (AttackType tag, HitShape Circle/Projectile/Box/..., timings, cooldown, damage, tags, knockback, rootsCaster, optional status payload, optional `ProjectileConfig`); `HeroDefinition` slots `basic`, `special`, `signature`, `ultimate`, `dash`, `move`, `rage`, `chains[]`, `primaryStatus`. Variants are SO subclasses; no `[SerializeReference]`. One plain-C# `AbilityRunner` state machine per slot; one hit resolver per HitShape in Runtime.
- Hit detection: melee/area via `Physics2D.Overlap*` with `ContactFilter2D` + reused list and layer masks; projectiles via trigger colliders on the `Projectile` layer. Layers: Player, Enemy, PlayerHitbox, EnemyHitbox, Projectile, Wall.
- `FeedbackRouter` subscribes to `CombatEvents` only and maps events to feedback via `FeedbackConfig`; gameplay never calls feedback directly. Hitstop requests go through `TimeScaleService` (only writer of time scale). Cosmetic randomness (shake jitter) uses the `Feedback` RNG stream.
- Events are typed past-tense readonly structs on per-encounter `CombatEvents`; subscribers unsubscribe in `OnDisable`. Core has no MonoBehaviour/Time/Physics/Random; Runtime never references DevTools. Every Core rule class gets `<Type>Tests.cs` EditMode tests. Update order: input -100, gameplay 0, feedback/HUD +100.

## Cross-Story Dependencies

- 3.1 (Health, DamageSystem, events, dummies) is used by 3.2/3.3 resolvers and consumed by 3.4 feedback.
- 3.2 `AbilityRunner` + slots + Thrust resolver is extended by 3.3 (Spin, Shot, projectile).
- Builds on Epic 2: `InputBuffer`, `DashController`, `InvulnerabilityTracker`, `TimeScaleService`, `EncounterContext`, pause/debug menu, DummyAttacker (source of `PlayerDamaged`).
- Later: Combo/RAGE (Epic 4) consume `HitLanded`/`AbilityUsed`; Shock (Epic 5) fills the status payload hook; HUD (4.3) replaces the cooldown readout.

# Epic 5 Context: Shock, Wet and Conducted Shock

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Heracles' attacks chain lightning between nearby enemies, and Wet (applied via a DevTools key in this epic, since Theseus arrives in Epic 8) visibly amplifies that chain into Conducted Shock. Everything is built on a generic, data-driven Status System so future statuses (Burn, Curse, Slow, Rhythm) are added as assets or single behaviour classes without touching the core. This makes the Wet + Shock hero-synergy idea visible early and underpins the later synergy gate (SM-3).

## Stories

- Story 5.1: Generic Status System core (definitions, container, ticker, visuals)
- Story 5.2: Shock with chain lightning on Heracles' abilities
- Story 5.3: Wet status and the data-driven Wet-conducts-Shock rule

## Requirements & Constraints

- Any damageable entity (enemies now, Heroes later) can carry statuses. A status asset defines duration, max stacks, refresh rule, behaviours, tint and icon. Statuses are visible on the entity (tint + icon above it). Every application records its source Player.
- New status with existing behaviours = new asset only. New behaviour type = one new class, no edits to `StatusContainer` (proved by a test-only `StatusBehaviour` subclass in the test assembly).
- Shock: applying Shock deals Shock damage and chains to up to N other enemies within radius R, with damage falloff per jump; each enemy is hit at most once per chain; a visible lightning line connects each jump. Defaults: N 3, R 3.5 u, falloff x0.7 per jump.
- Thunderbolt and Storm Whirl apply Shock on hit; Wrath of Zeus applies Shock at maximum chain count.
- Wet: blue tint, 6 s duration, re-applying refreshes duration, deals no damage.
- Conducted Shock (approved): Shock damage (initial hit or chain jump) on a Wet target is x2.0; a chain jump leaving a Wet target gets +1 jump and +1.5 u radius; distinct blue-white arc. Wet is NOT consumed by Shock by default (`consumeTargetStatus` false), toggleable in data. Telemetry counting of Conducted Shock lands in Epic 9; the event must carry what it needs (source Player, target).
- Combo/RAGE (approved, already wired in M5a): status ticks and chain jumps each count +1 Combo and fill RAGE at the lower status rate (0.5%), not the direct-hit rate. A chain hitting 4 enemies raises the applying Player's Combo by 4.
- Determinism (approved): chain tie-breaks use a seeded `RunRng` stream (`Status`); identical across runs with the same seed. No `UnityEngine.Random`.
- Performance target: 60 FPS on Steam Deck with 15 enemies and full Shock chains on screen.
- All numbers in ScriptableObject assets, read live; N-player safe (no statics); `BannedApiTests` green; Core rules EditMode-tested.

## Technical Decisions

- `StatusDefinition` (id, duration, `maxStacks`, `RefreshRule`, tint/icon, `behaviours[]`). `StatusBehaviour` is an abstract SO with `OnApply`/`OnTick`/`OnExpire`/`ModifyIncoming`; variants are SO subclasses, never `[SerializeReference]`.
- `StatusContainer` is Core, one per damageable (`IDamageable.Statuses`): stores instances with `SourceId`, ticks via `Tick(dt)`, exposes `IncomingModifiers(tags)`, resolves interaction rules from a `StatusRuleSet` asset. It is the sole raiser of `StatusApplied` and `ConductedShock`.
- `StatusTicker` (Runtime, `Update`) is the sole ticker: each registered container exactly once per frame.
- `StatusInteractionRule` holds `whenApplied` or `whenDamageTagged`, `targetHas`, multiplier, bonus jumps, bonus radius, `consumeTargetStatus`, `conductedEventTag`. Wet logic must never be special-cased in Shock code; it lives in `Rule_WetConductsShock`.
- All damage (hits, ticks, chain jumps) goes through `DamageSystem.Apply`: invulnerability check, then modifier fold, then HP, then events. Fold order: status modifiers (container order) then archetype modifiers; flat adds before multipliers. `ChainParams` (MaxJumps, Radius, Falloff) is mutated only inside the fold.
- `ChainLightningBehaviour` queries `EnemyRegistry`, emits `ChainJump`-tagged `DamageRequest`s with the original Player's `SourceId`, tracks visited enemies per chain. Re-entrancy: requests raised during resolution are enqueued and run FIFO in the same frame (cap 256/frame), never recursive.
- Ability status payload is the existing optional `StatusPayload` (def, stacks) on `AbilityDefinition`. `DamageTags` already includes `Shock`, `Water`, `StatusTick`, `ChainJump`.
- Asset naming: `Status_Shock`, `Status_Wet`, `Rule_WetConductsShock`; data under `Data/Statuses/` and `Data/Rules/`. Namespace area `Status` (`GMR.Core.Status`, `GMR.Runtime.Status`). Debug "Apply Wet to all" key lives in `GMR.DevTools`.

## UX & Interaction Patterns

- Status readability: tint + icon above the entity; Wet is blue. Placeholder shapes only.
- Lightning line per jump; Conducted jumps use a brighter blue-white arc so the synergy reads at a glance.

## Cross-Story Dependencies

- 5.2 and 5.3 build on 5.1 (container, definitions, ticker, rule set).
- 5.3 relies on 5.2's chain and `ChainParams` fold for bonus jumps/radius.
- Consumes Epic 3 abilities and `DamageSystem`, Epic 4 Combo/RAGE status-rate wiring and the Wrath of Zeus payload hook.
- Epic 8 (Theseus' Riptide Bolt / Maelstrom) applies Wet in play; Epic 9 records Conducted Shock telemetry; Epic 6/7 enemies register in `EnemyRegistry` and become chain targets.

# Adversarial Review — ARCHITECTURE-SPINE.md

Verdict: the spine is strong on RNG, assembly and time-scale ownership, but leaves the core combat mutation paths (HP, knockback, i-frames, event raising, status ticking) under-owned, so two ADs-compliant stories will collide there.

## Findings (incompatible-build pairs)

### F1. Two owners of HP and who writes it (AD-6 vs AD-9 vs IDamageable)
- AD-6 says `PlayerCombatState` owns "HP fraction". AD-9 says `DamageSystem` applies HP. `IDamageable.ApplyResolved(DamageResult)` is the write hook, but nothing says who stores the HP number, in what unit (absolute vs fraction), or whether enemies keep HP on `EnemyBrain` or a separate component.
- Story A (Player) stores HP as a 0-1 fraction in `PlayerCombatState` and implements `ApplyResolved` by subtracting `Amount / maxHp`. Story B (Enemy) stores absolute HP on `EnemyBrain` with `DamageResult.Amount` absolute. Both obey every AD. Swap carry (`carryHpFraction`) then multiplies by the wrong max, and `HeroRuntime` could also keep max HP in `HeroDefinition`, making max HP owned by the hero while current HP is owned by the player.
- `DamageResult` is never defined (fields, killing-blow flag, overkill, post-modifier amount).

### F2. Knockback has no owner (AD-8, AD-9, AD-13)
- `DamageRequest.Knockback` is a Vector2, and `ArchetypeDefinition` has `knockback resistance`, but no AD says who applies it. Story A (hero movement) applies it in `ApplyResolved` via `Rigidbody2D.AddForce`. Story B (EnemyBrain) ignores it and sets a Stunned state with its own knockback velocity. Story C (`ReverseKnockbackBonus`) rewrites the request vector before DamageSystem, while resistance is applied by neither.
- Impulse vs velocity vs displacement over time, and whether it is world-space or direction-times-magnitude, are unspecified. Is it a modifier in step 2? Does it apply when i-frames block damage? Does `rootsCaster` interact?

### F3. I-frames: three sources, no decider (AD-9 step 1, AD-12, AD-13)
- Step 1 lists "i-frames, post-hit invulnerability, spawn telegraph" but not who owns the flag. `DashController` sets i-frames on its timeline, `PlayerCombatState` owns post-hit invuln, `EnemyBrain` owns spawn invuln. `IDamageable` has only `IsAlive`, no `IsInvulnerable`, so DamageSystem cannot ask. Pair: dash story stores `invulnUntil` on `DashController`; combat story stores it on `PlayerCombatState`; DamageSystem can read only one.
- Perfect Dodge (AD-12) is detected at dash start from `PendingHit`, but `HitAvoided` is raised by DamageSystem at hit time. Which event drives Combo/RAGE for a dodge, and who raises `PerfectDodged` (registry, dash controller or DamageSystem)? Two stories each raise it, giving double RAGE.
- Unit mismatch: AD-12 compares `landTime` to scaled time, but `TimeScaleService` slow-mo changes the perfect window in real seconds. Which clock `landTime` and `perfectWindow` use is not stated.

### F4. Event raising and tick ownership (AD-10, AD-11)
- "Only the owning system raises an event" lists no owner for most events: `StatusApplied`, `ConductedShock` (container vs behaviour vs rule resolver), `AbilityUsed` (runner vs hero), `DashStarted`, `EnemyDied` (DamageSystem on kill vs EnemyBrain on Dead state), `ChainTriggered`.
- Who calls `StatusContainer.Tick(dt)` is unspecified. The Update Order table covers combo/RAGE ticking but not statuses. Core containers need `dt` pushed by a shell; one story ticks in `Update` per entity, another from a central `StatusSystem`, and a dead or pooled entity ticks twice or never. `StatusContainer.Apply` takes `now` while `Tick` takes `dt`: two clocks.
- `StatusTick` damage and `ChainLightning` run inside Core and call `DamageSystem.Apply` re-entrantly mid-event dispatch. Combo subscribers fire on `HitLanded` from a tick, and Shock-from-Wet via `whenDamageTagged` can recurse. No re-entrancy rule or queue is given. `SourceId` of status ticks is stated only for chains.
- C# `event Action<T>` fires in subscription order. Combo `OnHit` and RAGE `Add` ordering vs HUD reads is undefined (the `[DefaultExecutionOrder]` row does not govern event dispatch).

### F5. Shared data shapes with no definition (clashing structs)
- `ChainParams`, `StatusPayload`, `DamageResult`, `StatusApplyContext`, `HitId`, `EntityId` vs `PlayerId`/`EnemyId`/`SourceId` are referenced but not defined. `ChainParams` is `ref`-modified by `IDamageModifier`, but also used by `StatusInteractionRule` (bonus jumps/radius) and by `ChainBonus` (AD-8). Three places mutate chain params (modifier, rule, chain bonus) with no stated order.
- `RageSource` and `RageConfig` are not enumerated: M5a defines the source list while the FR list lives in the PRD.
- AD-8: `AbilityDefinition` carries damage, `DamageTags`, status payload, while `ChainBonus` and `DamageModifier` can also change them. Two paths to add Shock to a hit: the ability's `StatusPayload` vs a `StatusBehaviour`. Both are legal.
- `ArchetypeDefinition.damageModifiers[]` and status `ModifyIncoming` both fold in step 2, but the fold order (additive vs multiplicative; modifier from Wet x2 vs armor) is unspecified; results depend on order.

### F6. Hero swap ownership (AD-6)
- Swap destroys `HeroRuntime`, but statuses (on the player entity, via `IDamageable.Statuses`), `AbilityRunner` cooldown state, pending chain state and in-flight projectiles are all unowned: StatusContainer lives with "each damageable entity" (PlayerRoot or HeroRuntime?). In-flight projectiles reference the destroyed hero as source.
- Ultimate `IsFull` gate lives in `RageMeter` (player) but the ability definition lives on the hero; who calls `Consume()`: runner (hero) or PlayerCombatState?

## Over-engineering risk (2-person, 3 h/week)
- Named RNG stream tree with stateless `Derive(name, index)` for seven streams plus hand-written PCG32, FNV, SplitMix: a single seeded `System.Random`-like PCG plus per-system instances would suffice for the prototype.
- Six asmdefs including Debug and Editor on day one; Core must be Unity-free enough for tests yet uses `ScriptableObject`, which blurs the purity story the paradigm claims.
- Abstract SO subclass hierarchy for statuses, chain bonuses, damage modifiers, interaction rules and a `StatusRuleSet` for exactly two statuses (Wet, Shock); AD-10 targets Burn/Curse/Slow that are future scope.
- `CombatIntensityTracker`, `ICombatIntensitySource` with `PerPlayer`, and `IntensityAggregation` built in M5 only to log to telemetry.
- Per-player everything plus a `ITargetSelector` and PlayerRegistry abstraction while co-op is "post-launch" and a debug "Spawn Player 2" is the only test. AD-4 cost is paid on every system.
- 20+ event types and 14 ADs for 4 milestones; a `BannedApiTests` text-scan that includes static-field pattern matching will be brittle (false positives on `static readonly` mutable collections).
- Live-read config (not cached) is at odds with the pure-Core rule: Core classes taking `ComboConfig` SO read it each call, a hidden coupling to Unity object lifetime.

## Suggested AD fixes
1. Add AD: `Health` is one Core class per damageable (absolute HP, max from definition), owned by the entity root; `PlayerCombatState` holds no HP; swap carry copies the fraction.
2. Add AD: `DamageSystem` computes a `DamageResult` including a final `KnockbackImpulse`; the target's `ApplyResolved` is the sole place knockback is applied, with resistance folded in step 2.
3. Add `bool IsInvulnerable(in DamageRequest)` to `IDamageable`, implemented only on the entity root by OR-ing timer flags registered through one `InvulnerabilityTracker`; `DashController` only registers.
4. Add an event-owner table (event, sole raiser) and a rule that `Apply` re-entry is queued and drained FIFO after the current dispatch; assign `StatusContainer.Tick` to one `CombatTicker` in `Update`.
5. Define `DamageResult`, `ChainParams`, `StatusPayload`, `EntityId` in Key Signatures and fix the step-2 fold order (flat, then multipliers, in asset-list order).

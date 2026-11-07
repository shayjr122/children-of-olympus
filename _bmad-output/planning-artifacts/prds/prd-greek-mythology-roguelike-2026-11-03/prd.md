---
title: Greek Mythology Roguelike — Combat Prototype
status: final
created: 2026-11-03
updated: 2026-11-03
scope: Combat prototype only (GDD §17.3 step 2; kickoff milestones 2–6)
sources:
  - GDD.md (§4, §5.1–5.5, §7, §10.3, §16, §17.3)
  - prompts/dev-kickoff-prompt.md
mode: headless (orchestrator); assumptions tagged inline and indexed in §10
---

# PRD: Greek Mythology Roguelike — Combat Prototype

## 0. Document Purpose

This PRD defines the **combat prototype** for Greek Mythology Roguelike: one arena room, two playable heroes, three enemy archetypes, and the combat systems (movement, DASH, ability slots, combo, RAGE, statuses, aim) needed to answer one question: **is the combat fun?** (GDD §17.3: "if combat isn't fun here, nothing else will save it"). It is written for the two-person team, for the downstream architecture document, and for the epics/stories breakdown (kickoff milestone 2). The GDD (Hebrew) is the source of truth for the full game. This PRD narrows it to the prototype and records where the prototype deliberately departs from it (two heroes and the Wet status, both user decisions). Vocabulary is fixed by the Glossary (§3). Features group the FRs, which are numbered globally (FR-1…FR-N). Inferred decisions are tagged `[ASSUMPTION]` and indexed in §10. Starting tuning values and technical notes live in `addendum.md`.

## 1. Vision

A fast, responsive, combo-driven fight in a single locked arena, in the spirit of Wizard of Legend. The player moves freely, dashes through telegraphed attacks, chains three abilities and builds a **Combo** that pays out in **RAGE**, which is spent on a hero-unique **Ultimate**. Playing well, landing hits without getting hit, makes the player stronger inside the fight.

The prototype also tests the GDD's core synergy idea (pillar 3) at the smallest possible scale: **Heracles** chains lightning (⚡ Shock) between enemies, **Theseus** soaks them (💧 Wet), and **Wet amplifies Shock**. Mixing both heroes against the same enemies should produce a visible "whoa" moment, the kind of discovered combination the full game depends on.

The prototype is built so the game's long-term technical bets hold from day one: all randomness comes from one **Run Seed** (Daily Run), combat logic supports **N players** (future co-op), input is controller-first through the Unity Input System (Steam Deck), and every tuning value lives in ScriptableObjects so designers tune without code. Art is placeholder shapes only. The goal is feel, not looks.

## 2. Target User

### 2.1 Jobs To Be Done
- **Team (designer-developers, ~3 h/week total):** find out cheaply and early whether the combat loop is fun before investing in art, content and the Crete vertical slice.
- **Team as tuners:** change any combat number (dash length, combo window, RAGE fill, enemy HP) in the Inspector and replay within seconds, without code changes.
- **Playtesters (friends, action/roguelike players):** pick up a controller, understand the fight within a minute, and say honestly whether they want "one more run".
- **Future team (architecture):** ensure the prototype's code becomes the foundation for the vertical slice, not a throwaway, especially seeded RNG, N-player logic and the generic status system.

### 2.2 Non-Users (prototype)
- Public players, streamers or Steam audiences. Nothing here is shipped or shown publicly.
- Online co-op players. Only the *architecture* supports N players, and no second-player UI or networking is built.

### 2.3 Key User Journeys

- **UJ-1. Shay plays the first fight with Heracles.** Shay launches the prototype scene with a controller on PC or Steam Deck, picks Heracles on the Hero Select screen, and the arena locks. A Minotaur-man charges with a visible wind-up. Shay dashes through it at the last moment (Perfect Dodge flash, RAGE ticks up), turns and strings Basic → Signature → Special while the Combo counter climbs past 15. RAGE fills, Shay fires the Ultimate, and lightning chains across the last wave. The room clears, the HUD shows clear time, max combo and the Run Seed, and Shay presses Restart. **Edge case:** Shay gets hit at combo 18. The counter resets visibly, and RAGE keeps what was earned but keeps decaying.
- **UJ-2. Shay tests the Wet + Shock synergy.** Shay starts with Theseus, soaks a group of archers and the bronze automaton with Riptide Bolt and Maelstrom (blue Wet tint), presses the Hero Swap button, and as Heracles hits the wet group with Thunderbolt: the Shock chain jumps farther, deals boosted damage and shows a distinct "conducted" effect. The tank goes down in roughly half the usual time.
- **UJ-3. A playtester gives the "is it fun" verdict.** A friend with no briefing plays three runs per hero. The prototype logs telemetry (time-to-kill, combos, dashes, perfect dodges, ultimates) to a session file. Afterwards the friend answers the short playtest questionnaire (§7.4). The team compares answers and telemetry against the Success Metrics.
- **UJ-4. The team tunes the dash without code.** After a playtest, both team members feel the dash is too floaty. They open the Heracles `DashConfig` asset, shorten duration and recovery, enter Play Mode and feel the change immediately. They replay with the same Run Seed to compare against the same wave sequence.

## 3. Glossary

- **Player** — one human-controlled participant (input device + one Hero instance). The system supports N Players. The prototype UI exposes one.
- **Hero** — a playable character definition (stats, Ability Kit, Ultimate, Primary Status). Prototype Heroes: **Heracles** and **Theseus**.
- **Hero Select** — the pre-fight screen where the Player picks a Hero.
- **Hero Swap** — the prototype-only action that replaces the Player's active Hero with the other Hero in place, mid-fight, keeping the arena and all enemy Statuses intact.
- **Ability Slot** — one of three input-bound slots per Hero: **Basic** (no cooldown), **Special** (cooldown), **Signature** (longer cooldown). (GDD §5.1)
- **Ability** — an attack placed in an Ability Slot. Has exactly one **Attack Type**.
- **Attack Type** — a tag on an Ability: 🌀 **Spin** (circular, around the Hero), 🏹 **Shot** (ranged projectile), 🗡️ **Thrust** (short, focused). Any Attack Type may occupy any Ability Slot.
- **Ability Kit** — the three Abilities a Hero starts with. Must contain at least one close (Spin or Thrust) and one ranged (Shot) Ability.
- **Ability Chain** — a built-in bonus that triggers when a specific Ability is used within a short window after another specific Ability (GDD §5.2).
- **Ultimate** — the Hero-unique RAGE attack, usable only when RAGE is full.
- **DASH** — the evade move: no cooldown; i-frames only at its start; followed by **Dash Recovery**.
- **i-frames** — the window during which the Hero cannot be damaged.
- **Dash Recovery** — a short period after a DASH with no i-frames, during which a new DASH cannot start.
- **Perfect Dodge** — a DASH started within the narrow **Perfect Dodge Window** before an enemy hit would have landed on the Hero.
- **Combo** — the count of consecutive Player hits on enemies without the Player taking damage. Resets when the **Combo Window** (~2 s, tunable) passes with no hit, or when the Player takes damage.
- **RAGE** — a per-Player meter (0–100%) filled by hits, Combo milestones and Perfect Dodges. It **decays** over time. When full, the Ultimate is available.
- **Status** — a timed effect applied to an entity, defined as data (Status Definition) and processed by the generic **Status System**. The prototype implements **Shock** and **Wet**.
- **Shock (⚡)** — Heracles' Primary Status. On application, lightning **chains** from the target to nearby enemies.
- **Wet (💧)** — Theseus' Primary Status. Amplifies Shock received by the Wet entity (damage multiplier and extra chain reach). In the full game it also extinguishes Burn (not in the prototype).
- **Conducted Shock** — a Shock application or chain jump that lands on a Wet target and gets the Wet bonus.
- **Primary Status** — the Status a Hero's kit is built around (GDD §7).
- **Archetype** — an enemy role. Prototype: **Chaser** (Minotaur-man), **Shooter** (Cretan Archer), **Tank** (Bronze Automaton).
- **Telegraph** — a visible wind-up before an enemy attack lands.
- **Arena** — the single locked combat room of the prototype.
- **Wave** — a group of enemies spawned together. An **Encounter** is the ordered set of Waves in one Arena attempt.
- **Run Seed** — the single integer from which all randomness in an Encounter derives.
- **Aim Direction** — the direction Abilities fire. Defaults to the movement direction, modified by **Aim Assist** and **Direction Lock**.
- **Aim Assist** — light snapping of the Aim Direction to the nearest enemy within a cone around the movement direction (toggleable).
- **Direction Lock** — a held button that freezes the Aim Direction while the Hero moves freely (toggleable).
- **Tuning Asset** — a ScriptableObject holding designer-tunable values.
- **Combat Intensity** — a 0–1 value derived from combat state (enemy count, RAGE, HP) that drives dynamic music state selection (Phase 2).
- **Session Telemetry** — the per-session log of combat events used to measure Success Metrics.

## 4. Features

### 4.1 Hero Movement and Aim
**Description:** The Hero moves freely in 8+ directions with analog speed. It can move while attacking (GDD §4). Aim follows movement by default. Two independently toggleable aids, Aim Assist and Direction Lock, address the "can't shoot backwards while retreating" risk the GDD flags for playtest. Controller is primary and keyboard is supported at a basic level. Realizes UJ-1, UJ-2.

#### FR-1: Free movement
A Player can move their Hero freely with analog input (left stick / WASD) at a speed set per Hero in a Tuning Asset, including while an Ability is executing (unless that Ability's data says it roots the Hero).
**Consequences (testable):**
- Stick deflection below a configurable dead-zone produces no movement. Full deflection reaches the Hero's max speed within the configured acceleration time.
- Using any Ability does not stop movement unless the Ability's `rootsCaster` flag is set.
- Hero collides with Arena walls and does not leave the Arena bounds.

#### FR-2: Movement-direction aim
The Aim Direction equals the last non-zero movement direction. All Abilities fire along the Aim Direction.
**Consequences (testable):**
- When the stick returns to neutral, the Aim Direction keeps its last value.
- A visible aim indicator (placeholder arrow) shows the current Aim Direction at all times.

#### FR-3: Aim Assist (toggleable)
When enabled, firing an Ability snaps the Aim Direction to the nearest living enemy inside a cone (angle and range in a Tuning Asset) centered on the current Aim Direction. If no enemy is inside the cone, the Aim Direction is unchanged.
**Consequences (testable):**
- With Aim Assist on, a Shot fired with an enemy 15° off-axis inside range hits it. With Aim Assist off, the same Shot misses.
- The toggle is reachable from the in-game settings/debug menu and persists for the session.

#### FR-4: Direction Lock (toggleable)
When enabled, holding the Direction Lock button freezes the Aim Direction at its value when the button was pressed. Movement remains free.
**Consequences (testable):**
- While held, the Hero can walk away from an enemy and keep firing Shots at it.
- Releasing the button resumes movement-direction aim on the next movement input.
- When the feature is toggled off, the button does nothing.

#### FR-5: Right-stick aim (fallback, Could)
`[ASSUMPTION]` A third aim mode, right-stick / mouse aim, is available behind the same settings menu as the GDD §4 fallback if playtests show movement-aim is still frustrating. Priority: Could. Build it only if SM-6 fails.

### 4.2 DASH and Defense
**Description:** DASH is the only active defense (no block or parry, GDD §5.3). It has no cooldown. Spam is held back by i-frames only at the start, a Dash Recovery period, and a narrow Perfect Dodge Window that rewards timing rather than spamming. Realizes UJ-1, UJ-4.

#### FR-6: DASH
A Player can DASH in the current movement direction (or the Aim Direction if no movement input) with no cooldown. Distance, duration, i-frame duration and Dash Recovery duration are per-Hero Tuning Asset values.
**Consequences (testable):**
- The Hero takes no damage from hits that land during the i-frame portion of the DASH. Hits landing after i-frames end, including during Dash Recovery, deal damage.
- A DASH input during Dash Recovery is ignored, or buffered if input buffering is enabled (FR-8).
- DASH passes through enemies but not through Arena walls.
- A DASH cancels the Hero's own in-progress Ability recovery (animation-cancel) `[ASSUMPTION]`.

#### FR-7: Perfect Dodge
If a DASH begins within the Perfect Dodge Window (Tuning Asset, default narrow) before an enemy hit would have landed on the Hero, the DASH counts as a Perfect Dodge: the hit is avoided, RAGE gains the Perfect Dodge amount (FR-15), and a distinct feedback plays (flash + brief time-slow, both tunable/disableable).
**Consequences (testable):**
- An identical DASH started just outside the window avoids the hit via i-frames but grants no Perfect Dodge.
- Perfect Dodge is counted in Session Telemetry.
- Each enemy attack can grant at most one Perfect Dodge per Player.

#### FR-8: Input buffering
DASH and Ability inputs pressed up to a short buffer window (Tuning Asset) before they become legal execute as soon as they are legal.
**Consequences (testable):**
- Setting the buffer to 0 disables buffering.

### 4.3 Ability Slots and Hero Kits
**Description:** Each Hero has three Ability Slots: Basic (free), Special (cooldown) and Signature (longer cooldown). Each slot holds an Ability tagged Spin, Shot or Thrust. Any tag can go in any slot (GDD §5.1). Abilities are data-driven so new Abilities can be assembled from Tuning Assets. Each Hero also has a Hero-unique Ultimate (§4.5), a Primary Status (§4.6), and one built-in Ability Chain. Realizes UJ-1, UJ-2.

#### FR-9: Data-driven Ability Slots
A Hero's Ability Kit is defined by assigning Ability assets to the three Ability Slots. Each Ability asset defines Attack Type, damage, range/area, projectile parameters (if Shot), knockback, cooldown, startup/active/recovery timings, Status applied (optional, with stacks/duration), and whether it roots the caster.
**Consequences (testable):**
- Swapping which Ability asset sits in which slot requires no code change and works in Play Mode after re-entering.
- The Basic slot enforces cooldown = 0. Special and Signature show remaining cooldown on the HUD.
- Spin hits all enemies within its radius around the Hero. Shot spawns a projectile along the Aim Direction. Thrust hits a short narrow area in front of the Hero along the Aim Direction.

#### FR-10: Heracles Ability Kit
Heracles ships with: **Basic — Club Strike** (🗡️ Thrust, close, heavy hits); **Special — Thunderbolt** (🏹 Shot, applies Shock); **Signature — Storm Whirl** (🌀 Spin around Heracles, high damage, applies Shock to every enemy hit). Stats: higher damage, average speed, average HP (GDD §7: "heavy attacks"). `[ASSUMPTION]` Ability names and slot placement are the prototype's proposal, not the GDD's.
**Consequences (testable):**
- The kit contains at least one close Ability (Club Strike, Storm Whirl) and one ranged Ability (Thunderbolt).
- Thunderbolt and Storm Whirl apply Shock on hit (FR-18).

#### FR-11: Theseus Ability Kit
Theseus ships with: **Basic — Trident Thrust** (🗡️ Thrust, longer reach than Club Strike, strong knockback); **Special — Riptide Bolt** (🏹 Shot, piercing water projectile, applies Wet); **Signature — Maelstrom** (🌀 Spin, pushes all nearby enemies outward hard and applies Wet). Stats: lower damage, higher knockback/space control (GDD §7: "trident, space control, pushing enemies"). `[ASSUMPTION]` names and slot placement proposed by this PRD.
**Consequences (testable):**
- The kit contains at least one close and one ranged Ability.
- Riptide Bolt and Maelstrom apply Wet on hit (FR-19).
- Theseus' knockback visibly separates enemies more than Heracles' equivalent attacks.

#### FR-12: Ability Chains
Each Hero has one built-in Ability Chain, defined as data (first Ability, follow-up Ability, window, bonus effect):
- **Heracles — Thunderhead:** Signature (Storm Whirl) followed by Special (Thunderbolt) within the chain window → Thunderbolt splits into 3 bolts in a fan.
- **Theseus — Undertow:** Signature (Maelstrom) followed by Basic (Trident Thrust) within the chain window → the Thrust pulls enemies in front of Theseus toward him instead of pushing them away.
`[ASSUMPTION]` Chain choices are proposals. The GDD only requires 1–2 chains per Hero.
**Consequences (testable):**
- The chain triggers only when the follow-up starts within the window (Tuning Asset). A visible cue shows the chain is "armed" during the window.
- Adding a new chain requires only a new chain data asset, with no code change, as long as its bonus effect type already exists.

### 4.4 Combo
**Description:** The Combo counter rewards hitting without being hit (GDD §5.2). It is per Player and visible at all times. Realizes UJ-1.

#### FR-13: Combo counter
Each Player has a Combo counter that increments on every hit that Player's Abilities, Ultimate or Status effects deal to an enemy, and resets to 0 when (a) no hit lands within the Combo Window (default ~2 s, Tuning Asset) or (b) the Player takes damage.
**Consequences (testable):**
- A Shock chain that hits 4 enemies adds 4 to the Combo of the Player who applied the Shock.
- Taking damage resets Combo immediately, even inside the window. Avoided hits (i-frames) do not.
- The HUD shows the current Combo, a draining bar for the remaining window, and a clear reset cue.
- `[ASSUMPTION]` Hits on the same enemy count each time (no diminishing).

#### FR-14: Shrinking-window mode (playtest toggle)
A toggle switches the Combo Window from fixed to shrinking: the window shortens as Combo grows, down to a minimum (curve and minimum in a Tuning Asset). GDD §5.2 asks for this to be tested.
**Consequences (testable):**
- With the toggle on, at Combo 30 the window equals the curve value, never below the minimum.

### 4.5 RAGE and Ultimate
**Description:** RAGE fills from aggressive, skillful play and drains over time, pushing the player to spend it (GDD §5.2). When full, the Hero's Ultimate is available. Realizes UJ-1.

#### FR-15: RAGE fill
A Player's RAGE increases by: a fixed amount per hit dealt; a bonus at each Combo milestone (e.g. every 10 Combo); and a fixed amount per Perfect Dodge. All amounts are Tuning Asset values per Hero.
**Consequences (testable):**
- RAGE is clamped to 0–100%.
- Status-damage ticks fill RAGE at a separate (lower) per-hit rate `[ASSUMPTION]`.

#### FR-16: RAGE decay
RAGE decays at a configurable rate after a configurable grace period since the last RAGE gain. Decay also applies when RAGE is full.
**Consequences (testable):**
- With no gain, full RAGE reaches 0 in (grace + 100 / decay rate) seconds.
- Telemetry records each time RAGE reached full and whether the Ultimate was used before it decayed below full.

#### FR-17: Ultimate
When RAGE is full, the Player can activate the Hero's Ultimate, which consumes all RAGE:
- **Heracles — Wrath of Zeus:** lightning strikes every enemy in the Arena, dealing heavy damage and applying Shock at maximum chain count.
- **Theseus — Poseidon's Flood:** a wave sweeps the Arena from Theseus outward, dealing damage, applying Wet to every enemy and knocking them back strongly.
`[ASSUMPTION]` Ultimate designs proposed by this PRD (GDD only says "unique per hero").
**Consequences (testable):**
- The Ultimate input does nothing (with a "not ready" cue) when RAGE < 100%.
- The Hero has i-frames during the Ultimate's startup `[ASSUMPTION]`.

### 4.6 Status System: Shock and Wet
**Description:** A generic Status System applies, ticks, stacks, expires and resolves interactions between Statuses as data. The GDD lists six Statuses. The prototype implements only Shock and Wet, plus the Wet→Shock interaction, and must allow the remaining four (Burn, Curse, Slow, Rhythm) to be added later without architectural change (GDD §5.4). Realizes UJ-2.

#### FR-18: Generic Status System
Any damageable entity (enemy, and Hero for future use) can carry Statuses. A Status Definition asset specifies duration, max stacks, refresh rule, tick behavior, on-apply behavior, visual tint/icon, and a list of interaction rules (e.g. "when Status X is applied to an entity carrying Status Y, apply modifier Z").
**Consequences (testable):**
- Adding a new Status with existing behavior types requires only a new Status Definition asset.
- Adding a new behavior type requires a new behavior class only, with no edits to the Status System core (verified by code review against the architecture doc).
- Statuses on an entity are visible (tint + icon above the entity).
- Every Status application records its source Player (for Combo/RAGE attribution and co-op).

#### FR-19: Shock
When Shock is applied to an enemy, it takes Shock damage, and lightning chains from it to up to N other enemies within chain radius R, each jump dealing reduced damage (falloff). Each enemy is hit at most once per chain. N, R, damage and falloff are Tuning Asset values.
**Consequences (testable):**
- With 4 enemies in a line spaced under R apart and N ≥ 3, one Thunderbolt hit damages all 4. A visible lightning line connects each jump.
- Chain target selection uses the Run Seed-derived RNG when ties occur (FR-26).

#### FR-20: Wet
When Wet is applied, the enemy is marked Wet (blue tint) for a configurable duration. Re-applying refreshes duration. Wet has no damage of its own `[ASSUMPTION]`.
**Consequences (testable):**
- Wet expires after its duration unless refreshed.

#### FR-21: Wet amplifies Shock (Conducted Shock)
When Shock damage (initial hit or chain jump) lands on a Wet enemy, damage is multiplied by the Wet multiplier (default ×2, GDD §5.4 "doubles Shock damage"), and a chain jump that leaves a Wet enemy gets +1 extra jump and an extended radius. Conducted Shock has a distinct visual (e.g. brighter, blue-white arc). `[ASSUMPTION]` Wet is **not** consumed by Shock (stays until it expires). Tunable.
**Consequences (testable):**
- Against the same Wet Tank, Thunderbolt deals multiplier × its dry damage.
- A chain passing through Wet enemies reaches at least one more enemy than through dry ones in the same layout.
- Conducted Shock events are counted in Session Telemetry.

### 4.7 Hero Selection and Swap
**Description:** Both Heroes are playable. Because the Wet + Shock synergy needs both Heroes and the prototype has no second-player UI, the prototype offers a Hero Swap so one Player can set up Wet with Theseus and pay it off with Heracles. Realizes UJ-2.

#### FR-22: Hero Select
Before each Encounter, a minimal Hero Select screen lets the Player choose Heracles or Theseus with the controller. Restarting the Encounter returns to Hero Select (or repeats the last choice via a "quick restart" button).
**Consequences (testable):**
- Hero Select is fully navigable with a controller and with a keyboard.

#### FR-23: Hero Swap
During an Encounter, the Player can press the Hero Swap button to replace the active Hero with the other one at the same position. Enemy Statuses, the Arena and Wave progress are kept. Current HP fraction carries over. Combo and RAGE carry over `[ASSUMPTION]`. Ability cooldowns of the incoming Hero start ready. Hero Swap has its own cooldown (Tuning Asset, default a few seconds) and can be disabled in the settings/debug menu.
**Consequences (testable):**
- Wet applied by Theseus is still active on enemies immediately after swapping to Heracles.
- Swap during DASH or Ultimate is ignored.
- Telemetry records swaps and Conducted Shock events within N seconds after a swap.
**Out of Scope:** Hero Swap is a prototype tool to test cross-hero synergy. Whether it becomes a shipped mechanic is an open question (§9).

### 4.8 Enemies
**Description:** Three Archetypes from GDD §10.3, each with a readable Telegraph so DASH and Perfect Dodge are skill-based. Enemies are data-driven (stats and behavior parameters in Tuning Assets). Realizes UJ-1, UJ-3.

#### FR-24: Enemy archetypes
- **Chaser — Minotaur-man:** fast. Closes distance and performs a telegraphed charge (wind-up, then a straight rush that deals contact damage). Low HP. Role: forces the Player to move.
- **Shooter — Cretan Archer:** keeps a preferred distance, retreats when the Player is close, fires telegraphed arrows (aim line shown during wind-up). Low-medium HP. Role: forces the Player to close distance or use Shots.
- **Tank — Bronze Automaton:** slow, high HP, high knockback resistance, telegraphed heavy slam with an area. Role: demands a long Combo or a status synergy. `[ASSUMPTION]` Tank has no armor mechanic beyond HP and knockback resistance in the prototype.
**Consequences (testable):**
- Every enemy attack that can damage the Player has a Telegraph of at least the configured minimum duration, and the Telegraph is visually distinct per Archetype.
- Enemies target the nearest Player (N-player aware, FR-30).
- Enemies flash on hit, get knocked back per their resistance, and die at 0 HP with a death effect.

#### FR-25: Arena and Encounter
The prototype has one Arena: a closed room at the 640×400 reference resolution with walls and some pillars for cover `[ASSUMPTION]`. An Encounter consists of a configurable number of Waves (default 3). Each Wave's composition is drawn from a weighted Archetype table with a difficulty budget per Wave, and spawn points are chosen from a set of spawn markers, all seeded. The next Wave starts when the current one is cleared (or after a timeout, tunable). Clearing the last Wave ends the Encounter with a results panel (clear time, max Combo, damage taken, Perfect Dodges, Ultimates used, Run Seed).
**Consequences (testable):**
- Spawns show a short spawn Telegraph, and enemies cannot damage the Player during it.
- Player death ends the Encounter with the same results panel and a Restart option.

### 4.9 Seeded Randomness
#### FR-26: Single Run Seed
Every random decision in an Encounter (Wave composition, spawn points, enemy behavior variation, Shock chain tie-breaks, any crit/variance) derives from one Run Seed. Each subsystem draws from its own stream derived from the Run Seed, so one system's draws do not shift another's. The Run Seed is shown on the HUD and results panel, can be typed in from the debug menu, and a "replay same seed" option exists.
**Consequences (testable):**
- Two Encounters with the same Run Seed and no Player input produce identical Wave compositions and spawn positions.
- Using more Abilities (more chain tie-break draws) does not change Wave composition for that seed.
- No code path calls `UnityEngine.Random` or `System.Random` without a seeded stream (enforced by code review / static search).

### 4.10 Combat Feedback (Juice)
#### FR-27: Hit feedback
Hits produce placeholder feedback: enemy flash, short hitstop, knockback, floating damage numbers, light screen shake on heavy hits and Ultimates. Player damage produces a red flash and a brief invulnerability `[ASSUMPTION]` (post-hit i-frames, tunable, default short). All intensities are tunable and individually toggleable.
**Consequences (testable):**
- Setting hitstop to 0 disables it. Damage numbers can be turned off.
- Placeholder SFX hooks fire for hit, DASH, Perfect Dodge, Ultimate, Status apply and Conducted Shock `[ASSUMPTION]` (sounds may be silent/blips in Phase 1).

### 4.11 HUD, Settings and Debug
#### FR-28: Combat HUD
Shows per Player: HP (numeric + bar), Combo with window bar, RAGE meter (with "full" state), Special and Signature cooldowns, active Hero, Hero Swap cooldown, and the Run Seed. Readable at 640×400 on a Steam Deck screen.

#### FR-29: Settings / debug menu
A pause menu (controller-navigable) exposes: Aim Assist on/off, Direction Lock on/off, aim mode (FR-5 if built), shrinking Combo Window on/off, Hero Swap on/off, damage numbers on/off, Run Seed entry, restart, and toggles for god mode and enemy freeze (debug).

### 4.12 Session Telemetry
#### FR-30: Session Telemetry log
The prototype writes a per-session log file (local, JSON or CSV) recording: Hero, Run Seed, Encounter result and clear time, per-enemy time-to-kill (spawn-to-death and first-hit-to-death) by Archetype, Combo values at each reset and the max, DASH count, Perfect Dodge count, i-frame time share, RAGE-full events and Ultimate uses (and RAGE wasted to decay), Ability uses per slot, Hero Swaps, Conducted Shock events, damage taken by source Archetype, deaths.
**Consequences (testable):**
- One file per session in a known folder. No network transmission.
- A short summary is printed on the results panel.

### 4.13 Dynamic Music (Phase 2)
**Description:** Kickoff milestone 6. Music is part of the game's identity (GDD pillar 5). Built only after the Phase 1 "is it fun" gate.

#### FR-31: Combat Intensity
A Combat Intensity value (0–1) is computed continuously from combat state: living enemy count, RAGE level and Player HP (weights in a Tuning Asset), smoothed to avoid flicker. With N Players it aggregates across them `[ASSUMPTION: max of per-player values]`.

#### FR-32: State-driven music crossfade
Four separate, independently looping tracks (calm, combat, advanced-combat, boss) crossfade smoothly through an Audio Mixer according to game state. Calm plays after combat and in rooms with no combat. Combat plays during normal combat. Advanced-combat is combat with a distinct added element (e.g. faster tempo) for high intensity. Boss plays for bosses and sub-bosses. The tracks do not share BPM or key, so each state change is clearly audible. Combat vs advanced-combat is chosen by Combat Intensity thresholds with hysteresis, calm when no enemies are alive, and boss via an explicit encounter flag. Fade times are configurable. Placeholder tracks are generated with ACE-Step 1.5, saved under `Assets/Audio/Placeholder/` with the `AI_PLACEHOLDER_` prefix and documented in `manifest.md` (caption, seed, BPM, key).
**Consequences (testable):**
- Each track loops on its own without an audible seam, and tracks need no shared tempo, key or sample-accurate sync.
- Only the tracks involved in a transition are audible during a crossfade, and the state never flickers between combat and advanced-combat (hysteresis).
- Transitions are audible but not abrupt (no clicks), and fade times are tunable.
- Placeholder music is never used in marketing material without approval.

## 5. Cross-Cutting Non-Functional Requirements

- **NFR-1 Seeded RNG:** all randomness from the Run Seed via per-subsystem streams (FR-26). Required for the future Daily Run.
- **NFR-2 N-player ready:** no singleton or static "the player". Combat, Combo, RAGE, aim, input, Status attribution, enemy targeting and HUD are per-Player instances. The prototype runs with one Player, but spawning a second Player via a debug command must work without code changes to combat systems (verified by a debug "spawn Player 2 on second controller" test) `[ASSUMPTION: this debug test is in scope; no polished 2nd-player UI]`.
- **NFR-3 Input:** Unity Input System with action maps. Full controller support (Xbox/PlayStation layouts, Steam Deck built-in controls) and basic keyboard (+ mouse for menus). No hard-coded keys. All actions rebindable in data (no rebind UI required).
- **NFR-4 Tuning without code:** every gameplay number (movement, DASH, Perfect Dodge, Abilities, chains, Combo, RAGE, Statuses, enemies, waves, feedback, aim, Hero Swap, music intensity) lives in ScriptableObject Tuning Assets. Changes apply on the next Play Mode entry at minimum. Live-edit during Play Mode is desirable.
- **NFR-5 Performance:** stable 60 FPS on Steam Deck at 1280×800 (640×400 ×2) with 15 enemies, 30 projectiles and full Shock chains on screen. Input-to-action response within one frame of the input being read.
- **NFR-6 Pixel-perfect presentation:** URP 2D, Pixel Perfect Camera at 640×400 reference, 32×32 character scale. Placeholder shapes only (colored primitives, distinct per Archetype/Hero). No art production.
- **NFR-7 Testability:** core rules (Combo, RAGE fill/decay, Status application and Wet→Shock interaction, seeded streams) are implemented as engine-light logic covered by EditMode tests.
- **NFR-8 Work-session sizing:** every story fits a single 1–3 h work session and ends in something runnable (kickoff rule). Team capacity is ~3 h/week total.
- **NFR-9 Engine/platform:** Unity 6000.6.7f1, PC (Windows) build, Steam Deck as official target from day one.
- **NFR-10 Code reuse:** prototype systems are written to be carried into the Crete vertical slice. Throwaway code is limited to debug tools and the Hero Swap UI.

## 6. Non-Goals (Explicit)

- Not a vertical slice: no map/room generation, corridors, doors between rooms, minimap, or the other nine room types.
- No village, items/relics, economy (drachmas, keys, ichor), potions, shop, achievements, codex or meta progression.
- No bosses (Medusa, Talos, Minotaur) and no elite enemies.
- No faction/loyalty system.
- No co-op networking and no second-player UI. Only N-player-capable architecture (NFR-2).
- No art, animation or final audio. Placeholder shapes, blips and AI placeholder music only.
- No Statuses beyond Shock and Wet. No environment hazards (traps, pits, water tiles, breakable pots/walls) `[ASSUMPTION: pits deferred even though Theseus' identity includes pushing into pits; see §9]`.
- No save/load, no Daily Run leaderboard (only the seed foundation).
- No in-run Ability swapping/upgrades, no village Ability variants.

## 7. Scope, Phasing and Success Metrics

### 7.1 Phasing (maps to kickoff milestones)

| Phase | Milestone | Contents | FRs |
| --- | --- | --- | --- |
| 0 | M3 | Unity skeleton, Input System, Pixel Perfect Camera, test scene | NFR-3, NFR-6, NFR-9 |
| 1A | M4 | Movement, aim + toggles, DASH/Perfect Dodge framework, Ability Slots, Heracles kit vs. training dummies | FR-1–FR-4, FR-6–FR-10, FR-27 (basic), FR-29 (partial) |
| 1B | M5 | Combo, RAGE, Ultimate, Status System (Shock, Wet, interaction), Theseus kit, chains, Hero Select/Swap, 3 Archetypes, Arena/Waves, seeded RNG, HUD, telemetry → **"is it fun" gate** | FR-11–FR-26, FR-28, FR-30 |
| 2 | M6 | Dynamic music: Combat Intensity, 4-track state-driven crossfade (calm, combat, advanced-combat, boss), ACE-Step placeholders | FR-31, FR-32 |

`[ASSUMPTION]` M5 is too large for one 1–3 h session and must be split into several sub-milestones in the epics breakdown. Seeded RNG streams (FR-26) should land early in 1B (before enemies/waves). Theseus + Hero Swap should land after the Status System. FR-5 (right-stick aim) is built only if SM-6 fails.

### 7.2 MVP (Phase 1) In Scope
Two Heroes with full kits, chains and Ultimates. DASH with Perfect Dodge. Combo (fixed + shrinking toggle). RAGE with decay. Generic Status System with Shock, Wet and Conducted Shock. Hero Select + Hero Swap. 3 Archetypes. One Arena with seeded Waves. HUD, settings/debug menu, Session Telemetry. All tuning in Tuning Assets.

### 7.3 Success Metrics — the "is combat fun" gate (end of Phase 1B)

Measured over a playtest of **both team members plus at least 3 external playtesters** `[ASSUMPTION: 3 external testers is achievable]`, each playing ≥3 Encounters per Hero, with Session Telemetry captured. Thresholds are starting targets and may be recalibrated once, before the gate, with the reason logged.

**Primary**
- **SM-1 "One more run":** ≥ 4/5 average on "I wanted to play again" (Q1), and ≥ 3 of 5 playtesters ask to keep playing or replay without prompting. Validates the whole prototype.
- **SM-2 Responsiveness:** ≥ 4/5 average on "Controls felt responsive and I felt in control" (Q2). Telemetry: input-to-action ≤ 1 frame (NFR-5). Validates FR-1, FR-6, FR-8.
- **SM-3 Synergy discovered:** ≥ 3 of 5 playtesters can explain Wet + Shock unprompted after playing (Q6). ≥ 3 Conducted Shock events per Encounter in Encounters where Hero Swap was used. Tank time-to-kill with Wet + Shock is ≥ 30% shorter than with a single Hero. Validates FR-19–FR-23.

**Secondary (feel metrics from telemetry)**
- **SM-4 Time-to-kill (first-hit-to-death, focused attack, no synergy):** Chaser 1.5–3 s, Shooter 2–4 s, Tank 6–10 s. Encounter (3 Waves) clear time 60–150 s. Validates FR-9–FR-11, FR-24, FR-25.
- **SM-5 Combo:** median max-Combo per Encounter ≥ 12 by the third Encounter. Experienced players (team) reach ≥ 25 at least once per session. ≥ 40% of Combo resets come from the window timing out rather than taking damage (shows players are not just tanking hits). Validates FR-13, FR-14.
- **SM-6 Aim:** ≥ 3.5/5 on "I could hit what I aimed at" (Q4), with Aim Assist on. Record which toggle combination each tester preferred. Failure triggers FR-5.
- **SM-7 DASH usage:** 8–30 DASHes per minute of combat. ≥ 1 Perfect Dodge per Encounter by the third Encounter. Validates FR-6, FR-7.
- **SM-8 RAGE loop:** ≥ 1 Ultimate per Encounter on average. RAGE-full events that decay without an Ultimate ≤ 40%. Validates FR-15–FR-17.
- **SM-9 Kit usage:** every Ability Slot accounts for ≥ 15% of Ability uses per Hero (no dead slot). Chains trigger ≥ 2 times per Encounter for the team. Validates FR-9–FR-12.
- **SM-10 Challenge:** first-attempt Encounter clear rate 40–75% for external testers. ≥ 3.5/5 on "Deaths felt fair; I could see what hit me" (Q5). Validates FR-24, FR-27.
- **SM-11 Heroes feel distinct:** ≥ 4/5 on "The two heroes felt different to play" (Q3).

**Counter-metrics (do not optimize)**
- **SM-C1 i-frame share:** share of combat time with active i-frames ≤ 25%. Higher means DASH spam beats the game (counterbalances SM-7).
- **SM-C2 Combo inflation:** do not hit SM-5 by lengthening the Combo Window beyond ~3 s or by counting Status ticks generously. Combo should reflect skill (counterbalances SM-5).
- **SM-C3 Difficulty collapse:** do not raise clear rate above 75% just to improve SM-1. "Too easy" is a stated GDD fear (counterbalances SM-10).
- **SM-C4 Swap dependency:** Heroes must stand alone. Each Hero's solo Encounter clear rate must stay within 15 points of the other's (counterbalances SM-3).

**Gate rule:** Pass = SM-1, SM-2 and SM-3 met, plus at least 5 of SM-4–SM-11. Otherwise, run a tuning round (three proposed tuning changes per kickoff milestone 5), re-test, and escalate to a design change if a second round fails.

### 7.4 Playtest Questionnaire (1–5 Likert unless noted)
1. I wanted to play again right away.
2. Controls felt responsive and I felt in control.
3. The two heroes felt different to play.
4. I could hit what I aimed at. (Which aim settings did you use?)
5. Deaths felt fair; I could see what hit me.
6. (Open) Did you notice anything special happen when you combined the heroes? Explain it.
7. (Open) The best moment, and the most frustrating moment.
8. (Open) Did RAGE / the Ultimate feel worth chasing?
9. DASH felt good, neither too strong nor too weak.
10. I understood the combo counter and tried to keep it alive.

Facilitator notes, without coaching: record when the tester first DASHes through an attack on purpose, first triggers an Ultimate, and first triggers a Conducted Shock.

## 8. Risks

- **Movement-aim frustration with Shots** (GDD §4). Mitigation: FR-3/FR-4 toggles, FR-5 fallback, SM-6.
- **DASH spam** (GDD §5.3). Mitigation: Dash Recovery, i-frames only at start, SM-C1.
- **Scope vs. ~3 h/week:** two Heroes + Status interaction roughly doubles content over the GDD's one-Hero prototype. Mitigation: Theseus and Hero Swap sequenced after Heracles proves the core. Theseus can slip without blocking the SM-1/SM-2 read (SM-3 then deferred).
- **Hero Swap masks solo balance.** Mitigation: SM-C4.

## 9. Open Questions

1. Should Hero Swap (or a variant) become a shipped mechanic, or is cross-hero synergy reserved for co-op? (Affects co-op design and GDD §7.)
2. Is Wet consumed by Shock, or does it persist until expiry? (Defaulted to persist, tunable.)
3. Should Theseus' identity ("push enemies into pits and traps") be tested with one pit in the Arena in Phase 1B, or wait for the vertical slice? (Defaulted to wait.)
4. Fixed vs. shrinking Combo Window: which becomes default? Decided by playtest (FR-14).
5. Does Combo/RAGE carry over through Hero Swap, or reset? (Defaulted to carry over.)
6. Are 3 external playtesters available for the gate, and on which hardware (Steam Deck vs. PC + controller)?
7. Ability, chain and Ultimate names/designs proposed here need user approval (GDD only specifies Primary Status and style).
8. Should the Tank get an armor/stagger mechanic (GDD: "requires long combo or push into pit")?
9. Combat Intensity weights and thresholds for music (Phase 2): to be set by ear in M6.

## 10. Assumptions Index

- §4.1 FR-5 — right-stick/mouse aim exists only as a Could, built if SM-6 fails.
- §4.2 FR-6 — DASH cancels the Hero's own Ability recovery.
- §4.3 — "three ability slots + special/signature" interpreted per GDD §5.1: slots are Basic / Special / Signature, and the Hero-unique "special" beyond the slots is the RAGE Ultimate plus the Primary Status mechanic (Shock chain / Wet).
- §4.3 FR-10/FR-11 — Ability names, Attack Type per slot and stat profiles are proposals.
- §4.3 FR-12 — one chain per Hero (Thunderhead, Undertow) are proposals.
- §4.4 FR-13 — repeated hits on the same enemy count fully toward Combo. Status-applied chain hits count.
- §4.5 FR-15 — Status damage fills RAGE at a lower per-hit rate.
- §4.5 FR-16 — RAGE decays after a grace period, including when full.
- §4.5 FR-17 — Ultimate designs (Wrath of Zeus, Poseidon's Flood) and i-frames during Ultimate startup.
- §4.6 FR-20/21 — Wet deals no damage. Default Wet multiplier ×2 (GDD "doubles"), +1 chain jump and extended radius from Wet targets. Wet not consumed.
- §4.7 FR-22/23 — Hero Select screen before each Encounter + in-place Hero Swap (with cooldown, toggleable) is how the Player picks and switches Heroes. Swap keeps HP fraction, Combo and RAGE, and enemy Statuses persist. Chosen because the synergy must be demonstrable with one Player and no second-player UI.
- §4.8 FR-24 — Tank has no armor mechanic beyond HP and knockback resistance.
- §4.8 FR-25 — Arena includes pillars for cover. Default 3 Waves with budgeted, weighted composition.
- §4.10 FR-27 — short post-hit invulnerability for the Player. Placeholder SFX hooks in Phase 1.
- §4.13 FR-31 — with N Players, Combat Intensity uses the max across Players.
- §5 NFR-2 — a debug "spawn Player 2" test is in scope to prove N-player readiness.
- §6 — environment hazards (pits, water tiles, traps) are out of scope, despite the GDD §5.4 "water conducts lightning" idea.
- §7.1 — M5 will be split into multiple sub-milestones. Theseus/Hero Swap come after Heracles + Status System.
- §7.3 — at least 3 external playtesters, and the metric thresholds are starting targets.
- Departure from GDD §5.4 ("prototype: Shock only") and §7 ("Heracles is the prototype hero"): two Heroes and Wet added by explicit user decision.

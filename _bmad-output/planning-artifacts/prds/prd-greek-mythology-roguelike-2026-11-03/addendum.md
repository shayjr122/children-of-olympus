# Addendum: Combat Prototype PRD

Depth that belongs downstream (architecture / tuning) rather than in the PRD itself.

## A. Starting tuning values (all Tuning Assets; first-pass guesses, to be tuned in playtest)

| Area | Value | Start |
| --- | --- | --- |
| Movement | Heracles / Theseus max speed | 5.5 / 6.0 units/s (1 unit = 32 px) |
| Movement | Acceleration time / stick dead-zone | 0.06 s / 0.2 |
| DASH | Distance / duration | 3 units / 0.15 s |
| DASH | i-frames (from DASH start) | 0.10 s |
| DASH | Dash Recovery (no i-frames, no new DASH) | 0.12 s |
| DASH | Perfect Dodge Window (before hit lands) | 0.08 s |
| DASH | Perfect Dodge time-slow | 0.15 s at 30% speed |
| Input | Buffer window | 0.12 s |
| Aim Assist | Cone half-angle / range | 25° / 8 units |
| Combo | Combo Window (fixed) | 2.0 s |
| Combo | Shrinking mode | 2.0 s at 0 → 1.0 s at 50, min 0.8 s |
| RAGE | Per hit / per status tick / per Perfect Dodge | 2% / 0.5% / 10% |
| RAGE | Combo milestone bonus | +5% every 10 Combo |
| RAGE | Decay grace / rate | 1.5 s / 8% per s |
| Abilities | Special / Signature cooldown | 3 s / 8 s |
| Chains | Chain window | 1.0 s |
| Shock | Max jumps N / radius R / falloff | 3 / 3.5 units / ×0.7 per jump |
| Wet | Duration / Shock multiplier / bonus jumps / radius bonus | 6 s / ×2.0 / +1 / +1.5 units |
| Hero Swap | Cooldown | 4 s |
| Feedback | Hitstop light / heavy | 0.03 s / 0.08 s |
| Player | Post-hit invulnerability | 0.5 s |
| Enemies | HP Chaser / Shooter / Tank | 30 / 40 / 160 (Heracles Basic ≈ 10) |
| Enemies | Min Telegraph | 0.4 s (Tank slam 0.8 s) |
| Waves | Count / max simultaneous enemies | 3 / 8 |

## B. Technical notes for architecture

- **Seeded RNG:** one Run Seed → deterministic per-subsystem streams (e.g. hash(RunSeed, "waves"), "spawns", "ai", "status", "combat"). Wrap behind an injectable RNG service. Ban direct `UnityEngine.Random` use (analyzer or grep check in review).
- **N-player:** `PlayerInput`-per-player (Input System `PlayerInputManager`), per-player components for Combo/RAGE/Aim. Enemies query a player registry, not a static. HUD instantiated per player.
- **Status System:** Status Definition SO + behavior strategies (OnApply/OnTick/OnExpire/OnDamageReceived modifiers) + interaction-rule SOs. Damage pipeline with tagged damage (e.g. `DamageTag.Shock`) so Wet can modify only Shock damage.
- **Abilities:** Ability SO (Attack Type, timings, hit shape, projectile, status payload) + Chain SO (first, follow-up, window, bonus effect type).
- **Telemetry:** event bus → writer to `Application.persistentDataPath/telemetry/<timestamp>.json`.
- **Music (Phase 2):** three AudioSources started on the same DSP time (`PlayScheduled`), mixer-group volume crossfade driven by Combat Intensity thresholds.

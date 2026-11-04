# Art Direction Brief — Greek Mythology Roguelike

> **Status:** ✅ **Questionnaire answered (2026-11-04).** The decisions are in §0. Sections 2–8 keep the original analysis; where a decision changed a recommendation, the section says so.
> **Scope:** Art direction for the **Crete vertical slice** (GDD §17.2). It runs in parallel with the combat milestones (M5b onward) and does not touch `game/` until the team approves a style test.
> **Sources:** `GDD.md` (latest version on `prototype/combat`), §1, §7, §10, §15, §16, §17. PRD (NFR-6, FR-24/25 telegraphs). Architecture Spine (Pixel Perfect setup, LFS, "no Animator for combat timing"). The two concept images in `images/`.

---

## 0. Decisions

| # | Topic | Decision |
| --- | --- | --- |
| 1 | Who makes the art | **Fully in-house, AI-assisted.** Art uses **the same ~3 h/week** the team already has, worked together with Claude Code (not extra hours). |
| 2 | Visual direction | **Mix, balanced atmosphere:** A (Minoan Fresco Daylight) for gameplay and environments, B (pottery motifs) for UI, codex, cutscenes and item cards, C (Mythic Night Glow) for VFX and abilities. One coherent identity. |
| 3 | Camera | **3/4 top-down**, presented like Wizard of Legend. Not isometric. |
| 4 | Master palette | **Apollo (~46 colors).** Each region gets a distinctive subset that stays consistent with the master palette. |
| 5 | Outline | **1 px dark colored outline (sel-out)** on characters and pickups. No outline on environment. |
| 6 | Facing directions | **2 directions** (one drawn side, mirrored). Aim is shown by VFX and projectiles, as close to Wizard of Legend as practical. |
| 7 | Animation fidelity | **Showcase:** rich, fluid animation comparable to Wizard of Legend. Budget in §4.6. |
| 8 | Lighting | **Simple URP 2D lights:** global light per region, local lights (torches), ability and VFX lights, bloom mostly on VFX. No normal maps. |
| 9 | AI policy | **AI-assisted final art is allowed**, including assets in the commercial release, with review, cleanup and Steam disclosure. Workflow and rules in §6. |
| 10 | External budget | **$0.** No commissions. |

### 0.1 What these decisions mean (read before A1)

1. **Time.** Showcase animation with $0 budget raises the in-house estimate to about **~320 h** for the Crete vertical slice (§7.5). At 3 h/week that is about **2 years of art for the slice alone**. AI may shorten the static assets (icons, props, tiles, portraits). It is unlikely to shorten frame-by-frame animation much. The A1 style test measures the real speed, so the estimate should be replaced with measured numbers after A1.
2. **✅ Answered: art shares the team's 3 h/week** (GDD §17.1), worked with Claude Code. Each week's session is either art or code, so art milestones and combat milestones **alternate** instead of running in parallel. The calendar estimates in §7.5 assume all 3 h go to art; with alternating weeks they roughly double.
3. **✅ Steam disclosure accepted.** Any AI-generated content in the game, demo, playtest build or store page is declared in the Content Survey, and the store page shows that declaration (§6.4).
4. **AI and animation.** Generators do not keep a consistent pixel grid, palette and proportions across frames. The workflow in §6 uses AI for concepts, keyframe drafts and static assets, and treats animation frames as hand-finished. **Consistency is enforced by the controls in §6.5** (requested: many controls in this area). A1 checks whether that holds.
5. **Wizard of Legend as the reference for directions.** Check in A1 how WoL itself handles up/down movement, and whether 2 directions look right in our camera. Adding up/down idle and run later costs about 20–30 frames per hero.

---

## 1. Fixed constraints and what they imply

| Constraint | Source | What it means for art |
| --- | --- | --- |
| Characters **32×32** | GDD §15, PRD NFR-6 | The body fits a 32×32 cell (about 22–28 px tall). Attack frames need a larger canvas (**64×64**, feet pivot) for club swings, smears and capes. Faces are 3–5 px, so readability comes from **silhouette and color**, not detail. |
| Reference **640×400**, Pixel Perfect, ×2 on Deck (1280×800) | GDD §16, Spine (`assetsPPU 32`, crop = Windowbox, pixel snapping) | **PPU = 32** is already set: 1 unit = 32 px = one character. Recommended tile grid: **16×16** (cell 0.5 u), so a room viewport is 40×25 tiles. The full arena is visible at once, which suits WoL-style locked rooms. |
| Unity 6 URP 2D Renderer, `com.unity.2d.aseprite` 6.0.0 already in the manifest | Spine, `Packages/manifest.json` | `.aseprite` files import directly. No export step is needed (§5). |
| Git LFS on `*.aseprite`, `*.png` under `game/Assets/**` | Spine | Source art can live in the repo. Both machines need LFS. |
| **No Animator for combat timing**: timings are data in `AbilityRunner` | Spine §"Decisions" 6 | Attack animations must **stretch to the tuned phase durations** (windup / active / recovery), not the other way round (§5.3). |
| Telegraphs ≥ 0.4 s (Tank slam 0.8 s), "visually distinct per Archetype" | PRD FR-24, addendum | Each enemy needs a clear wind-up pose plus a ground decal. These are the most important enemy frames. |
| Every passive item that changes an attack must change **something visible** | GDD §18.4 | VFX must be **modular and recolorable** (color, shape, projectile count), not baked into hero frames. |
| Team of 2, ~3 h/week **total** | GDD §17.1 | Every frame counts. See §4 and §7. |

### 1.2 About the concept images in `images/`

They are AI-generated mood pieces. Useful: the **saturated per-hero accent color** (blue water, gold lightning, purple shadow), the glowing VFX against darker ground, and the Greek UI framing with icons in medallions. **Not usable as a target**, because:

- **Isometric** projection (GDD asks for the Wizard of Legend angle, which is 3/4 top-down). Isometric roughly doubles tile work and makes sorting and 8-direction sprites harder.
- **Detail density** about 2–3× what a 32×32 sprite at 640×400 can hold. Characters there are ~48–64 px tall, with painterly anti-aliasing.
- Shows an **outdated roster** (Perseus as son of Zeus). The GDD now has Heracles.
- AI-generated, so they **must not** appear on the Steam page or in a trailer without disclosure (§6).

---

## 2. Candidate visual directions

All three keep 32×32, 640×400, 3/4 top-down. They differ in palette, outline, shading and the art-history source.

### Direction A — "Minoan Fresco Daylight" (recommended base)

*Crete as it looked in Minoan wall paintings: warm plaster, ochre and red columns, Egyptian blue, saffron, bull-leaping energy.*

| Aspect | Choice |
| --- | --- |
| Palette | One **master palette of ~40–48 colors**, hue-shifted ramps (shadows go cool and purple, highlights go warm and yellow). Start from a Lospec palette: **Apollo** (46 colors, made for exactly this kind of game) or a **Resurrect 64** subset. **Endesga 32** if we want it tighter. Each region uses a **subset** of the master palette (GDD §10.1 "distinct palette per region"). Crete: ochre, terracotta, red, Egyptian blue, plaster white. |
| Outline | Characters: **1 px dark colored outline** ("sel-out": the outline takes a darker shade of the adjacent color, and black only at the ground contact). Environment: **no outline**, so characters pop. |
| Shading | 3–4 tones per material, light from top-left, no dithering on characters, light dithering allowed on large floors only. |
| Greek influence | **Minoan details** that read even at small size: red columns that are wider at the top, horn-of-consecration crenellations, bull motifs, spiral and wave friezes on wall tops, labrys (double axe) in props. |
| Lighting | Mostly baked in the sprites. URP 2D Global Light plus a few point lights for torches and VFX. **No normal maps.** |
| Pros | Best readability. Closest to the GDD ("modern, detailed, WoL scale"). Crete becomes instantly recognizable, and Minoan art is rarely seen in games, so it sets us apart from Hades. Region palettes come naturally. |
| Cons | Bright daylight is less "dramatic" in a trailer than C. Needs discipline so floors don't drown out characters. |
| Cost | **Baseline (1.0×).** |

### Direction B — "Black-figure / Red-figure Pottery"

*The whole game looks like a painted amphora: terracotta ground, black silhouettes with incised detail lines, meander borders.*

| Aspect | Choice |
| --- | --- |
| Palette | Very limited: **~12–16 colors**. Terracotta orange, black, cream, plus **2–3 saturated "divine" accents** (gold lightning, cyan water, purple curse) used only for gameplay-critical things: VFX, statuses, pickups. |
| Outline | Characters are mostly **black silhouettes** with cream or orange "incised" lines (black-figure), or the reverse for heroes (red-figure: orange figures on black). |
| Shading | Flat. One tone plus line detail. No gradients. |
| Greek influence | Direct: Attic black-figure (Archaic period) and red-figure vases (from about 530 BC). Meander and palmette borders frame the HUD and rooms. |
| Pros | **Unique** at first glance, strong trailer and capsule identity. **Cheapest shading** (flat), and the silhouette-first design suits 32×32 well. |
| Cons | Monotone backgrounds fight the GDD's "distinct palette per region" and status readability (Wet vs Shock vs Burn). Hero vs enemy separation is harder. Risk of feeling like a gimmick across a 65-minute run. The underworld and the sea palace are hard to tell apart. |
| Cost | **~0.8×** per frame (flat shading). |

**Best use of B:** not as the in-game look, but for **UI framing, the codex, item cards, milestone cutscenes and the opening narration** (GDD §14). Those are static or lightly animated, so the pottery look is cheap there and gives the game a signature.

### Direction C — "Mythic Night Glow" (closest to the concept images)

*Darker, desaturated environments. Heroes, enemies and VFX glow with saturated emissive color. URP 2D lights and bloom do much of the mood.*

| Aspect | Choice |
| --- | --- |
| Palette | Dark, low-saturation ground (~24 colors) plus a separate **emissive palette** for VFX and hero accents. |
| Outline | 1 px black outline, so characters stay readable on dark ground. |
| Shading | 4–5 tones, rim lights, **normal maps or emission masks** (`_MaskTex` secondary textures) for lit sprites. |
| Greek influence | Marble ruins at night, braziers, the underworld. Less specific to Crete. |
| Pros | Most "wow" in a trailer. VFX and statuses read very clearly against dark ground. Matches what the team already liked in the concept images. |
| Cons | **Highest cost:** secondary textures add about 30–60% per frame. **Technical risk:** bloom and light falloff break crisp pixels and the limited palette, and they need performance testing on the Deck. Darker look weakens "distinct palette per region". Visually close to Hades and Children of Morta, so it differentiates less. |
| Cost | **~1.4–1.6×** per frame. |

### Recommendation: A + B accents + C's glow only

- **In-game:** Direction A.
- **UI, codex, cutscenes, item cards, Steam capsule framing:** Direction B motifs (meander borders, black-figure vignettes).
- **VFX only:** C's idea of saturated emissive color with a **thresholded bloom** that affects only VFX layers (lightning, Ultimate, Perfect Dodge flash). No normal maps.

---

## 3. Reference games: what to borrow from each

| # | Game | Borrow | Avoid |
| --- | --- | --- | --- |
| 1 | **Wizard of Legend** | Camera angle and scale, room size relative to the screen, spell VFX that are bigger than the character, the quick "pose, then smear" attack frames, how elemental color codes the spells. | Its fairly generic environment palette. |
| 2 | **Hades** | Instant silhouette readability of mythological figures, one dominant accent color per character, the codex and UI tone. | Copying Greek iconography the way Hades draws it. Our differentiator is Minoan / pottery. |
| 3 | **Enter the Gungeon** | Characters of about 20–28 px that still feel full of personality, dodge-roll timing, item icons readable at 16–24 px, enemy design driven by one gag or silhouette. | Its 8-direction aim sprites (too costly for us). |
| 4 | **Nuclear Throne** | **Frame economy:** left/right-only sprites, 2–4 frame idles, huge feedback from hit flashes, screen shake and recoil instead of frames. This is our budget model. | The very low resolution and grunge palette. |
| 5 | **Hyper Light Drifter** | Limited-palette mastery, strong mood with few colors, dash and slash smear frames, telegraph clarity. | Mood-first readability. It is sometimes too dark and busy for a "fair but hard" game. |
| 6 | **The Binding of Isaac: Rebirth** | Items **visibly change the attack** (tear color, size, shape), which our synergy pillar requires (GDD §8, §18.4). The pickup "Bang" moment. | Its gross-out style. |
| 7 | **Moonlighter** | 3/4 top-down dungeon rooms with locked doors, room-type set dressing, and a cozy "village vs dungeon" contrast like our village (GDD §12). | Its slow, soft combat animation. |
| 8 | **Blasphemous** | Turning a specific art-history source (Spanish religious art) into pixel art with a coherent identity. That is our model for Minoan frescoes and Greek pottery. | Gore. Side-view-specific techniques. |
| 9 | **Children of Morta** | Environment lighting with torches and fog, family of heroes with distinct silhouettes and palettes (our 6 heroes). | Its cost. It needed a full art team. |
| 10 | **CrossCode** | Readable top-down combat VFX, elemental mode colors, how UI and HUD stay clean during chaos. | 16-bit-era proportions. |

---

## 4. Sprite and animation budget for the Crete vertical slice

### 4.1 Rules that keep the budget sane

1. **Two drawn directions for the vertical slice: side, mirrored for left and right.** Aim in 8 directions is carried by **VFX and projectiles** rotated in code, not by body frames (the Nuclear Throne / Soul Knight model). Up/down idle and run can be added after a playtest if facing looks wrong (+20 frames per hero).
2. **Body 32×32, canvas 64×64**, pivot at the feet (bottom center). Weapon and smears go out of the 32 px cell. VFX are separate sprites.
3. **Attack tags are split into phases:** `windup`, `active`, `recover`, so playback stretches to the tuned durations (§5.3).
4. **Hit flash, squash, knockback, shake and hitstop come from code** (`FeedbackRouter` already exists), not from frames. Hurt = 1–2 frames.
5. **Elites = palette swap + overlay VFX** (shader). Co-op Player 2 = palette swap. No extra frames.
6. **Theseus** is only in the combat prototype, not in the vertical slice. He stays a placeholder until after the slice (or gets idle/run only: ~10 frames).

### 4.2 Heracles (hero)

| Action | Frames | Notes |
| --- | --- | --- |
| Idle | 4 | Breathing, lion-pelt and cape motion |
| Run | 6 | |
| DASH | 3 | Start, stretch, land. Afterimages via shader (Perfect Dodge uses a gold tint) |
| Basic: Club Strike | 5 | windup 1, smear 1, impact 1, recover 2 |
| Special: Thunderbolt (Shot) | 4 | Throw pose. Bolt is VFX |
| Signature: Storm Whirl (Spin) | 6 | In 1, loop 4, out 1 |
| Ultimate: Wrath of Zeus | 8 | Raise, charge loop 2, strike, hold. The "trailer moment", so this one is worth extra polish |
| Hurt | 2 | |
| Death | 6 | |
| Spawn / victory | 4 | Optional (village and results screen) |
| **Total** | **~48** | +20 if up/down idle and run are added |

Plus a **portrait** (48×48 or 64×64, HUD and codex) and **4 ability icons** (24×24).

### 4.3 The three enemy archetypes (prototype set, GDD §10.3)

| Action | Minotaur-man (Chaser) | Cretan Archer (Shooter) | Bronze Automaton (Tank, body ~40×48) |
| --- | --- | --- | --- |
| Idle | 2 | 2 | 2 |
| Move | 6 (gallop) | 4 (walk / back-step) | 4 (heavy walk) |
| **Telegraph** | 3 (paws ground, head down) | 4 (draw bow, aim line is VFX) | 4 (raise fists, glow seams) |
| Attack | 2 (charge loop) | 2 (release) | 2 (slam) |
| Recover | 2 (skid) | — | 2 (stuck in ground) |
| Hurt | 1 | 1 | 1 |
| Death | 5 | 5 | 6 (falls apart in pieces) |
| **Total** | **21** | **18** | **21** |

Shared: spawn telegraph (6 VFX frames), ground telegraph decals (3 procedural shapes: line, cone, circle).

Not in the prototype set, but needed for a full Crete region (GDD: 4–6 types): **exploding labyrinth spider** (~14 frames) and **labyrinth priestess** (summoner, ~18 frames). Budget them in the second art pass.

### 4.4 Bosses (Crete)

Body ~64×64, canvas 96–128. These are the most expensive assets per frame.

| Boss | Frames | Approach to cut cost |
| --- | --- | --- |
| **The Minotaur** (main boss, 2 phases) | ~36 | Full frame-by-frame. The showcase character. Phase 2 = palette and damage overlay plus 6 new frames (included). |
| **Medusa** | ~30 | Upper body frame-by-frame. Snake hair is a short loop layer. The petrifying gaze is VFX. |
| **Talos** | ~15 + parts | **Modular puppet:** a bronze giant built from parts (head, torso, arms) moved in code with pixel-snapped translation only (no rotation, so the pixels stay clean). This fits a mechanical giant and costs about half. |

### 4.5 Environment, VFX, UI, village

| Group | Content | Count |
| --- | --- | --- |
| Crete tiles (16×16) | Floor ×3 variants, walls (reduced Rule Tile set), wall tops with Minoan frieze, pits, water | ~40 tiles |
| Props | Breakable urns ×2 (3-frame break), spike trap (4), fire trap (4), doors (locked / open / key / boss), cracked wall (3), red Minoan columns, statues, chest (3), room set pieces: shop stall, Hades altar, gods' altar, Hermes shrine, challenge stone | ~25 props |
| Sub-area variety | 3 sub-areas through prop sets and palette shifts, not new tilesets | — |
| VFX | Hit spark (4), heavy hit (5), dash dust (4), Perfect Dodge flash (3), Shock loop (4), lightning bolt strip (3, the chain line itself is procedural), Thunderbolt (3), Wrath of Zeus (8), death puff (5), arrow (1) | ~45 small frames |
| UI | HUD frame (HP, Combo, RAGE, cooldowns), meander border kit, ability icons, **20–30 item icons** (24×24), minimap tiles, codex page frame, pixel font (licensed, not drawn) | — |
| Village | One 640×400 screen built from tiles and props. Hestia, Chiron, potion seller: 4-frame idle each | ~12 frames + 1 scene |

### 4.6 ✅ Decided budget: Showcase (Q7)

The tables in §4.2–4.4 are the "economy" budget. The decision is **Showcase**, so the vertical slice targets these counts. The rules in §4.1 still apply: 2 directions, phase-split attack tags, feedback from code, elites as palette swaps.

| Asset | Economy | **Showcase (target)** | Where the extra frames go |
| --- | --- | --- | --- |
| Heracles | ~48 | **~70** | Idle 6, run 8, DASH 5, Club Strike 7 (2-frame anticipation, 2 smears), Thunderbolt 6, Storm Whirl 8, Wrath of Zeus 12, hurt 3, death 8, spawn / victory 8. Cape and lion-pelt follow-through on every action |
| Minotaur-man | 21 | **~30** | Longer charge wind-up, skid and turn, head-shake idle |
| Cretan Archer | 18 | **~26** | Full draw, nock, retreat hop |
| Bronze Automaton | 21 | **~30** | Steam vents, seam glow, multi-stage collapse |
| The Minotaur | ~36 | **~55** | Phase transition, roar, more attack variety |
| Medusa | ~30 | **~45** | Snake-hair loop, gaze wind-up, tail slither |
| Talos | ~15 + parts | **~25 + parts** | Still a modular puppet |
| Village NPCs | 3 × 4 | **3 × 6** | |
| VFX | ~45 | **~60** | Longer Ultimate and Perfect Dodge effects |

If the calendar becomes a problem, the cheapest lever that keeps the Showcase look is to keep Showcase for **Heracles, the Minotaur and the Ultimate**, and use the economy counts for regular enemies.

---

## 5. Tool pipeline: Aseprite to Unity

### 5.1 Tools

- **Aseprite** (~$20, or free if built from source). Alternative: Pixelorama (free, open source). Use one tool for both team members.
- **Lospec** for the palette (`.gpl` or `.ase` palette file committed under `docs/art/palettes/`).
- **Unity 2D Aseprite Importer** (`com.unity.2d.aseprite` 6.0.0, already installed).
- AI image generators are allowed (Q9), but **AI output never goes into Unity directly**. It always passes through the cleanup steps in §6.2 inside Aseprite. No automatic upscalers or "pixelize" filters as the final step, because they break the grid and the palette.

### 5.2 File conventions (proposal)

```
game/Assets/Art/
  Characters/Heroes/Heracles/Heracles.aseprite     # one file per character, tags per animation
  Characters/Enemies/MinotaurMan/MinotaurMan.aseprite
  Bosses/Minotaur/...
  Environment/Crete/Tiles/Crete_Tiles.aseprite     # 16x16 grid, slice per tile
  Environment/Crete/Props/...
  VFX/...
  UI/Icons/Items/...
  Palettes/Master.aseprite
  Atlases/Atlas_Heroes.spriteatlasv2 ...
  Placeholder/            # existing shapes
  Placeholder/AI/         # AI_PLACEHOLDER_* only, see §6
```

- **Tags:** `idle`, `run`, `dash`, `atk_basic.windup`, `atk_basic.active`, `atk_basic.recover`, `hurt`, `death` …
- **Layers:** `body`, `weapon`, `fx_guide` (guide layers are hidden and not imported).
- **Canvas:** 64×64 for heroes and enemies, 128×128 for bosses. Pivot at the feet, at the same pixel in every frame.
- **Naming** follows the Spine: `Hero_Heracles`, `Enemy_Chaser` → art: `Heracles.aseprite`, `MinotaurMan.aseprite`.

### 5.3 Unity import settings

| Setting | Value | Why |
| --- | --- | --- |
| Pixels Per Unit | **32** | Matches the Pixel Perfect camera's `assetsPPU` 32 |
| Filter Mode | **Point (no filter)** | Crisp pixels |
| Compression | **None** | Compression smears limited palettes |
| Generate Mip Maps | Off | |
| Wrap Mode | Clamp | |
| Pivot | Bottom center (custom, feet) | Y-sorting |
| Sprite Mesh Type | Tight for characters, Full Rect for tiles | |
| 2D Renderer: Transparency Sort | Custom Axis (0, 1, 0) | Top-down depth sorting by Y |
| Sprite Sort Point | Pivot | |
| Aseprite importer | Import mode *Animated Sprite*, layers merged, **"Generate Animation Clips" off** for combat characters (see below) | |

**Animation driving (proposal for the architect, not decided here):** the Spine says combat timing is data, not Animator. Suggested approach: a small `SpriteFlipbook` presentation component. It reads the frames of each tag and, for attacks, maps `AbilityRunner`'s current phase and normalized time to the `windup` / `active` / `recover` tags. If a designer shortens a windup in the tuning asset, the animation speeds up to match, and art never needs re-timing. Ambient objects (torches, NPC idles) can use plain clips.

### 5.4 Sprite atlases (Sprite Atlas v2)

| Atlas | Contents | Notes |
| --- | --- | --- |
| `Atlas_Heroes` | Heracles (+ Theseus later) | |
| `Atlas_Enemies_Crete` | 3–5 Crete enemies | One per region, loaded per region later |
| `Atlas_Bosses_Crete` | Medusa, Talos, Minotaur | Can split per boss if over 2048 |
| `Atlas_Env_Crete` | Tiles + props | |
| `Atlas_VFX` | All VFX | |
| `Atlas_UI` | HUD, icons, frames | |

Settings: **Filter Point, Compression None, padding 2–4 px, Allow Rotation off, Tight Packing off, max 2048×2048.** 

### 5.5 Shaders (Shader Graph, small and reusable)

- **Palette swap** (ramp or LUT): elites, co-op Player 2, Minotaur phase 2, region recolors of shared props.
- **Hit flash / status tint**: Shock, Wet and so on (statuses already have a tint field in `StatusDefinition`).
- **Afterimage** for DASH and Perfect Dodge.
- **VFX-only bloom**: threshold set so only emissive VFX colors bloom.

### 5.6 Technical flag for the architect (not an art decision)

With crop = Windowbox at 640×400, a **1920×1080 desktop** gets integer zoom ×2 (1280×800) with large black borders, because ×3 (1920×1200) doesn't fit. Options: allow a "stretch to fill" setting on desktop, use an ornamental border (pottery meander frame, Direction B) to fill the margin, or show slightly more of the world on 16:9 screens. The border option is cheap and on-brand. Worth one line in the next architecture review.

---

## 6. AI-assisted production (✅ decided: AI-assisted final art allowed)

> The original brief recommended AI for placeholders only. The decision (Q9) allows AI-assisted **final** art, with review and Steam disclosure. This section is the workflow for that decision.

### 6.1 Where AI helps most, and where it helps least

| Asset | AI usefulness | How to use it |
| --- | --- | --- |
| Concept art, mood boards, character design exploration | ⭐⭐⭐ High | Generate many options, pick one, then draw the 32×32 design sheet from it. |
| Item icons (24×24), portraits, codex illustrations, item cards | ⭐⭐⭐ High | Static, single image. Generate, downscale, quantize to Apollo, clean up by hand. |
| Props, set pieces, village background | ⭐⭐ Medium | Good start for shapes and composition. Needs grid and perspective fixes to match the 3/4 camera. |
| Tiles (16×16, must tile seamlessly) | ⭐ Low–medium | Use as texture reference. Seams and Rule Tile edges are easier to draw than to fix. |
| Animation frames | ⭐ Low | Generators don't keep the same proportions, palette and pixel grid from frame to frame. Use AI for **one keyframe pose** at most, then animate by hand in Aseprite. A1 tests this. |
| VFX | ⭐ Low | Small and frame-based. Faster to draw directly. |
| Steam capsule / key art | ⭐⭐ Medium | Allowed. It is the first thing players see, and it carries the disclosure label next to it, so give it the most cleanup time. |

### 6.2 Cleanup pipeline (every AI-assisted asset)

1. **Generate** at a larger size, with the prompt and reference image that match the style guide (this document, §2 A/B/C and the Apollo palette).
2. **Downscale** to the target size with nearest-neighbor (32×32 body, 24×24 icon and so on).
3. **Quantize to the Apollo palette** (Aseprite: *Sprite → Color Mode → Indexed* with the Apollo palette loaded).
4. **Hand cleanup in Aseprite:** fix stray pixels, orphan pixels and broken lines, add the 1 px sel-out outline (characters and pickups), set light from top-left, check proportions against the character's design sheet.
5. **Check in-game** at 640×400 next to existing assets, before calling it final.
6. **Log it** in the provenance manifest (§6.3).

Rule: **if cleanup takes longer than drawing it from scratch would, draw it from scratch.** Note the time in the manifest so the speed comparison is real data.

### 6.3 Provenance manifest

The Spine already uses `AI_PLACEHOLDER_*` plus `manifest.md` for ACE-Step music. For art, AI-assisted assets are allowed to be final, so the prefix no longer marks "to be replaced". The manifest is kept anyway, because the Steam disclosure and licensing need a record.

- One file: `game/Assets/Art/AI-PROVENANCE.md` *(proposal, not created in `game/`)*.
- One row per AI-assisted asset: asset path, tool and version, date, prompt or reference used, how much was hand-edited (light / heavy / redrawn), time spent.
- Assets that are only temporary placeholders still use the `AI_PLACEHOLDER_` prefix, so they are easy to find and replace.

### 6.4 Steam disclosure and licensing

Since January 2024, Valve's Content Survey asks developers to **disclose AI-generated content** that is *pre-generated* (made during development and present in the game, store page or marketing) or *live-generated* at runtime. The disclosure is shown on the store page.

- **Disclosure is required** under this decision, from the first public build. **Steam Playtest and the Next Fest demo are public builds.**
- Write the disclosure text in plain words, based on the manifest. For example: "AI tools were used to create concept art and as a starting point for some sprites, icons and illustrations. All in-game art was edited by hand to fit the game's style."
- Check the **current wording of the Content Survey** when filling it in, because Valve has adjusted it over time.
- **Tool terms:** before using a generator for commercial assets, check that its terms allow commercial use and what they say about ownership of the output. Record the tool and version in the manifest.
- **Copyright:** purely AI-generated images have weak or no copyright protection in several jurisdictions (for example, the US Copyright Office position). Substantial hand editing strengthens the case for the edited result. This matters most for the logo, capsule and key characters.
- **Marketing:** some players react negatively to the AI disclosure label. The quality bar in §6.2 is the best defense: the art should look deliberate and consistent.
- The concept images in `images/` (outdated roster, isometric) should still not be used as store screenshots.

### 6.5 Consistency controls

> Requested in the questionnaire follow-up: **many controls in this area, so everything stays consistent.** The controls are layered. Each layer catches what the one before it misses, and none of them depends on remembering to be careful.

#### Layer 1. One source of truth (the style bible)

Everything AI or a person makes is compared against these files. They live in `docs/art/` and change only by explicit decision.

| File | Contents |
| --- | --- |
| `palettes/apollo.hex` | The master palette, exactly as downloaded from Lospec. Never edited. |
| `palettes/<region>.hex` | Region subsets (Crete first). Every color must exist in `apollo.hex`. |
| `model-sheets/<Character>.png` | Per character: the approved neutral pose at 1× and 8×, body height in px, head height in px, which Apollo ramp each material uses (skin, bronze, cloth, pelt), outline color per material. |
| `golden-lineup.png` | Every approved character side by side at 1× and 4×, on the floor tile, same baseline. Regenerated by the checker (`lineup` command). |
| `animation-sheets/<Character>.md` | Per tag: frame count (from §4.6), which frames are keys, phase split (windup / active / recover). |
| `STYLE-RULES.md` | One page: light from top-left, sel-out rules, 3–4 tones per material, no dithering on characters, no pure black except ground contact, pivot at the feet. |

#### Layer 2. Controls on AI input

- **Pin the tool and version** per asset family (characters, icons, environment). Changing the tool means re-checking the golden lineup.
- **Prompt templates** in `docs/art/ai/prompts/` (one per asset family), versioned in git. Each has a fixed style block (palette, camera, outline, light direction, size) and a short variable part. Free-form prompts are not used for final assets.
- **Always attach reference images**: the model sheet and the golden lineup. Never generate a character without its model sheet.
- **Record seed and settings** in the provenance manifest (§6.3), so any asset can be regenerated.
- **Describe the style in our own words.** Don't put other games' or living artists' names in prompts. It hurts consistency (the model pulls toward their look) and raises licensing questions.
- **Optional, after ~30 approved assets:** train a small style model (LoRA) **only on our own approved, cleaned sprites**. This is the strongest consistency tool available, and it is safe because the training data is ours.

#### Layer 3. Pipeline gates (§6.2)

No step is skipped. Downscale with nearest-neighbor, quantize to Apollo, hand cleanup, in-game check, manifest entry. **AI output never goes into `game/Assets` without passing all five.**

#### Layer 4. Automated checks: `tools/art/check_sprites.py`

A script in this repo that checks exported sprites (PNG, or an Aseprite sheet plus its JSON). Rules come from `tools/art/art-rules.json`. It fails (exit code 1) on errors, so it can run before every commit and at every art gate.

| Check | Error or warning | Catches |
| --- | --- | --- |
| Every opaque pixel is an Apollo color (or a region subset color) | Error | AI color drift, anti-aliasing, gradients |
| Alpha is only 0 or 255 | Error | Soft edges from downscaling |
| Canvas size matches the category (64×64 character, 128×128 boss, 24×24 icon, 16×16 tile) | Error | Wrong export size |
| Feet baseline is on the same row in every frame of a tag | Error | Characters "floating" or sinking between frames |
| Body height stays within ±2 px of the tag median | Warning | Proportion drift between frames |
| Edge pixels of characters, pickups and icons are dark (sel-out) | Error above 15% light edge pixels | Missing or broken outline |
| Number of colors per sprite ≤ limit | Warning | Over-rendering, noise |
| Orphan pixels (no opaque neighbor) | Warning | AI noise left after cleanup |
| Frame count per tag matches the animation sheet | Warning | Missing or extra frames vs the §4.6 budget |
| Width and height of the opaque body within the model sheet's range | Warning | Character too big or small relative to the cast |

#### Layer 5. Unity import gate (proposal for `game/`, not added)

An `AssetPostprocessor` that forces the §5.3 import settings for everything under `Assets/Art/` (PPU 32, Point filter, no compression, no mipmaps), plus an EditMode test in the style of `BannedApiTests` that fails if any sprite under `Assets/Art/` has different settings or if a build scene references an `AI_PLACEHOLDER_` asset.

#### Layer 6. Human review

Every asset gets this checklist before it is marked final in the manifest:

1. **Lineup test:** placed next to the golden lineup at 1× and 4×. Same height logic, same outline weight, same light direction.
2. **Silhouette test:** filled black, is it still recognizable and different from the others?
3. **Grayscale test:** does it read in value alone? (Telegraphs and pickups must.)
4. **Onion-skin test** (animations): play the tag in Aseprite with onion skin. No popping volume, no wobble in parts that should be still.
5. **In-game test:** in the prototype arena at 640×400 and on the Steam Deck. A non-team viewer finds the hero, the enemy and the telegraph within 1 second.
6. **Sign-off:** one approver (the art lead) marks it final in the manifest. Assets without a sign-off are not final, even if they are in the game.

#### Layer 7. Drift control over time

- **Every 10 approved assets:** regenerate the golden lineup and compare with the previous one. If the style drifted, fix it now, not after 100 assets.
- **Palette changes** only by explicit decision, logged in `docs/art/CHANGELOG.md` with the reason. The checker makes an unlogged change impossible to miss.
- **Re-baseline** (accepting a deliberate style change) updates the model sheets and lineup in the same commit, so the source of truth never lags behind.

#### Definition of Done for one art asset

- [ ] `check_sprites.py` passes with no errors
- [ ] Human review steps 1–5 done
- [ ] Manifest row filled (tool, version, seed, prompt template, edit level, time, approver)
- [ ] Imported with the enforced settings (Layer 5 once it exists)
- [ ] Golden lineup updated if it is a character

---

## 7. Cost and time estimate (vertical slice)

### 7.1 Assumptions

Average production time for an **intermediate hobbyist** pixel artist, including cleanup:

| Asset | Time |
| --- | --- |
| 32×32-body frame (64 canvas) | ~20 min |
| 64×64-body boss frame | ~45 min |
| VFX frame (16–32 px) | ~10 min |
| 16×16 tile | ~20 min |
| Prop | ~30 min |
| 24×24 icon | ~20 min |
| Character design sheet (before animation) | 2–3 h |

These are averages. A beginner will be **1.5–2× slower** for the first 20–30 hours. The style test (§8) will give real numbers to replace these.

### 7.2 Hours (in-house, Direction A)

| Group | Hours |
| --- | --- |
| Heracles (48 frames + design + portrait + icons) | ~19 |
| 3 enemies (60 frames + designs) | ~23 |
| 3 bosses (~81 frames + Talos parts + designs) | ~70 |
| Crete tiles + props + sub-area variants | ~29 |
| VFX | ~8 |
| UI (HUD, borders, 20–30 item icons, minimap, codex frame) | ~22 |
| Village (scene + 3 NPCs) | ~14 |
| **Subtotal** | **~185** |
| Rework and learning buffer (+20%) | ~37 |
| **Total** | **~220 h** |

Direction B ≈ 0.8× (~175 h). Direction C ≈ 1.4–1.6× (~300+ h).

### 7.3 What that means in calendar time

| Art hours per week | Vertical slice art done in |
| --- | --- |
| 1 h/week (a third of team time) | ~4 years |
| 1.5 h/week (half of team time) | ~2.8 years |
| 3 h/week (all team time, code paused) | ~1.4 years |

This lines up with GDD §18.1, which already notes that pixel art alone exceeds the time budget. It should not be read as "impossible", but as **"art must be partly bought or commissioned, or scope cut further."**

### 7.4 Three scenarios

| Scenario | In-house hours | Money (rough market ranges, verify with real quotes) | Risk |
| --- | --- | --- | --- |
| **S1. All in-house** | ~220 h | ~$20 (Aseprite) | Calendar time. Quality depends on one person's skill. |
| **S2. Hybrid (recommended)**: in-house heroes, enemies, VFX, UI. **Commission the 3 bosses and the capsule / key art.** Environment either in-house or a commercial tileset recolored to our palette | ~110–140 h | Bosses: ~80–90 frames at 64×64 ≈ **$1,600–4,000**. Capsule / key art ≈ **$300–1,500**. Tileset pack ≈ $10–50 + ~10 h to recolor | Need to manage freelancers and a style guide (this document). A bought tileset weakens the Minoan identity. |
| **S3. Mostly commissioned** | ~40 h (direction, integration) | 32×32 frames at roughly $8–25 each, plus bosses, tiles and UI ≈ **$5,000–12,000** | Budget. Style drift between artists. |

### 7.5 ✅ Decided scenario: in-house, AI-assisted, Showcase, $0

Using the §4.6 Showcase counts and the §7.1 rates (25 min per character frame for the extra polish, 50 min per boss frame):

| Group | Hours |
| --- | --- |
| Heracles (~70 frames + design, portrait, icons) | ~33 |
| 3 enemies (~86 frames + designs) | ~39 |
| 3 bosses (~125 frames + Talos parts + designs) | ~112 |
| Crete tiles + props + sub-area variants | ~29 |
| VFX (~60 frames) | ~12 |
| UI (HUD, borders, 20–30 item icons, minimap, codex frame) | ~22 |
| Village (scene + 3 NPCs × 6 frames) | ~18 |
| **Subtotal** | **~265** |
| Rework and learning buffer (+20%) | ~53 |
| **Total without AI** | **~320 h** |
| Expected AI saving (static assets: icons, portraits, props, village, designs, about 70 h of the total, 25–40% faster) | −20 to −30 h |
| **Total with AI assistance** | **~290–300 h** |

| Art hours per week | Crete vertical slice art done in |
| --- | --- |
| 3 h/week (the decision) | **~1.9 years** |

These numbers are estimates. A1 measures the real time per frame and the real AI saving, and §7.5 should be updated with those numbers right after A1.

---

## 8. Proposed art milestones (parallel to the combat gates)

Each one ends with something you can look at in the game, following the GDD's milestone rule.

| Gate | Content | Hours | Exit check |
| --- | --- | --- | --- |
| **A0** ✅ | Questionnaire answered (§0). Apollo chosen as the master palette | done | GDD §15 diff approved |
| **A1 Style test** | Heracles idle + run + Club Strike, Minotaur-man idle + run + telegraph, one 40×25-tile mock Crete room, all in **Apollo**. Make **one asset two ways**: from scratch, and with the AI pipeline (§6.2) | 12–15 | Screenshot at 640×400 and on a **real Steam Deck** screen. Can a non-team viewer find the hero and the enemy within 1 second? Measured: minutes per frame, minutes per AI-assisted asset. Update §7.5 |
| **A2 Combat cast** | Full Heracles set + 3 enemies + core VFX at Showcase counts, swapped in for the placeholder shapes in the prototype arena | ~85 | Playtest: telegraphs are still readable (PRD SM metrics don't get worse than with placeholders) |
| **A3 Crete environment** | Tiles, props, traps, doors, 3 sub-area variants | ~30 | One room of each type is recognizable from its look alone |
| **A4 Bosses** | Medusa, Talos, Minotaur (in-house) | ~112 | |
| **A5 Village, UI, icons** | Village screen, HUD, meander kit, 20–30 item icons | ~40 | |
| **A6 Store art** | Capsule, key art, trailer shots (in-house, AI-assisted, disclosed) | ~15 | Needed before GDD §17.3 step 5 (Steam Coming Soon page) |

A1 is the only one that should start now. It is cheap, and it replaces every estimate in §7 with real numbers.

---

## 9. Decision questionnaire

> ✅ **Answered 2026-11-04.** The answers are in §0. The questions and original recommendations are kept below for the record.

1. **Who makes the art, and how many of the ~3 weekly hours go to it?**
   *Recommendation:* one of you becomes the **art lead** at ~1.5 h/week, starting with the A1 style test. Decide the outsourcing budget (Q10) after A1, when the real speed is known.

2. **Which visual direction: A (Minoan fresco daylight), B (black/red-figure pottery), C (mythic night glow), or a mix?**
   *Recommendation:* **A in-game, B for UI, codex and cutscenes, C's glow for VFX only** (§2).

3. **Camera: 3/4 top-down (Wizard of Legend) or isometric (like the concept images)?**
   *Recommendation:* **3/4 top-down.** It is what the GDD says, it costs about half as much in tiles and sorting, and it suits 2-direction sprites.

4. **Palette: which Lospec starting point for the master palette?**
   *Recommendation:* **Apollo (46 colors)**, with Resurrect 64 as the alternative to test in A1. Crete uses a ~20-color subset. Lock the master palette after A1 and never add colors without a reason.

5. **Outline style on characters: black outline, dark colored outline (sel-out), or none?**
   *Recommendation:* **1 px dark colored outline** on characters and pickups. **No outline** on environment.

6. **Facing directions for the vertical slice: 2 (side + mirror), 3 (down / up / side), or 4+?**
   *Recommendation:* **2 directions**, with aim shown through VFX. Revisit after the A2 playtest. Adding up/down idle and run later costs ~20 frames per hero.

7. **Animation fidelity: "economy" (~48 hero frames, §4) or "showcase" (~80+, secondary motion everywhere)?**
   *Recommendation:* **Economy everywhere, showcase only for the trailer moments:** Heracles' Ultimate, DASH / Perfect Dodge, and the Minotaur.

8. **Lighting: none (baked into the sprites), simple URP 2D lights, or full lit sprites with normal maps?**
   *Recommendation:* **Simple 2D lights** (global light plus torches and VFX lights), **no normal maps**, VFX-only bloom. Test performance on the Deck in A2.

9. **AI policy for art: internal placeholders only, or allow AI-assisted final art (with Steam disclosure)?**
   *Recommendation:* **Placeholders only**, with the `AI_PLACEHOLDER_` prefix, a manifest and a build gate (§6). Nothing AI-made in any public build or on the store page.

10. **Is there a money budget for the vertical slice art, and what is the ceiling?**
    *Recommendation:* **Scenario S2.** Set a ceiling of roughly **$2,000–4,000** for the 3 bosses plus capsule / key art. Get 2–3 quotes after A1, with this document and the A1 sprites as the style guide.

---

## Appendix A. Proposed GDD change (diff only, not applied)

Against `GDD.md` §15 on `prototype/combat` (verified with `git apply --check`; the same patch is in [`GDD-section15.diff`](GDD-section15.diff)). ✅ Updated with the **questionnaire answers** (§0).

```diff
@@ -406,15 +406,26 @@ Hades כבר עשה roguelike במיתולוגיה היוונית עם ברכו
 
 | נושא | החלטה |
 | --- | --- |
-| סגנון | **Pixel Art מודרני**, מופשט אך עם פרטים קטנים. קנה מידה וזווית בהשראת Wizard of Legend |
+| סגנון | **Pixel Art מודרני**, מופשט אך עם פרטים קטנים. קנה מידה וזווית בהשראת Wizard of Legend. ✅ **כיוון משולב:** "פרסקו מינואי" בסביבות ובמשחק עצמו, מוטיבים של כדי חרס בממשק, בקודקס, בסצנות ובכרטיסי החפצים, ו"זוהר מיתולוגי" באפקטים וביכולות (ראו `docs/art/ART-DIRECTION.md`) |
+| זווית מצלמה | ✅ **3/4 מלמעלה**, כמו Wizard of Legend. לא איזומטרי. תמונות הקונספט ב-`images/` הן השראה לאווירה בלבד |
 | רזולוציה | **דמויות 32×32** |
-| צבע | **פלטה מובחנת לכל אזור** |
+| גודל קנבס ואריחים | ✅ גוף 32×32 בתוך קנבס 64×64 (בוסים: גוף 64×64), PPU 32, אריחים 16×16 |
+| צבע | **פלטה מובחנת לכל אזור**. ✅ פלטת אב: **Apollo** (כ-46 צבעים, Lospec). כל אזור מקבל תת-פלטה ייחודית שנשארת עקבית עם פלטת האב |
+| קו מתאר | ✅ קו כהה צבעוני (Sel-out) ברוחב פיקסל לדמויות ולחפצים. בלי קו מתאר בסביבה |
+| כיווני דמות | ✅ 2 כיוונים (צד אחד מצויר + שיקוף), כמה שיותר קרוב ל-Wizard of Legend. כיוון התקיפה מוצג באפקטים ובקליעים |
+| רמת אנימציה | ✅ **Showcase:** אנימציות עשירות וזורמות ברמה של Wizard of Legend. כ-70 פריימים להרקלס, כ-26–30 לאויב, 25–55 לבוס |
+| תאורה | ✅ אורות URP 2D פשוטים: תאורה גלובלית לכל אזור, אורות מקומיים (לפידים), אורות לאפקטים וליכולות. Bloom בעיקר על אפקטים. בלי Normal Maps |
+| הפקת גרפיקה | ✅ **הכול בבית, בעזרת AI, תקציב חיצוני 0$.** הגרפיקה משתמשת באותן ~3 שעות שבועיות של הצוות (סעיף 17.1), בעבודה משותפת עם Claude Code, כך שאבני דרך של גרפיקה ושל קוד מתחלפות |
+| AI בגרפיקה | ✅ **מותר גם בנכסים הסופיים**, בתנאי שכל נכס עובר ניקוי ידני לפי כיוון האמנות (גודל, פרופורציות, פלטה, קו מתאר וסגנון אנימציה), נרשם ביומן מקור, ונבדקו תנאי השימוש המסחרי בכלי. ✅ **דיווח על תוכן AI ב-Steam**, כבר מהבילד הציבורי הראשון (Playtest / דמו) |
+| בקרת עקביות | ✅ שבע שכבות בקרה: מקור אמת אחד (פלטה, דפי מודל, Golden Lineup), תבניות פרומפט קבועות, צינור ניקוי, בדיקה אוטומטית (`tools/art/check_sprites.py`), שער ייבוא ב-Unity, סקירה אנושית וחתימה, ובדיקת סחף כל 10 נכסים (`docs/art/ART-DIRECTION.md` §6.5) |
 | מוזיקה | **כלים יווניים עתיקים** + אורקסטרה, ו**אלקטרוני / סינת'** |
 | מוזיקה דינמית | **כן, חלק מהזהות.** משתנה לפי עוצמת הקרב |
 | קול | **קריין לסרטונים בלבד** |
 
 הדגש הוויזואלי: **הדמויות המיתולוגיות** (בוסים, גיבורים, אויבים) צריכות להיות מזוהות מיד, ומונפשות בצורה שגורמת לשחקן לרצות לראות אותן.
 
+⚠️ **הערכת זמן:** כ-290–300 שעות גרפיקה ל-Vertical Slice (כרתים). מכיוון שהגרפיקה חולקת את 3 השעות השבועיות עם הקוד, זה יותר משנתיים. מבחן הסגנון (A1) ימדוד את הקצב האמיתי (`docs/art/ART-DIRECTION.md` §7.5).
+
 ---
 
 ## 16. טכני ופלטפורמות
```

A second, smaller suggestion for GDD §17.3: add **"A1 style test"** right after step 2 (combat prototype), and **"store art (capsule, key art)"** as a prerequisite of step 5 (Steam page).

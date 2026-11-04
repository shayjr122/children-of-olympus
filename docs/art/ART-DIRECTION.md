# Art Direction Brief — Greek Mythology Roguelike

> **Status:** Draft for discussion (one-hour session). Nothing here is decided until the questionnaire in §9 is answered.
> **Scope:** Art direction for the **Crete vertical slice** (GDD §17.2). It runs in parallel with the combat milestones (M5b onward) and does not touch `game/` until the team approves a style test.
> **Sources:** `GDD.md` (latest version on `prototype/combat`), §1, §7, §10, §15, §16, §17. PRD (NFR-6, FR-24/25 telegraphs). Architecture Spine (Pixel Perfect setup, LFS, "no Animator for combat timing"). The two concept images in `images/`.

---

## 0. TL;DR

1. **Recommended direction: A, "Minoan Fresco Daylight"**, with UI and cutscene framing borrowed from B (pottery) and only the glow VFX borrowed from C. Details in §2.
2. **Camera: 3/4 top-down, as in Wizard of Legend. Not isometric.** The concept images in `images/` are isometric and far more detailed than 32×32 at 640×400 can show. Treat them as **mood only**, not as a style target (§1.2).
3. **The real constraint is hours, not style.** The vertical slice needs about **185–220 hours of art** if everything is made in-house (§7). At ~3 h/week *total* for the whole team, art alone would take 1.5–3 years. The single most important decision is **who makes the art and with what budget** (Q1, Q10).
4. **Frame economy:** heroes ~48 frames, enemies ~20 frames each, 2 drawn directions (side + mirror) for the vertical slice (§4).
5. **AI:** use it for internal placeholders and concept work only, with an `AI_PLACEHOLDER_` prefix and a manifest. **Nothing AI-made ships**, and nothing AI-made appears in a public build (Steam Playtest and the Next Fest demo are public builds) without a Steam disclosure (§6).
6. **First art step:** a ~10–12 h **style test** (Heracles idle/run/attack + Minotaur-man + one mock Crete room, in 2 palettes), checked on a real Steam Deck screen before anything else is produced (§8).

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

---

## 5. Tool pipeline: Aseprite to Unity

### 5.1 Tools

- **Aseprite** (~$20, or free if built from source). Alternative: Pixelorama (free, open source). Use one tool for both team members.
- **Lospec** for the palette (`.gpl` or `.ase` palette file committed under `docs/art/palettes/`).
- **Unity 2D Aseprite Importer** (`com.unity.2d.aseprite` 6.0.0, already installed).
- No AI upscalers or "pixelize" filters on final art. They break the grid and the palette.

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

## 6. AI-assisted placeholder vs hand-made

### 6.1 Rules

| Use | AI allowed? | Notes |
| --- | --- | --- |
| Mood boards, concept exploration, color studies | ✅ Yes (internal) | Like the current `images/`. Never on the store page. |
| Item icon placeholders during vertical slice development | ✅ Yes, temporary | Downscale and quantize to the master palette so they don't mislead playtesters about readability. |
| Codex illustration drafts, village background block-out | ✅ Yes, temporary | Composition only. Replaced by hand-made art. |
| Character sprites and animation | ❌ No | AI does not keep a consistent pixel grid and palette across frames. This is also pillar 2, the marketing hook. |
| Tiles and props | ❌ No | Must be grid-exact and palette-exact. |
| Final UI, logo, Steam capsule, key art, trailer frames | ❌ No | Commission or hand-make. It is the store page. |
| Scripts that quantize, slice or atlas (not generative) | ✅ Yes | Not "AI content". |

### 6.2 Tracking (mirrors the existing music convention)

The Spine already uses `AI_PLACEHOLDER_*` plus `manifest.md` for ACE-Step music. Proposal: the same for art.

- Files: `game/Assets/Art/Placeholder/AI/AI_PLACEHOLDER_<name>.png`.
- `manifest.md` next to them: tool, prompt, date, which final asset will replace it.
- **Gate before every public build** (Steam Playtest, Next Fest demo, store-page screenshots and trailer): an editor check or `BannedApiTests`-style test that fails if a build scene references any `AI_PLACEHOLDER_` asset. *(Proposal only. Not added to `game/`.)*

### 6.3 Steam disclosure

Since January 2024, Valve's Content Survey asks developers to **disclose AI-generated content** that is *pre-generated* (made during development and present in the game, store page or marketing) or *live-generated* at runtime. The disclosure is shown on the store page. Practical consequences:

- If **no** AI-generated art or music is in the shipped game, demo, playtest build or store page, there is nothing to disclose for art. That is the recommended target.
- **Steam Playtest and the Next Fest demo are public builds.** Placeholders must be gone by then, or disclosed.
- The concept images in `images/` must not be used as store screenshots or capsule art.
- Check the **current wording of the Content Survey** when filling it in, because Valve has adjusted it over time.
- Side note: purely AI-generated images have weak or no copyright protection in several jurisdictions. That is one more reason the capsule and key art should be human-made.

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

---

## 8. Proposed art milestones (parallel to the combat gates)

Each one ends with something you can look at in the game, following the GDD's milestone rule.

| Gate | Content | Hours | Exit check |
| --- | --- | --- | --- |
| **A0** | This discussion. Answer §9. Lock the master palette | 1 | Questionnaire answered, GDD §15 diff approved |
| **A1 Style test** | Heracles idle + run + Club Strike, Minotaur-man idle + run + telegraph, one 40×25-tile mock Crete room, in **2 palette options** | 10–12 | Screenshot at 640×400 and on a **real Steam Deck** screen. Can a non-team viewer find the hero and the enemy within 1 second? |
| **A2 Combat cast** | Full Heracles set + 3 enemies + core VFX, swapped in for the placeholder shapes in the prototype arena | 40–50 | Playtest: telegraphs are still readable (PRD SM metrics don't get worse than with placeholders) |
| **A3 Crete environment** | Tiles, props, traps, doors, 3 sub-area variants | ~30 | One room of each type is recognizable from its look alone |
| **A4 Bosses** | Medusa, Talos, Minotaur (in-house or commissioned) | ~70 or $ | |
| **A5 Village, UI, icons** | Village screen, HUD, meander kit, 20–30 item icons | ~35 | |
| **A6 Store art** | Capsule, key art, trailer shots | commissioned | Needed before GDD §17.3 step 5 (Steam Coming Soon page) |

A1 is the only one that should start now. It is cheap, and it replaces every estimate in §7 with real numbers.

---

## 9. Decision questionnaire

Answer in the one-hour discussion. Each question has a recommendation.

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

Against `GDD.md` §15 on `prototype/combat` (verified with `git apply --check`; the same patch is in [`GDD-section15.diff`](GDD-section15.diff)). The values shown are the **recommendations**. Edit them to match the questionnaire answers before applying.

```diff
@@ -406,15 +406,24 @@ Hades כבר עשה roguelike במיתולוגיה היוונית עם ברכו
 
 | נושא | החלטה |
 | --- | --- |
-| סגנון | **Pixel Art מודרני**, מופשט אך עם פרטים קטנים. קנה מידה וזווית בהשראת Wizard of Legend |
+| סגנון | **Pixel Art מודרני**, מופשט אך עם פרטים קטנים. קנה מידה וזווית בהשראת Wizard of Legend. ✅ **כיוון:** "פרסקו מינואי" במשחק עצמו, מוטיבים של כדי חרס (דמויות שחורות / אדומות) בממשק, בקודקס ובסצנות, וזוהר רק באפקטים (ראו `docs/art/ART-DIRECTION.md`) |
+| זווית מצלמה | ✅ **3/4 מלמעלה** (כמו Wizard of Legend), לא איזומטרי. תמונות הקונספט ב-`images/` הן השראה לאווירה בלבד |
 | רזולוציה | **דמויות 32×32** |
-| צבע | **פלטה מובחנת לכל אזור** |
+| גודל קנבס ואריחים | ✅ גוף 32×32 בתוך קנבס 64×64 (בוסים: גוף 64×64), PPU 32, אריחים 16×16 |
+| צבע | **פלטה מובחנת לכל אזור**. ✅ פלטת אב אחת (~46 צבעים, נקודת פתיחה: Apollo מ-Lospec), וכל אזור משתמש בתת-קבוצה שלה |
+| קו מתאר | ✅ קו כהה צבעוני ברוחב פיקסל לדמויות ולחפצים. בלי קו מתאר בסביבה |
+| כיווני דמות | ✅ 2 כיוונים (צד + שיקוף) ב-Vertical Slice. כיוון התקיפה מוצג באפקטים. לבדוק שוב אחרי פלייטסט |
+| תקציב אנימציה | ✅ "חסכוני": כ-48 פריימים לגיבור, כ-20 לאויב. ליטוש מלא רק לרגעי הטריילר (Ultimate, DASH, המינוטאור) |
+| תאורה | ✅ אורות 2D פשוטים, בלי Normal Maps. Bloom רק על אפקטים |
+| AI בגרפיקה | ✅ רק Placeholder פנימי (`AI_PLACEHOLDER_` + manifest). שום דבר שנוצר ב-AI לא נכנס לבילד ציבורי או לעמוד ה-Steam |
 | מוזיקה | **כלים יווניים עתיקים** + אורקסטרה, ו**אלקטרוני / סינת'** |
 | מוזיקה דינמית | **כן, חלק מהזהות.** משתנה לפי עוצמת הקרב |
 | קול | **קריין לסרטונים בלבד** |
 
 הדגש הוויזואלי: **הדמויות המיתולוגיות** (בוסים, גיבורים, אויבים) צריכות להיות מזוהות מיד, ומונפשות בצורה שגורמת לשחקן לרצות לראות אותן.
 
+❓ **פתוח:** מי מכין את הגרפיקה, כמה מתוך 3 השעות השבועיות הולכות אליה, ומה תקציב ההזמנות מפרילנסרים (בוסים, Capsule). הערכה: כ-220 שעות גרפיקה ל-Vertical Slice אם הכול נעשה בבית (`docs/art/ART-DIRECTION.md` §7).
+
 ---
 
 ## 16. טכני ופלטפורמות
```

A second, smaller suggestion for GDD §17.3: add **"A1 style test"** right after step 2 (combat prototype), and **"store art (capsule, key art)"** as a prerequisite of step 5 (Steam page).

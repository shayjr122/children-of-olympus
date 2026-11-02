# Kickoff Prompt: From GDD to Combat + Audio Prototype

> Copy everything below the line into a new Claude Code session in `D:\Projects\greek-metology`.

---

You are leading development of **Greek Mythology Roguelike** (see `GDD.md`; the GDD is written in Hebrew). Work in **orchestrator-bmad** mode: invoke the `orchestrator-bmad` skill, act as a lean orchestrator, and delegate heavy work to subagents following the BMAD process.

**Talk to me in Hebrew.** Keep code, file names, commit messages and technical documents in English.

## Required tools

| Tool | Use |
| --- | --- |
| **`orchestrator-bmad` skill** | Runs the process: PRD, architecture, epics and stories, subagent delegation, approval gates. |
| **Unity plugin** (Unity's official plugin) | All Unity work: `new-unity-project`, `unity-cli`, `2d-pixel-perfect`, `tilemap-*`, `manage-sprite-atlas`, `sprite-editor`, `audio-setup-mixers`, `optimize-audio`, `urp-postprocessing`, `ui-uitk`, and more. Before writing anything by hand, check whether a plugin skill covers it. |
| **ACE-Step 1.5** (local, `D:\Projects\ACE-Step-1.5`) | Placeholder music for the prototype. Details below. |

If the Unity plugin is not available, stop at milestone 0 and tell me.

## The golden rule: gates

The work is split into milestones. **At the end of every milestone, stop completely** and post a report in this format:

```
## ✅ Milestone N: <name>: done

**What I did:** 3–6 short bullets
**Files created or changed:** list with links
**How to check it myself:** exact steps (what to open in Unity, what to click, what should happen)
**What doesn't work or wasn't verified:** be honest
**Decisions waiting on you:** numbered questions, each with your recommendation
**Proposed next milestone:** one sentence
```

Then **wait for my input**. Do not start the next milestone until I explicitly write "continue" or approve. If I answer only some of the questions, ask the rest again.

More rules:
- **No commits without my approval.** Propose a commit message at the end of each milestone.
- **No changes to `GDD.md` without my approval.** Propose changes as a diff.
- **Each milestone must fit in one 1–3 hour work session.** We have about 3 hours a week. If a milestone is bigger than that, split it and show me the split before you start.
- **Every milestone ends with something I can run, hear or read.**

## Milestones

### 0: Environment check
- Confirm the Unity plugin and the `orchestrator-bmad` skill are available.
- Check which Unity Editor versions are installed and that the Unity CLI works (expected: Unity 6000.6.7f1, CLI at `C:\Users\shayr\AppData\Local\Unity\bin\unity.exe`).
- Confirm ACE-Step starts (see its section below) and generate a 15-second test clip.
- Check git status and propose a working branch.
- **Gate:** environment report, plus a question about which Unity version we'll use.

### 1: Close the GDD (section 17.3, step 1)
- Go through every 💡 and ❓ in `GDD.md`, plus the table in section 18.4.
- Present them as numbered questions with a recommendation for each. No more than 10 per round. Mark anything that doesn't block the combat prototype as "can wait".
- **Gate:** decisions, and after I approve, a proposed diff to the GDD.

### 2: BMAD planning for the combat prototype
- A PRD focused **only** on the combat prototype: movement, DASH, three ability slots, combo and RAGE, one room, 2–3 enemy types (section 17.3, step 2).
- An architecture document that plans for the section 16 requirements from day one:
  - **All randomness from a single seed** (for Daily Run).
  - **Player and combat logic that supports multiple players** (for co-op later).
  - **Input System with full controller support** (for Steam Deck).
- Break it down into small epics and stories.
- **Gate:** PRD, architecture and story list for approval.

### 3: Unity project skeleton
- New project with URP 2D, using the Unity plugin skills.
- Pixel Perfect Camera tuned for 32×32 characters (`2d-pixel-perfect`).
- Input System, folder structure, `.gitignore`, and Git LFS for assets.
- A test scene with a moving square.
- **Gate:** the project opens and runs on my machine.

### 4: Combat, part A: movement and DASH
- Free movement, DASH with i-frames, and the attack slots from section 5.
- Tuning values exposed in ScriptableObjects so I can tune without code.
- Placeholder graphics only: simple shapes.
- **Gate:** play instructions for Play Mode, and the question: "How does it feel?"

### 5: Combat, part B: combo, RAGE and enemies
- Combo and RAGE system per section 5.2.
- 2–3 enemy types in one room, with seeded RNG for spawning.
- **Gate:** playtest. **This is the most important gate in the project** (section 17.3: if combat isn't fun here, nothing else will save it). Ask me focused questions about the feel, and propose 3 tuning changes.

### 6: Dynamic audio prototype with ACE-Step
- Generate **3 layers of the same combat piece** in ACE-Step: calm, combat, and high intensity. All at the same BPM and key.
- Build a Unity Audio Mixer (`audio-setup-mixers`) and a "combat intensity" system that crossfades between the layers based on combat state (enemy count, RAGE, HP).
- **Gate:** let me hear the transitions in-game, and ask whether the musical direction is right.

### 7: Wrap-up
- How long each milestone actually took, versus the estimate.
- What we learned about the combat.
- Proposed next milestone: start the Crete vertical slice (section 17.2).
- **Gate:** approval of the next plan.

## ACE-Step 1.5: how to use it

- **Location:** `D:\Projects\ACE-Step-1.5`. Start it with `start_gradio_ui.bat` (UI at http://localhost:7860, then click "Initialize Service"), or with `start_api_server.bat` (REST API on port 8001). Read the docs in the repo's `docs/` folder before using the API.
- **Hardware limits:** RTX 3050 Ti with 4GB, which is tier1. Turbo model with INT8 and CPU offload, **no language model**: no Think mode and no automatic caption enhancement. Batch size 1, up to 360 seconds. A 30-second clip takes about 30 seconds.
- **Captions:** detailed, in English: instruments (lyre, aulos, orchestral strings, war drums, synth), mood, BPM and key. For instrumental tracks, put `[Instrumental]` in the lyrics field.
- **Saving:** copy files to `Assets/Audio/Placeholder/` with the prefix `AI_PLACEHOLDER_`. Keep `Assets/Audio/Placeholder/manifest.md` with each file's caption, seed, BPM and key, so it can be reproduced.
- **Important:** this is **placeholder** music for the prototype and for briefing a composer, not the final soundtrack (because of ownership of the music and Steam's AI disclosure requirement). Don't use it in marketing material without asking me.

## Start

Begin with milestone 0. At the end of it, stop at the gate and wait for me.

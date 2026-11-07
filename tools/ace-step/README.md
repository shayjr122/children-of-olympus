# ACE-Step 1.5 tools (placeholder music)

Local tools for generating **AI placeholder** music with [ACE-Step 1.5](https://github.com/ace-step/ACE-Step-1.5),
installed at `D:\Projects\ACE-Step-1.5` (own venv in `.venv`). Used for story 10.1 (M6-pre).

- `run_api_int8.py`: starts the ACE-Step REST API on `127.0.0.1:8001` with INT8 weight-only quantization.
  The stock API server has no INT8 option, so the launcher patches the default; the ACE-Step repo is left untouched.
- `generate.py`: standard-library client. Submits one job, polls, downloads the WAV. `--src` turns the job into a cover task.

## Hardware profile (tier1: RTX 3050 Ti, 4 GB)

Turbo DiT, INT8, CPU offload, **no language model**, batch size 1.
Model load takes about 32 s; a 15 s clip takes about 19 s.

## Start the server

PowerShell:

```powershell
$env:ACESTEP_INIT_LLM = "false"
$env:ACESTEP_OFFLOAD_TO_CPU = "true"
$env:ACESTEP_OFFLOAD_DIT_TO_CPU = "true"   # REQUIRED on 4 GB: without it the VAE decode falls back to CPU and hangs 20+ min
$env:ACESTEP_CONFIG_PATH = "acestep-v15-turbo"
D:\Projects\ACE-Step-1.5\.venv\Scripts\python.exe tools\ace-step\run_api_int8.py
```

Wait until `GET http://127.0.0.1:8001/health` answers. Stop the server (Ctrl+C) when done so the GPU is free.

## API flow

1. `POST /release_task` with a JSON body returns `data.task_id`.
2. Poll `POST /query_result` with `{"task_id_list": ["<id>"]}` until the item's `status` is `1` (`0` = running).
3. The item's `result` is a JSON string; its `file` field is a URL like `/v1/audio?path=...`. `GET` it from `http://127.0.0.1:8001`.

Request fields to keep fixed on this hardware:

```json
{
  "prompt": "<detailed English caption>",
  "lyrics": "[Instrumental]",
  "audio_duration": 60, "bpm": 128, "keyscale": "D minor", "time_signature": "4",
  "thinking": false, "use_format": false, "use_cot_caption": false, "use_cot_language": false,
  "inference_steps": 8, "batch_size": 1,
  "use_random_seed": false, "seed": 42,
  "audio_format": "wav"
}
```

Cover task (re-voice an existing clip, keep its structure): add `"task_type": "cover"`,
`"src_audio_path": "<absolute path>"` and `"audio_cover_strength": 0.0-1.0` (higher = closer to the source).

## Example

```powershell
python tools\ace-step\generate.py req_combat.json combat.wav
python tools\ace-step\generate.py req_calm.json calm.wav --src D:\path\to\combat.wav
```

Outputs are AI placeholders: see `game/Assets/Audio/Placeholder/manifest.md` for the usage policy.

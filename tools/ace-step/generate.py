"""Minimal ACE-Step 1.5 REST client: submit one job, wait, download the WAV.

Usage:
    python generate.py <request.json> <out.wav> [--src <abs path to source wav>]

The request JSON holds ACE-Step /release_task fields (prompt, bpm, keyscale, seed...).
With --src the job becomes a cover task (task_type=cover, src_audio_path=<src>),
which keeps the source's structure and timing while re-voicing it from the prompt.
Only the standard library is used, so any Python 3.9+ works.
"""
import json, sys, time, urllib.parse, urllib.request

BASE = "http://127.0.0.1:8001"

def post(path, body):
    req = urllib.request.Request(BASE + path, data=json.dumps(body).encode(),
                                 headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.load(r)

def main():
    args = sys.argv[1:]
    src = None
    if "--src" in args:
        i = args.index("--src"); src = args[i + 1]; del args[i:i + 2]
    req_path, out_path = args
    body = json.load(open(req_path, encoding="utf-8"))
    if src:
        body["task_type"] = "cover"
        body["src_audio_path"] = src
    t0 = time.time()
    task_id = post("/release_task", body)["data"]["task_id"]
    print("task", task_id, flush=True)
    while True:
        time.sleep(3)
        item = post("/query_result", {"task_id_list": [task_id]})["data"][0]
        if item["status"] == 1:
            break
        if item["status"] not in (0,):
            sys.exit(f"task failed: {json.dumps(item)[:2000]}")
    result = item["result"]
    result = json.loads(result) if isinstance(result, str) else result
    first = result[0] if isinstance(result, list) else result
    file_url = first["file"]
    url = file_url if file_url.startswith("http") else BASE + file_url
    urllib.request.urlretrieve(url, out_path)
    print(f"saved {out_path} in {time.time() - t0:.1f}s", flush=True)

if __name__ == "__main__":
    main()

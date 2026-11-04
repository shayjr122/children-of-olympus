#!/usr/bin/env python3
"""Pixel-art consistency checker for the Greek Mythology Roguelike.

Checks exported sprites against the art rules in docs/art/ART-DIRECTION.md (section 6.5):
palette compliance, hard alpha, canvas size, feet baseline, body height drift,
sel-out outline, color count, orphan pixels and frame budgets.

Usage:
  python tools/art/check_sprites.py check --category character Heracles.png [--data Heracles.json]
  python tools/art/check_sprites.py check --category icon icons/*.png
  python tools/art/check_sprites.py lineup --out docs/art/golden-lineup.png Heracles.png MinotaurMan.png

Input is either a single-frame PNG, or a sprite sheet exported by Aseprite with its JSON:
  aseprite -b Heracles.aseprite --sheet Heracles.png --data Heracles.json --format json-array --list-tags
Without --data, a PNG next to the image with the same stem and .json extension is used if it exists.
Exit code: 0 = no errors, 1 = errors found, 2 = bad usage / missing files.
Requires Pillow.
"""
from __future__ import annotations

import argparse
import json
import statistics
import sys
from dataclasses import dataclass, field
from pathlib import Path

try:
    from PIL import Image
except ImportError:  # pragma: no cover
    sys.exit("Pillow is required: python -m pip install pillow")

REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_RULES = REPO_ROOT / "tools" / "art" / "art-rules.json"


# ---------------------------------------------------------------- palette

def load_palette(path: Path) -> set[tuple[int, int, int]]:
    """Read a Lospec .hex (one RRGGBB per line) or GIMP .gpl palette."""
    if not path.exists():
        raise FileNotFoundError(
            f"Palette not found: {path}\n"
            "Download the Apollo palette from lospec.com/palette-list/apollo as .hex "
            "and save it there (see docs/art/ART-DIRECTION.md section 6.5)."
        )
    colors: set[tuple[int, int, int]] = set()
    text = path.read_text(encoding="utf-8", errors="replace")
    if path.suffix.lower() == ".gpl":
        for line in text.splitlines():
            parts = line.split()
            if len(parts) >= 3 and all(p.isdigit() for p in parts[:3]):
                colors.add((int(parts[0]), int(parts[1]), int(parts[2])))
    else:
        for line in text.splitlines():
            h = line.strip().lstrip("#")
            if len(h) >= 6:
                try:
                    colors.add((int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)))
                except ValueError:
                    continue
    if not colors:
        raise ValueError(f"No colors read from palette {path}")
    return colors


def hexc(c: tuple[int, int, int]) -> str:
    return "#%02x%02x%02x" % c


def luminance(c: tuple[int, int, int]) -> float:
    r, g, b = (v / 255.0 for v in c)
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


# ---------------------------------------------------------------- frames

@dataclass
class Frame:
    index: int
    tag: str | None
    img: Image.Image  # RGBA, cropped to the frame rect


def load_frames(image_path: Path, data_path: Path | None) -> tuple[list[Frame], dict[str, int]]:
    sheet = Image.open(image_path).convert("RGBA")
    if data_path is None:
        candidate = image_path.with_suffix(".json")
        data_path = candidate if candidate.exists() else None
    if data_path is None:
        return [Frame(0, None, sheet)], {}

    data = json.loads(data_path.read_text(encoding="utf-8"))
    raw = data.get("frames", [])
    if isinstance(raw, dict):  # Aseprite "hash" format
        raw = list(raw.values())
    rects = [f["frame"] for f in raw]

    tag_of: dict[int, str] = {}
    tag_counts: dict[str, int] = {}
    for t in data.get("meta", {}).get("frameTags", []):
        tag_counts[t["name"]] = t["to"] - t["from"] + 1
        for i in range(t["from"], t["to"] + 1):
            tag_of.setdefault(i, t["name"])

    frames = []
    for i, r in enumerate(rects):
        box = (r["x"], r["y"], r["x"] + r["w"], r["y"] + r["h"])
        frames.append(Frame(i, tag_of.get(i), sheet.crop(box)))
    return frames, tag_counts


# ---------------------------------------------------------------- checks

@dataclass
class Report:
    name: str
    errors: list[str] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)

    def err(self, msg: str) -> None:
        self.errors.append(msg)

    def warn(self, msg: str) -> None:
        self.warnings.append(msg)


def opaque_mask(img: Image.Image):
    w, h = img.size
    px = img.load()
    return w, h, px


def bbox_of(img: Image.Image):
    return img.getchannel("A").getbbox()  # (l, t, r, b) or None


def frame_label(f: Frame) -> str:
    return f"frame {f.index}" + (f" [{f.tag}]" if f.tag else "")


def check_frame(f: Frame, rules: dict, palette: set, subset: set | None, rep: Report) -> None:
    w, h, px = opaque_mask(f.img)
    label = frame_label(f)

    canvas = rules.get("canvas")
    if canvas and (w, h) != tuple(canvas):
        rep.err(f"{label}: canvas {w}x{h}, expected {canvas[0]}x{canvas[1]}")

    off_palette: dict[tuple, int] = {}
    off_subset: dict[tuple, int] = {}
    soft_alpha = 0
    colors: set = set()
    opaque = set()
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            if a != 255:
                soft_alpha += 1
            c = (r, g, b)
            colors.add(c)
            opaque.add((x, y))
            if c not in palette:
                off_palette[c] = off_palette.get(c, 0) + 1
            elif subset is not None and c not in subset:
                off_subset[c] = off_subset.get(c, 0) + 1

    if soft_alpha:
        rep.err(f"{label}: {soft_alpha} semi-transparent pixels (alpha must be 0 or 255)")
    if off_palette:
        top = sorted(off_palette.items(), key=lambda kv: -kv[1])[:5]
        rep.err(f"{label}: {sum(off_palette.values())} pixels in {len(off_palette)} non-palette colors, "
                f"e.g. " + ", ".join(f"{hexc(c)}x{n}" for c, n in top))
    if off_subset:
        top = sorted(off_subset.items(), key=lambda kv: -kv[1])[:5]
        rep.err(f"{label}: {len(off_subset)} colors outside the region subset, e.g. "
                + ", ".join(hexc(c) for c, _ in top))

    max_colors = rules.get("max_colors")
    if max_colors and len(colors) > max_colors:
        rep.warn(f"{label}: {len(colors)} colors (limit {max_colors})")

    if not opaque:
        rep.warn(f"{label}: empty frame")
        return

    def is_opaque(x, y):
        return (x, y) in opaque

    if rules.get("outline"):
        edge = [(x, y) for (x, y) in opaque
                if any(not is_opaque(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
        thr = rules.get("outline_light_threshold", 0.45)
        light = [p for p in edge if luminance(px[p][:3]) > thr]
        ratio = len(light) / len(edge) if edge else 0.0
        if ratio > rules.get("outline_max_light_ratio", 0.15):
            rep.err(f"{label}: {ratio:.0%} of edge pixels are light (sel-out outline missing or broken)")

    if rules.get("orphan_check", True):
        orphans = sum(
            1 for (x, y) in opaque
            if not any(is_opaque(x + dx, y + dy)
                       for dx in (-1, 0, 1) for dy in (-1, 0, 1) if dx or dy))
        if orphans:
            rep.warn(f"{label}: {orphans} orphan pixel(s) with no opaque neighbor")

    rng = rules.get("body_height_range")
    if rng:
        l, t, r, b = bbox_of(f.img)
        bh = b - t
        if f.tag not in set(rules.get("height_exempt_tags", [])) and not (rng[0] <= bh <= rng[1]):
            rep.warn(f"{label}: body height {bh}px outside the model range {rng[0]}-{rng[1]}px")


def check_animation(frames: list[Frame], rules: dict, rep: Report) -> None:
    by_tag: dict[str, list[Frame]] = {}
    for f in frames:
        if f.tag:
            by_tag.setdefault(f.tag, []).append(f)

    def base(tag: str) -> str:
        return tag.split(".")[0]

    if rules.get("baseline_check"):
        exempt = set(rules.get("baseline_exempt_tags", []))
        for tag, fs in by_tag.items():
            if base(tag) in exempt or tag in exempt:
                continue
            bottoms = {f.index: bbox_of(f.img)[3] for f in fs if bbox_of(f.img)}
            if len(set(bottoms.values())) > 1:
                rep.err(f"tag '{tag}': feet baseline moves between frames (bottom rows "
                        + ", ".join(f"{i}:{b}" for i, b in bottoms.items()) + ")")

    tol = rules.get("height_tolerance_px")
    if tol is not None:
        exempt = set(rules.get("height_exempt_tags", []))
        for tag, fs in by_tag.items():
            if base(tag) in exempt or tag in exempt or len(fs) < 2:
                continue
            heights = {f.index: (lambda bb: bb[3] - bb[1])(bbox_of(f.img)) for f in fs if bbox_of(f.img)}
            med = statistics.median(heights.values())
            bad = {i: hgt for i, hgt in heights.items() if abs(hgt - med) > tol}
            if bad:
                rep.warn(f"tag '{tag}': body height drifts more than {tol}px from median {med:g} in frames "
                         + ", ".join(f"{i}:{hgt}" for i, hgt in bad.items()))


def check_budget(name: str, tag_counts: dict[str, int], budgets: dict, rep: Report) -> None:
    budget = budgets.get(name)
    if not budget or not tag_counts:
        return
    for tag, want in budget.items():
        if tag.startswith("_"):
            continue
        have = tag_counts.get(tag)
        if have is None:
            rep.warn(f"budget: tag '{tag}' missing (expected {want} frames)")
        elif have != want:
            rep.warn(f"budget: tag '{tag}' has {have} frames, animation sheet says {want}")


# ---------------------------------------------------------------- commands

def cmd_check(args) -> int:
    rules_all = json.loads(Path(args.rules).read_text(encoding="utf-8"))
    cats = rules_all["categories"]
    if args.category not in cats:
        print(f"Unknown category '{args.category}'. Known: {', '.join(cats)}", file=sys.stderr)
        return 2
    rules = cats[args.category]
    try:
        palette = load_palette(Path(args.palette) if args.palette else REPO_ROOT / rules_all["palette"])
        subset = load_palette(Path(args.subset)) if args.subset else None
    except (FileNotFoundError, ValueError) as e:
        print(e, file=sys.stderr)
        return 2
    if subset and not subset <= palette:
        print("Region subset contains colors that are not in the master palette: "
              + ", ".join(hexc(c) for c in sorted(subset - palette)), file=sys.stderr)
        return 1

    total_err = 0
    for img in args.images:
        p = Path(img)
        if not p.exists():
            print(f"Missing file: {p}", file=sys.stderr)
            return 2
        data = Path(args.data) if args.data else None
        frames, tag_counts = load_frames(p, data)
        rep = Report(p.name)
        for f in frames:
            check_frame(f, rules, palette, subset, rep)
        if len(frames) > 1:
            check_animation(frames, rules, rep)
        check_budget(p.stem, tag_counts, rules_all.get("frame_budgets", {}), rep)

        status = "FAIL" if rep.errors else ("WARN" if rep.warnings else "OK")
        print(f"[{status}] {p} ({len(frames)} frame(s), category {args.category})")
        for e in rep.errors:
            print(f"   error:   {e}")
        for w in rep.warnings:
            print(f"   warning: {w}")
        total_err += len(rep.errors)
    return 1 if total_err else 0


def cmd_lineup(args) -> int:
    """First frame of each sprite side by side, feet on one baseline, at 1x and a scaled copy."""
    firsts = []
    for img in args.images:
        frames, _ = load_frames(Path(img), None)
        im = frames[0].img
        bb = bbox_of(im)
        if bb:
            firsts.append(im.crop(bb))
    if not firsts:
        print("Nothing to line up", file=sys.stderr)
        return 2
    gap = 4
    height = max(i.height for i in firsts) + 2 * gap
    width = sum(i.width for i in firsts) + gap * (len(firsts) + 1)
    bg = tuple(int(args.background.lstrip("#")[i:i + 2], 16) for i in (0, 2, 4)) + (255,)
    sheet = Image.new("RGBA", (width, height), bg)
    x = gap
    for i in firsts:
        sheet.alpha_composite(i, (x, height - gap - i.height))
        x += i.width + gap
    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out)
    scaled = sheet.resize((width * args.scale, height * args.scale), Image.NEAREST)
    scaled_path = out.with_name(out.stem + f"@{args.scale}x" + out.suffix)
    scaled.save(scaled_path)
    print(f"Wrote {out} and {scaled_path}")
    return 0


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)

    c = sub.add_parser("check", help="check sprites against the art rules")
    c.add_argument("images", nargs="+")
    c.add_argument("--category", required=True)
    c.add_argument("--data", help="Aseprite JSON for a single sheet (default: <image>.json if present)")
    c.add_argument("--palette", help="override the master palette path")
    c.add_argument("--subset", help="region sub-palette (.hex/.gpl); every color must also be in the master")
    c.add_argument("--rules", default=str(DEFAULT_RULES))
    c.set_defaults(func=cmd_check)

    l = sub.add_parser("lineup", help="build the golden lineup image")
    l.add_argument("images", nargs="+")
    l.add_argument("--out", required=True)
    l.add_argument("--scale", type=int, default=4)
    l.add_argument("--background", default="#3a3a3a")
    l.set_defaults(func=cmd_lineup)

    args = ap.parse_args(argv)
    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())

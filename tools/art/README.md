# Art tools

Consistency checks for pixel art (see `docs/art/ART-DIRECTION.md` §6.5).

## One-time setup

1. `python -m pip install pillow`
2. Download the **Apollo** palette from Lospec (`lospec.com/palette-list/apollo`, format `.hex`) and save it as `docs/art/palettes/apollo.hex`. Do not edit it.
3. Optional: region subsets such as `docs/art/palettes/crete.hex`. Every color must also be in `apollo.hex`, and the checker refuses a subset that isn't.

## Export from Aseprite

```bash
aseprite -b Heracles.aseprite --sheet Heracles.png --data Heracles.json --format json-array --list-tags
```

## Check

```bash
python tools/art/check_sprites.py check --category character Heracles.png
```

```bash
python tools/art/check_sprites.py check --category icon --subset docs/art/palettes/crete.hex icons/*.png
```

Categories: `character`, `boss`, `pickup`, `icon`, `tile`, `prop`, `vfx`. The rules and frame budgets are in `art-rules.json`.
Exit code 0 = no errors, 1 = errors, 2 = usage problem (for example, a missing palette).

## Golden lineup

```bash
python tools/art/check_sprites.py lineup --out docs/art/golden-lineup.png Heracles.png MinotaurMan.png CretanArcher.png
```

This writes the lineup at 1× plus a 4× copy. Regenerate it every 10 approved assets and compare with the previous one.

"""Render a tile with its derived grid and detected terrain, for review.

The reviewer needs to see what the detector decided, in place
on the art, not a grid of glyphs beside a picture. This draws:

  * the exact square grid derived from dimensions.json
  * every square labelled with its (col,row) index
  * detected regions tinted by class and stamped with their glyph
  * a legend, and a warning banner listing regions whose boundary did not close

Output goes to tools/_overlays/<Tile>_overlay.png by default.

Run:  python tools/terrain_overlay.py Core_1A Core_19B Core_33A
      python tools/terrain_overlay.py --all Core
"""

import argparse
import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import terrain_detect as td  # noqa: E402

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_overlays")

# Tint per terrain class. Alpha kept low so the underlying art stays readable —
# the reviewer is checking the detector against the art, so the art must win.
TINT = {
    "d": (40, 110, 255, 70),    # difficult  (printed solid blue)
    "X": (255, 40, 40, 80),     # blocking   (printed solid red)
    "I": (255, 150, 30, 80),    # impassable (printed dashed red)
    "P": (160, 60, 200, 80),    # pit
    "-": (20, 20, 20, 120),     # void
}
LABEL = {"d": "DIFFICULT", "X": "BLOCKING", "I": "IMPASSABLE", "P": "PIT", "-": "VOID"}

GRID_RGBA = (255, 255, 255, 110)
MARGIN_TOP = 46


def render(tile, dims, scale=1.0, out_dir=OUT, clean=False):
    res = td.analyse(tile, dims, verbose=True)
    if not res:
        return None
    exp = tile.split("_")[0]
    src = os.path.join(td.TILES, exp, tile + ".png")
    base = Image.open(src).convert("RGBA")

    w, h = res["wh"]
    # Upscale small tiles so labels stay legible for the reviewer.
    if base.size[0] < 420 or base.size[1] < 420:
        scale = max(scale, 420 / min(base.size))
    if scale != 1.0:
        base = base.resize((int(base.size[0] * scale), int(base.size[1] * scale)), Image.LANCZOS)

    iw, ih = base.size
    px, py = iw / w, ih / h

    canvas = Image.new("RGBA", (iw, ih + MARGIN_TOP), (18, 18, 22, 255))
    canvas.paste(base, (0, MARGIN_TOP))
    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)

    # terrain tints (skipped in clean mode so a semantic review pass is
    # not anchored onto whatever the detector happened to decide)
    for r, row in enumerate([] if clean else res["grid"]):
        for c, g in enumerate(row):
            if g == ".":
                continue
            x0, y0 = c * px, r * py + MARGIN_TOP
            d.rectangle([x0, y0, x0 + px, y0 + py], fill=TINT.get(g, (255, 0, 255, 70)))

    # grid + indices
    for c in range(w + 1):
        d.line([(c * px, MARGIN_TOP), (c * px, ih + MARGIN_TOP)], fill=GRID_RGBA, width=1)
    for r in range(h + 1):
        d.line([(0, r * py + MARGIN_TOP), (iw, r * py + MARGIN_TOP)], fill=GRID_RGBA, width=1)

    for r in range(h):
        for c in range(w):
            x0, y0 = c * px + 3, r * py + MARGIN_TOP + 2
            d.text((x0, y0), f"{c},{r}", fill=(255, 255, 255, 190))
            g = "." if clean else res["grid"][r][c]
            if g != ".":
                d.text((c * px + px / 2 - 4, r * py + MARGIN_TOP + py / 2 - 6), g,
                       fill=(255, 255, 255, 255))

    classes = sorted({g for row in ([] if clean else res["grid"]) for g in row if g != "."})
    legend = "UNMARKED - identify terrain from the art" if clean else ("  ".join(f"{g}={LABEL.get(g, g)}" for g in classes) or "no terrain detected")
    d.text((6, 6), f"{tile}   {w}x{h}   px/sq={res['px']}   {legend}", fill=(255, 255, 255, 255))
    d.text((6, 22), f"red={res['red_frac']}%  blue={res['blue_frac']}%  "
                    f"regions={len(res['detail'])}  " + ("PASS 1: which squares are difficult/blocking/impassable?" if clean else "PASS 2: confirm or correct each tinted square"),
           fill=(200, 200, 210, 255))

    out = Image.alpha_composite(canvas, layer).convert("RGB")
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, f"{tile}_{'clean' if clean else 'overlay'}.png")
    out.save(path)
    return path, res


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("tiles", nargs="*")
    ap.add_argument("--all", metavar="EXPANSION")
    ap.add_argument("--clean", action="store_true",
                    help="grid and indices only, no detector output (semantic first pass)")
    args = ap.parse_args()

    dims = td.load_dimensions()
    names = list(args.tiles)
    if args.all:
        d = os.path.join(td.TILES, args.all)
        names += sorted(f[:-4] for f in os.listdir(d) if f.endswith(".png"))

    for n in names:
        r = render(n, dims, clean=args.clean)
        if not r:
            print(f"{n}: NOT FOUND")
            continue
        path, res = r
        marked = sum(ch != "." for row in res["grid"] for ch in row)
        print(f"{n:<11} -> {path}   marked={marked}/{res['wh'][0]*res['wh'][1]}")
    return 0


if __name__ == "__main__":
    sys.exit(main())

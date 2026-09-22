"""Contact sheets of the edges the detector is unsure about, for review by eye.

Each cell is one square turned so the edge in question runs along its top,
magnified, with the neighbouring squares along that side for context and the
detector's numbers underneath. Reviewing strips instead of whole tiles keeps
the eye on the one thing that matters -- is there a printed line at the cut.

    python tools/wall_strips.py            # ambiguous, used edges -> _overlays/strips/
    python tools/wall_strips.py --all      # every used edge
"""

import json
import os
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
TILES = os.path.join(ROOT, "ImperialCommander2", "Assets", "SagaTiles")
OVER = os.path.join(HERE, "_overlays")
OUT = os.path.join(OVER, "strips")

CELL_W, CELL_H, COLS, ROWS = 400, 135, 3, 8


def rotated_square(im, w, h, c, r, side):
    """The square plus one neighbour either side along the edge, edge on top."""
    W, H = im.size
    pw, ph = W / w, H / h
    if side in ("N", "S"):
        x0, x1 = int((c - 0.5) * pw), int((c + 1.5) * pw)
        y0, y1 = int(r * ph), int((r + 1) * ph)
    else:
        x0, x1 = int(c * pw), int((c + 1) * pw)
        y0, y1 = int((r - 0.5) * ph), int((r + 1.5) * ph)
    crop = im.crop((max(0, x0), max(0, y0), min(W, x1), min(H, y1)))
    # Pad so the target square is always centred whatever was clipped.
    full = Image.new("RGBA", (x1 - x0, y1 - y0), (40, 40, 40, 255))
    full.paste(crop, (max(0, x0) - x0, max(0, y0) - y0))
    return full.rotate({"N": 0, "E": 90, "S": 180, "W": 270}[side], expand=True)


def unsure(m):
    """Verdicts near a threshold, or walls failing a secondary check."""
    if 0.25 < m["cover"] < 0.625:
        return True
    if m["wall"]:
        return m["cover"] < 0.875
    return m["cover"] > 0 or m["ratio"] < 0.8


def main():
    everything = "--all" in sys.argv
    metrics = {f["face"]: f for f in json.load(open(os.path.join(OVER, "wall_metrics.json")))}
    joins = json.load(open(os.path.join(OVER, "wall_joins.json")))

    items = []
    for face, edges in sorted(joins.items()):
        m = metrics[face]
        by = {(tuple(e["sq"]), e["dir"]): e for e in m["edges"]}
        for e in edges:
            me = by[(tuple(e["sq"]), e["dir"])]
            if everything or unsure(me):
                items.append((face, m["w"], m["h"], me, len(e["missions"])))

    os.makedirs(OUT, exist_ok=True)
    per = COLS * ROWS
    for s in range(0, len(items), per):
        sheet = Image.new("RGB", (COLS * CELL_W, ROWS * CELL_H), (25, 25, 25))
        draw = ImageDraw.Draw(sheet)
        for i, (face, w, h, me, nmis) in enumerate(items[s:s + per]):
            cx, cy = (i % COLS) * CELL_W, (i // COLS) * CELL_H
            exp = face.split("_")[0]
            im = Image.open(os.path.join(TILES, exp, face + ".png")).convert("RGBA")
            c, r = me["sq"]
            strip = rotated_square(im, w, h, c, r, me["dir"])
            # Show the top 70% of the square: the edge band and enough inside it.
            sw, sh = strip.size
            strip = strip.crop((0, 0, sw, int(sh * 0.45)))
            scale = min((CELL_W - 10) / strip.size[0], (CELL_H - 40) / strip.size[1])
            strip = strip.resize((int(strip.size[0] * scale), int(strip.size[1] * scale)))
            sheet.paste(strip, (cx + 5, cy + 5))
            # Mark the target square's span along the top edge.
            q = strip.size[0] // 4
            draw.rectangle((cx + 5 + q, cy + 2, cx + 5 + 3 * q, cy + 4), outline=(255, 220, 0))
            draw.text((cx + 6, cy + CELL_H - 30),
                      "%d %s (%d,%d)%s" % (s + i, face, c, r, me["dir"]), fill=(255, 255, 255))
            draw.text((cx + 6, cy + CELL_H - 16),
                      "d=%.0f i=%.0f ratio=%.2f th=%d/%d cov=%.2f %s  in %d missions"
                      % (me["dark"], me["inner"], me["ratio"], me["thick"], me["t"], me["cover"], "WALL" if me["wall"] else "open", nmis),
                      fill=(255, 200, 120) if me["wall"] else (150, 220, 150))
        sheet.save(os.path.join(OUT, "strip_%02d.png" % (s // per)))
    print("%d edges on %d sheets in %s" % (len(items), (len(items) + per - 1) // per, OUT))
    return 0


if __name__ == "__main__":
    sys.exit(main())

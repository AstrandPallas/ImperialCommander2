"""Draw the wall verdicts on a tile face, for checking against the art.

    python tools/wall_overlay.py Core_7A Core_5A     # -> tools/_overlays/walls/<face>.png
"""

import json
import os
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
TILES = os.path.join(ROOT, "ImperialCommander2", "Assets", "SagaTiles")
OUT = os.path.join(HERE, "_overlays", "walls")
TARGET = 720


def main():
    metrics = {f["face"]: f for f in json.load(open(os.path.join(HERE, "_overlays", "wall_metrics.json")))}
    os.makedirs(OUT, exist_ok=True)
    for face in sys.argv[1:]:
        m = metrics[face]
        exp = face.split("_")[0]
        im = Image.open(os.path.join(TILES, exp, face + ".png")).convert("RGB")
        scale = TARGET / max(im.size)
        im = im.resize((int(im.size[0] * scale), int(im.size[1] * scale)))
        pw, ph = im.size[0] / m["w"], im.size[1] / m["h"]
        draw = ImageDraw.Draw(im)
        for r in range(m["h"]):
            for c in range(m["w"]):
                draw.rectangle((c * pw, r * ph, (c + 1) * pw, (r + 1) * ph), outline=(255, 255, 0))
                draw.text((c * pw + 4, r * ph + 4), "%d,%d" % (c, r), fill=(255, 255, 0))
        for e in m["edges"]:
            c, r = e["sq"]
            x0, y0, x1, y1 = c * pw, r * ph, (c + 1) * pw, (r + 1) * ph
            seg = {"N": (x0, y0, x1, y0), "S": (x0, y1, x1, y1),
                   "W": (x0, y0, x0, y1), "E": (x1, y0, x1, y1)}[e["dir"]]
            col = (255, 40, 40) if e["wall"] else (40, 255, 40)
            draw.line(seg, fill=col, width=6)
            lx = (seg[0] + seg[2]) / 2 + (8 if e["dir"] == "W" else -30 if e["dir"] == "E" else -10)
            ly = (seg[1] + seg[3]) / 2 + (8 if e["dir"] == "N" else -14 if e["dir"] == "S" else -6)
            draw.text((lx, ly), "%.2f" % e["cover"], fill=col)
        im.save(os.path.join(OUT, face + ".png"))
        print(face, im.size)
    return 0


if __name__ == "__main__":
    sys.exit(main())

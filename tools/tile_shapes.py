"""Extract tile playable shapes from the art's alpha channel.

Imperial Assault tiles are not all rectangles. dimensions.json gives a BOUNDING
BOX; the real playable shape can be smaller, and the difference matters because
in IA the tile's shape IS the wall -- a figure cannot walk off the playable area.

The artists already encoded that shape: non-rectangular tiles ship as RGBA with
the unplayable region fully transparent. So the shape needs no detector, no
threshold and no review. Tiles saved as RGB have no cut-outs and are fully
playable.

Run:  python tools/tile_shapes.py            # summary over all expansions
      python tools/tile_shapes.py Core_25B   # one tile
"""

import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import terrain_detect as td  # noqa: E402

ALPHA_CUT = 128
VOID_FRACTION = 0.5


def shape_of(tile, dims):
    """Row strings using the terrain glyphs: '.' playable, '-' void."""
    exp, rest = tile.split("_")
    tid = rest[:-1]
    path = os.path.join(td.TILES, exp, tile + ".png")
    if not os.path.exists(path) or (exp, tid) not in dims:
        return None
    w, h = dims[(exp, tid)]
    im = Image.open(path)
    if im.mode not in ("RGBA", "LA") and "transparency" not in im.info:
        return ["." * w for _ in range(h)]      # no cut-outs
    a = np.asarray(im.convert("RGBA"))[:, :, 3]
    px, py = im.size[0] / w, im.size[1] / h
    rows = []
    for r in range(h):
        row = ""
        for c in range(w):
            sub = a[int(r * py):int((r + 1) * py), int(c * px):int((c + 1) * px)]
            row += "-" if float((sub < ALPHA_CUT).mean()) > VOID_FRACTION else "."
        rows.append(row)
    return rows


def all_shapes(dims):
    out = {}
    for exp in sorted(os.listdir(td.TILES)):
        d = os.path.join(td.TILES, exp)
        if not os.path.isdir(d):
            continue
        for f in sorted(os.listdir(d)):
            if not f.endswith(".png"):
                continue
            name = f[:-4]
            s = shape_of(name, dims)
            if s:
                out[name] = s
    return out


def main():
    dims = td.load_dimensions()
    if len(sys.argv) > 1:
        for t in sys.argv[1:]:
            s = shape_of(t, dims)
            print(t)
            for row in s or []:
                print("   ", row)
        return 0

    shapes = all_shapes(dims)
    irregular = {k: v for k, v in shapes.items() if any("-" in r for r in v)}
    total_sq = sum(len(r) for v in shapes.values() for r in v)
    void_sq = sum(r.count("-") for v in shapes.values() for r in v)
    print(f"tile faces        : {len(shapes)}")
    print(f"non-rectangular   : {len(irregular)} ({100.0 * len(irregular) / max(len(shapes),1):.0f}%)")
    print(f"void squares      : {void_sq} of {total_sq} "
          f"({100.0 * void_sq / max(total_sq,1):.1f}%)")
    print("\nexamples:")
    for k in list(irregular)[:6]:
        print(" ", k)
        for row in irregular[k]:
            print("   ", row)
    return 0


if __name__ == "__main__":
    sys.exit(main())

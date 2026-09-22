"""Which outward tile edges ever meet another tile in a shipped mission.

A wall verdict only changes play where two tiles abut: an outward edge with
nothing beyond it has no square to walk into whatever it is painted. So the
edges worth reviewing carefully are the ones that form a JOIN somewhere in the
corpus, and a join is sealed if EITHER side prints a wall.

    python tools/wall_joins.py         # -> tools/_overlays/wall_joins.json, summary
"""

import glob
import json
import os
import sys
from collections import defaultdict

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import corpus_check as cc  # noqa: E402
import tile_shapes as ts  # noqa: E402

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "tools", "_overlays", "wall_joins.json")
STEP = {"N": (0, -1), "S": (0, 1), "W": (-1, 0), "E": (1, 0)}
ORDER = ["N", "E", "S", "W"]


def to_board(lc, lr, x, y, rot):
    rot %= 360
    if rot == 0:
        return (x + lc, y + lr)
    if rot == 90:
        return (x - 1 - lr, y + lc)
    if rot == 180:
        return (x - 1 - lc, y - 1 - lr)
    return (x + lr, y - 1 - lc)


def rotate_dir(d, rot):
    return ORDER[(ORDER.index(d) + (rot % 360) // 90) % 4]


def outward_edges(shape):
    h, w = len(shape), len(shape[0])
    for r in range(h):
        for c in range(w):
            if shape[r][c] == "-":
                continue
            for d, (dc, dr) in STEP.items():
                nc, nr = c + dc, r + dr
                if 0 <= nc < w and 0 <= nr < h and shape[nr][nc] != "-":
                    continue
                yield c, r, d


def main():
    dims = cc.load_dimensions()
    shapes = ts.all_shapes(dims)
    joins = defaultdict(lambda: {"missions": set(), "partners": set()})

    for path in sorted(glob.glob(os.path.join(cc.MISSIONS, "*", "*.json"))):
        d = cc.load_lenient(path)
        mission = os.path.splitext(os.path.basename(path))[0]
        placements = []
        owner = {}
        for section in d.get("mapSections") or []:
            for t in section.get("mapTiles") or []:
                exp = cc.EXPANSIONS[t["expansion"]]
                face = "%s_%s%s" % (exp, t["tileID"], t.get("tileSide", "A"))
                if face not in shapes:
                    continue
                fx, fy = cc.parse_pos(t["entityPosition"])
                x, y, rot = int(fx), int(fy), int(round(t["entityRotation"]))
                placements.append((face, x, y, rot))
                shape = shapes[face]
                for r in range(len(shape)):
                    for c in range(len(shape[0])):
                        if shape[r][c] != "-":
                            owner.setdefault(to_board(c, r, x, y, rot), face)

        for face, x, y, rot in placements:
            for c, r, dloc in outward_edges(shapes[face]):
                bsq = to_board(c, r, x, y, rot)
                dc, dr = STEP[rotate_dir(dloc, rot)]
                partner = owner.get((bsq[0] + dc, bsq[1] + dr))
                if partner is None:
                    continue
                j = joins[(face, c, r, dloc)]
                j["missions"].add(mission)
                j["partners"].add(partner)

    out = {}
    for (face, c, r, dloc), j in joins.items():
        out.setdefault(face, []).append({
            "sq": [c, r], "dir": dloc,
            "missions": sorted(j["missions"]), "partners": sorted(j["partners"]),
        })
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    json.dump(out, open(OUT, "w"), indent=1)

    total = sum(len(list(outward_edges(s))) for s in shapes.values())
    used = sum(len(v) for v in out.values())
    print("%d outward edges, %d (%.0f%%) meet another tile somewhere in %d faces"
          % (total, used, 100.0 * used / total, len(out)))
    return 0


if __name__ == "__main__":
    sys.exit(main())

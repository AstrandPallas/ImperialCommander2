"""Detect the printed black line along a tile's outward edges.

Imperial Assault prints a wall along a tile edge as a thin black line at the
very edge of the art, and prints an opening -- a corridor end, a doorway --
by letting the floor run right up to the cut. Interior walls defeated
detection because they are painted as 3D structures; the BORDER line is not,
and it measures cleanly: the outermost 3% of a square along a walled edge sits
at roughly half the luminance of the 5-15% just inside it, while an open edge
reads the same as its interior.

This only ever proposes. The verdict per face is reviewed against the art and
recorded in tools/walls.json with a reason; this script's numbers are what the
review starts from and what a later regression can be measured against.

    python tools/wall_detect.py            # all faces -> tools/_overlays/wall_metrics.json
    python tools/wall_detect.py --write    # ... and the walls source, tools/walls.json
    python tools/wall_detect.py Core_12A   # print one face's edges

tools/walls.json is what author_terrain.py reads. It carries every outward
edge the detector calls a wall, the edges it was unsure about (so a table
correction has somewhere to start), and the overrides from wall_review.json,
which win over the detector wherever they speak.
"""

import glob
import io
import json
import os
import re
import sys

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TILES = os.path.join(ROOT, "ImperialCommander2", "Assets", "SagaTiles")
DIMS = os.path.join(ROOT, "ImperialCommander2", "Assets", "Resources", "dimensions.json")
OUT = os.path.join(ROOT, "tools", "_overlays", "wall_metrics.json")
WALLS = os.path.join(ROOT, "tools", "walls.json")
REVIEW = os.path.join(ROOT, "tools", "wall_review.json")

# The printed line has one physical thickness, so in pixels it is a fixed
# fraction of a square: about 7 px on a 128 px square, 4 px on a 63 px one.
LINE = 0.055
SEARCH = 0.12          # the line sits within this much of the cut
INNER = (0.15, 0.28)   # reference floor, past any line
MARGIN = 0.2           # trim the ends of each unit edge, where corners confuse

# A wall: a plateau of LINE thickness, markedly darker than the floor just
# inside it, followed by a step back up. Ratio alone misreads dark art
# (a dark tile has a dark floor) and shadows (a gradient, not a step).
RATIO_WALL = 0.75
ABS_WALL = 100.0
RATIO_SURE = 0.5
# Share of the edge carrying the line, in BINS slices.
COVER_WALL = 0.625
COVER_OPEN = 0.25


def load_dims():
    raw = io.open(DIMS, encoding="utf-8-sig").read()
    data = json.loads(re.sub(r",(\s*[}\]])", r"\1", raw))
    return {(e["expansion"], str(e["id"])): (e["width"], e["height"]) for e in data}


def faces():
    out = []
    for exp in sorted(os.listdir(TILES)):
        for f in sorted(glob.glob(os.path.join(TILES, exp, "*.png"))):
            out.append((os.path.basename(f)[:-4], f))
    return out


def scan_margins(rgb, alpha):
    """Rows and columns of scanner border (pure black or translucent) on each side."""
    H, W = alpha.shape
    # A border line is empty right across: fully transparent or pure black.
    # A shaped tile's void is transparent too, but never along a whole line.
    border = (alpha == 0) | (rgb.max(axis=2) <= 2)

    def run(lines):
        n = 0
        for line in lines:
            if n < 6 and line.mean() > 0.98:
                n += 1
            else:
                break
        return n
    return {
        "N": run(border[y] for y in range(H)),
        "S": run(border[H - 1 - y] for y in range(H)),
        "W": run(border[:, x] for x in range(W)),
        "E": run(border[:, W - 1 - x] for x in range(W)),
    }


def profile(lum, c, r, side, pw, ph, n, span=(MARGIN, 1 - MARGIN)):
    """Mean luminance at each pixel distance from the cut, along part of the unit edge."""
    x0, y0 = int(c * pw), int(r * ph)
    x1, y1 = int((c + 1) * pw), int((r + 1) * ph)
    a0, a1 = span
    if side in ("N", "S"):
        lo, hi = x0 + int(a0 * pw), x0 + max(int(a0 * pw) + 1, int(a1 * pw))
    else:
        lo, hi = y0 + int(a0 * ph), y0 + max(int(a0 * ph) + 1, int(a1 * ph))
    if side == "N":
        return [float(lum[y0 + k, lo:hi].mean()) for k in range(n)]
    if side == "S":
        return [float(lum[y1 - 1 - k, lo:hi].mean()) for k in range(n)]
    if side == "W":
        return [float(lum[lo:hi, x0 + k].mean()) for k in range(n)]
    return [float(lum[lo:hi, x1 - 1 - k].mean()) for k in range(n)]


BINS = 8


def coverage(lum, c, r, side, pw, ph, n, px):
    """Fraction of the edge, in BINS slices, along which the line is present."""
    hits = 0
    for b in range(BINS):
        span = (0.1 + 0.8 * b / BINS, 0.1 + 0.8 * (b + 1) / BINS)
        p = profile(lum, c, r, side, pw, ph, n, span)
        if len(p) >= 8 and not any(np.isnan(p)) and judge(p, px)["wall"]:
            hits += 1
    return hits / float(BINS)


def judge(p, px):
    """Plateau-and-step verdict on one profile. px is pixels per square."""
    t = max(3, int(round(LINE * px)))
    search = max(t + 2, int(SEARCH * px))
    i0, i1 = int(INNER[0] * px), int(INNER[1] * px)
    inner = float(np.median(p[i0:i1])) if i1 <= len(p) else float(np.median(p[i0:]))

    # The line's core: the darkest 3 px run near the cut.
    core, pos = None, 0
    for k in range(0, search - 2):
        v = sum(p[k:k + 3]) / 3.0
        if core is None or v < core:
            core, pos = v, k

    # How thick the dark run is, measured at half depth against the floor
    # band. A printed line is about t px, up to twice that on the south and
    # east sides where the scan's drop shadow merges with it; a dark floor
    # is far wider, a scratch narrower.
    half = (core + inner) / 2.0
    lo = pos
    while lo > 0 and p[lo - 1] < half:
        lo -= 1
    hi = pos + 2
    while hi + 1 < len(p) and p[hi + 1] < half:
        hi += 1
    thick = hi - lo + 1

    # The reference is what lies just past the line. Walls are painted as
    # 3D structures, so the floor deeper in may be dark again; the line is
    # still a line if the pixels right after it jump up.
    after = p[hi + 1:hi + 1 + 2 * t]
    ref = float(np.median(after)) if after else inner
    ratio = core / ref if ref > 1 else 1.0
    step = (ref - core) / (inner - core) if inner > core + 1 else 0.0

    contrast = (ratio < RATIO_WALL and core < ABS_WALL) or ratio < RATIO_SURE
    wall = contrast and 0.4 * t <= thick <= 4 * t
    return {"dark": round(core, 1), "inner": round(ref, 1), "ratio": round(ratio, 3),
            "pos": pos, "thick": thick, "t": t, "step": round(step, 2), "wall": bool(wall)}


def measure(face, path, dims):
    exp, rest = face.split("_")
    tid, side = rest[:-1], rest[-1]
    if (exp, tid) not in dims:
        return None
    w, h = dims[(exp, tid)]
    im = np.asarray(Image.open(path).convert("RGBA")).astype(float)
    H, W = im.shape[:2]
    pw, ph = W / w, H / h
    rgb = im[..., :3]
    lum = rgb.mean(axis=2)
    alpha = im[..., 3]
    margins = scan_margins(rgb, alpha)
    # Shift the profile past the scanner border on the outer sides only.
    lum_in = lum[margins["N"]:H - margins["S"], margins["W"]:W - margins["E"]]
    Hi, Wi = lum_in.shape
    pwi, phi = Wi / w, Hi / h

    def real(c, r):
        if not (0 <= c < w and 0 <= r < h):
            return False
        return alpha[int(r * ph):int((r + 1) * ph), int(c * pw):int((c + 1) * pw)].mean() >= 128

    edges = []
    n = int(0.3 * min(pwi, phi))
    for r in range(h):
        for c in range(w):
            if not real(c, r):
                continue
            for dc, dr, side in ((0, -1, "N"), (0, 1, "S"), (-1, 0, "W"), (1, 0, "E")):
                if real(c + dc, r + dr):
                    continue
                p = profile(lum_in, c, r, side, pwi, phi, n)
                if len(p) < 8 or any(np.isnan(p)):
                    continue
                px = pwi if side in ("W", "E") else phi
                v = judge(p, px)
                cov = coverage(lum_in, c, r, side, pwi, phi, n, px)
                # The whole-edge verdict decides; coverage settles the cases
                # where art interrupts the line or a wall ends mid-edge.
                v["cover"] = cov
                if cov >= COVER_WALL:
                    v["wall"] = True
                elif cov <= COVER_OPEN:
                    v["wall"] = False
                else:
                    # Unsettled: the line runs along part of the edge. A
                    # figure needs most of the edge open to pass, so half
                    # or more line is a wall. These are listed as unsure.
                    v["wall"] = cov >= 0.5
                v.update({"sq": [c, r], "dir": side})
                edges.append(v)
    return {"face": face, "w": w, "h": h, "edges": edges}


def unsure(e):
    """Verdicts the numbers do not settle: a line along part of the edge."""
    return COVER_OPEN < e["cover"] < COVER_WALL


def write_walls(results):
    review = json.load(open(REVIEW)) if os.path.exists(REVIEW) else {}
    out = {"_comment": [
        "Printed wall lines on tile edges, one entry per face, in the tile's",
        "unrotated frame. Generated by tools/wall_detect.py --write from the",
        "art; 'unsure' lists edges the detector could not settle and",
        "tools/wall_review.json overrides it. Do not edit by hand.",
    ], "faces": {}}
    for m in results:
        face = m["face"]
        ov = review.get(face, {})
        force_wall = {tuple(x) for x in ov.get("wall", [])}
        force_open = {tuple(x) for x in ov.get("open", [])}
        walls, unsure_edges = [], []
        for e in m["edges"]:
            key = (e["sq"][0], e["sq"][1], e["dir"])
            if key in force_open:
                continue
            if key in force_wall or e["wall"]:
                walls.append(list(key))
            if unsure(e) and key not in force_wall:
                unsure_edges.append(list(key))
        entry = {"walls": walls}
        if unsure_edges:
            entry["unsure"] = unsure_edges
        if ov.get("why"):
            entry["why"] = ov["why"]
        out["faces"][face] = entry
    json.dump(out, open(WALLS, "w"), indent=1)
    n = sum(len(f["walls"]) for f in out["faces"].values())
    u = sum(len(f.get("unsure", [])) for f in out["faces"].values())
    print("%s: %d wall edges, %d unsure, %d faces overridden" % (
        os.path.relpath(WALLS, ROOT), n, u, len(review)))


def main():
    dims = load_dims()
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if args:
        for face in args:
            exp = face.split("_")[0]
            m = measure(face, os.path.join(TILES, exp, face + ".png"), dims)
            if not m:
                print(face, "unknown")
                continue
            print(face, "%dx%d" % (m["w"], m["h"]))
            for e in m["edges"]:
                print("  (%d,%d) %s  dark=%5.1f inner=%5.1f ratio=%.2f pos=%2d thick=%d/%d cover=%.2f  %s"
                      % (e["sq"][0], e["sq"][1], e["dir"], e["dark"], e["inner"],
                         e["ratio"], e["pos"], e["thick"], e["t"], e["cover"], "WALL" if e["wall"] else "open"))
        return 0

    results = []
    for face, path in faces():
        m = measure(face, path, dims)
        if m:
            results.append(m)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    json.dump(results, open(OUT, "w"), indent=1)

    total = sum(len(m["edges"]) for m in results)
    walls = sum(1 for m in results for e in m["edges"] if e["wall"])
    print("%d faces, %d outward edges, %d read as wall (%.0f%%)"
          % (len(results), total, walls, 100.0 * walls / max(1, total)))
    if "--write" in sys.argv:
        write_walls(results)
    return 0


if __name__ == "__main__":
    sys.exit(main())

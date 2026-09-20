"""Detect printed terrain markings on Imperial Assault map tile art.

Printed conventions (Rules Reference):
  solid blue border   -> difficult terrain  (+1 MP to enter)
  solid red border    -> blocking terrain   (no enter, no LOS, no counting through)
  dashed red border   -> impassable terrain (no enter; spaces stay ADJACENT and LOS passes)

The grid is exact: a tile's pixel size divided by its (width, height) from
dimensions.json gives the pixels per square. Markings run along square
boundaries, so every candidate is sampled as a band centred on a grid line.

The hard part is not finding coloured pixels, it is rejecting coloured
ARTWORK. Core_33A is a 2x2 tile that is 12.9% red because it depicts a glowing
vent; it carries no terrain marking at all. The discriminator is therefore
grid alignment and line thinness, never colour alone.

Run:  python tools/terrain_detect.py Core_1A [Core_19B ...]
      python tools/terrain_detect.py --all Core
"""

import argparse
import json
import os
import re
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
ASSETS = os.path.join(REPO, "ImperialCommander2", "Assets")
TILES = os.path.join(ASSETS, "SagaTiles")
DIMENSIONS = os.path.join(ASSETS, "Resources", "dimensions.json")

# Half-width in pixels of the band sampled either side of a grid line.
BAND = 4
# A marking must cover at least this fraction of the edge to count at all.
MIN_COVERAGE = 0.35
# Above this coverage with few gaps we call it solid; below, dashed.
SOLID_COVERAGE = 0.80
MAX_GAPS_SOLID = 2


def load_dimensions():
    raw = open(DIMENSIONS, encoding="utf-8-sig").read()
    dims = {}
    for t in json.loads(re.sub(r",(\s*[}\]])", r"\1", raw)):
        dims[(t["expansion"], str(t["id"]))] = (t["width"], t["height"])
    return dims


def masks(a):
    """Return (red, blue) boolean masks for saturated marking ink."""
    R, G, B = a[:, :, 0].astype(int), a[:, :, 1].astype(int), a[:, :, 2].astype(int)
    mx = np.maximum(np.maximum(R, G), B)
    mn = np.minimum(np.minimum(R, G), B)
    sat = mx - mn
    red = (R > 110) & (R - G > 55) & (R - B > 45) & (sat > 60)
    blue = (B > 105) & (B - R > 45) & (B - G > 25) & (sat > 55)
    return red, blue


def runs(vec):
    """(coverage, gap_count) for a boolean 1-D vector."""
    if vec.size == 0:
        return 0.0, 99
    cov = float(vec.mean())
    gaps = 0
    prev = True  # leading gap is not counted
    for v in vec:
        if prev and not v:
            gaps += 1
        prev = bool(v)
    return cov, gaps


def edge_band(mask, px, py, c, r, direction):
    """Boolean vector sampled along one square edge.

    Square (c, r) spans pixels [c*px, (c+1)*px) x [r*py, (r+1)*py).
    'N' is the top edge, 'W' the left edge, and so on. The band is BAND pixels
    either side of the boundary; a pixel column/row counts if any pixel in the
    band is ink, which tolerates the line being a pixel or two off.
    """
    h, w = mask.shape
    x0, x1 = int(round(c * px)), int(round((c + 1) * px))
    y0, y1 = int(round(r * py)), int(round((r + 1) * py))
    if direction in ("N", "S"):
        y = y0 if direction == "N" else y1
        lo, hi = max(0, y - BAND), min(h, y + BAND + 1)
        if lo >= hi or x0 >= x1:
            return np.zeros(0, bool)
        return mask[lo:hi, x0:x1].any(axis=0)
    y_lo, y_hi = max(0, y0), min(h, y1)
    x = x0 if direction == "W" else x1
    lo, hi = max(0, x - BAND), min(w, x + BAND + 1)
    if lo >= hi or y_lo >= y_hi:
        return np.zeros(0, bool)
    return mask[y_lo:y_hi, lo:hi].any(axis=1)


# How far off the nominal grid line a printed marking may sit, as a fraction of
# a square. Imperial Assault draws a space's border just INSIDE that space, so
# the ink does not straddle the boundary -- on Empire_8B the blue line sits 7.7
# pixels clear of it. Squares run 85 to 256 pixels across the corpus, so a fixed
# pixel tolerance is simultaneously too tight on the big tiles and too loose on
# the small ones; this is a fraction for that reason.
SEARCH_FRAC = 0.09


def best_edge_band(mask, px, py, c, r, direction):
    """Best (vector, offset) for one edge, searching for where the line sits.

    Widening BAND would also find an offset line, but at a cost measured over
    the reviewed corpus: going from 4 to 12 pixels gains 6.6% more edges on
    faces that carry real terrain and 77% more on faces that carry none. A wide
    band unions everything within reach, so it collects scattered artwork just
    as eagerly as a line.

    Searching offsets instead keeps the band narrow and asks a different
    question: is there SOME single offset at which this edge is covered? A
    printed line is a coherent straight run and answers yes at exactly one
    offset. Artwork scattered through the same neighbourhood answers no at every
    one of them, because it is never aligned.
    """
    reach = max(1, int(round(SEARCH_FRAC * (py if direction in ("N", "S") else px))))
    best, best_off, best_cov = None, 0, -1.0
    for off in range(-reach, reach + 1):
        if direction in ("N", "S"):
            vec = edge_band(mask, px, py, c, r + off / py, direction)
        else:
            vec = edge_band(mask, px, py, c + off / px, r, direction)
        if vec.size == 0:
            continue
        cov, _ = runs(vec)
        if cov > best_cov:
            best, best_off, best_cov = vec, off, cov
    return (best if best is not None else np.zeros(0, bool)), best_off


def ink_edges(red, blue, px, py, w, h):
    """Map every interior grid edge to its ink.

    Returns {(c, r, 'N'|'W'): {'red': (cov, gaps), 'blue': (cov, gaps)}} using
    canonical storage: each edge is owned by exactly one square via its N or W
    side, so an edge is never sampled twice.
    """
    edges = {}
    for r in range(h):
        for c in range(w):
            for d in ("N", "W"):
                if d == "N" and r == 0:
                    continue  # tile perimeter, not an interior edge
                if d == "W" and c == 0:
                    continue
                e = {}
                for name, mask in (("red", red), ("blue", blue)):
                    # Same offset search as edge_report. The region fill reads
                    # THIS map, so if the two disagree a marking can be visible
                    # in the edge listing and still invisible to the flood fill.
                    vec, _ = best_edge_band(mask, px, py, c, r, d)
                    e[name] = runs(vec)
                edges[(c, r, d)] = e
    return edges


def edge_between(a, b):
    """Canonical key for the edge separating two orthogonally adjacent squares."""
    (c1, r1), (c2, r2) = a, b
    if r2 == r1 + 1:
        return (c2, r2, "N")
    if r1 == r2 + 1:
        return (c1, r1, "N")
    if c2 == c1 + 1:
        return (c2, r2, "W")
    if c1 == c2 + 1:
        return (c1, r1, "W")
    return None


def inked(e, colour):
    if e is None:
        return False
    cov, _ = e[colour]
    return cov >= MIN_COVERAGE


def regions(edges, w, h, colour, void=frozenset()):
    """Connected components of squares, treating inked edges of `colour` as walls.

    `void` holds squares that are not part of the tile at all, read from the
    art's alpha channel. They bound a component exactly as a wall does, because
    a figure cannot stand there and a marking has nothing to enclose there.

    Honouring them is not cosmetic. A marking that closes against a void corner
    forms no loop across the bare bounding box, so the fill leaks out and the
    region is lost -- the same defect as a marking closing against the tile
    edge. Jabba_8A is the worked example: its haze patch at (0,1),(1,1) is inked
    on three sides and closed on the fourth by the void at (0,0), and without
    this the fill escapes north and swallows the whole top half of the tile.
    """
    comp = {}
    cid = 0
    for start in ((c, r) for r in range(h) for c in range(w)):
        if start in comp or start in void:
            continue
        stack, cid = [start], cid + 1
        comp[start] = cid
        while stack:
            c, r = stack.pop()
            for dc, dr in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nb = (c + dc, r + dr)
                if not (0 <= nb[0] < w and 0 <= nb[1] < h) or nb in comp:
                    continue
                if nb in void:
                    continue  # not part of the tile
                if inked(edges.get(edge_between((c, r), nb)), colour):
                    continue  # blocked by a marking line
                comp[nb] = cid
                stack.append(nb)
    out = {}
    for sq, i in comp.items():
        out.setdefault(i, []).append(sq)
    return out


def boundary_profile(cells, edges, w, h, colour):
    """How much of a component's boundary is drawn in `colour` ink.

    A genuine terrain region is ringed by its marking. The tile's open floor is
    bounded mostly by the tile perimeter instead, which carries no marking ink,
    so this cleanly separates the two without guessing which is larger.
    """
    cellset = set(cells)
    ink_n = per_n = 0
    gaps = []
    for (c, r) in cells:
        for dc, dr in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nb = (c + dc, r + dr)
            if nb in cellset:
                continue
            if not (0 <= nb[0] < w and 0 <= nb[1] < h):
                per_n += 1  # tile perimeter
                continue
            e = edges.get(edge_between((c, r), nb))
            if inked(e, colour):
                ink_n += 1
                gaps.append(e[colour][1])
    total = ink_n + per_n
    return {
        "ink": ink_n, "perimeter": per_n,
        "ink_ratio": (ink_n / total) if total else 0.0,
        "median_gaps": float(np.median(gaps)) if gaps else 99.0,
    }


def square_ink(mask, px, py, c, r, inset=0.18):
    """Ink density in a square's inner ring, used to decide which side of a
    marking line the terrain lies on. IA draws a space's border just inside
    that space, so the marked side carries visibly more ink."""
    h, w = mask.shape
    x0, x1 = int(round(c * px)), int(round((c + 1) * px))
    y0, y1 = int(round(r * py)), int(round((r + 1) * py))
    x0, x1 = max(0, x0), min(w, x1)
    y0, y1 = max(0, y0), min(h, y1)
    if x0 >= x1 or y0 >= y1:
        return 0.0
    sub = mask[y0:y1, x0:x1]
    ih = max(1, int(sub.shape[0] * inset))
    iw = max(1, int(sub.shape[1] * inset))
    ring = np.zeros(sub.shape, bool)
    ring[:ih, :] = ring[-ih:, :] = True
    ring[:, :iw] = ring[:, -iw:] = True
    return float(sub[ring].mean())


def analyse(tile, dims, verbose=False):
    exp, rest = tile.split("_")
    tid = rest[:-1]
    path = os.path.join(TILES, exp, tile + ".png")
    if not os.path.exists(path) or (exp, tid) not in dims:
        return None
    w, h = dims[(exp, tid)]
    im = Image.open(path).convert("RGB")
    a = np.asarray(im)
    px, py = im.size[0] / w, im.size[1] / h
    red, blue = masks(a)
    edges = ink_edges(red, blue, px, py, w, h)
    void = void_squares(tile, dims)

    grid = [["." for _ in range(w)] for _ in range(h)]
    detail = []

    # Blue first (difficult), then red, so a red marking wins on conflict.
    for colour, mask in (("blue", blue), ("red", red)):
        comps = regions(edges, w, h, colour, void)
        if len(comps) < 2:
            continue  # no closed marking of this colour on this tile
        for cells in comps.values():
            prof = boundary_profile(cells, edges, w, h, colour)
            if prof["ink"] == 0:
                continue
            # Which side of the boundary is the marked space? Compare inner-ring
            # ink inside the component against its neighbours across inked edges.
            inside = np.mean([square_ink(mask, px, py, c, r) for c, r in cells])
            outs = []
            cs = set(cells)
            for (c, r) in cells:
                for dc, dr in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nb = (c + dc, r + dr)
                    if nb in cs or not (0 <= nb[0] < w and 0 <= nb[1] < h):
                        continue
                    if inked(edges.get(edge_between((c, r), nb)), colour):
                        outs.append(square_ink(mask, px, py, nb[0], nb[1]))
            outside = float(np.mean(outs)) if outs else 0.0
            if inside <= outside * 1.10:
                continue  # neighbours are the marked side, not us
            if colour == "blue":
                g = "d"
            else:
                solid = prof["median_gaps"] <= MAX_GAPS_SOLID
                g = "X" if solid else "I"
            for (c, r) in cells:
                grid[r][c] = g
            if verbose:
                detail.append((sorted(cells), g, prof, round(inside, 3), round(outside, 3)))

    return {
        "tile": tile, "size": im.size, "wh": (w, h),
        "px": (round(px, 1), round(py, 1)),
        "red_frac": round(float(red.mean()) * 100, 2),
        "blue_frac": round(float(blue.mean()) * 100, 2),
        "grid": ["".join(row) for row in grid], "detail": detail,
    }


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("tiles", nargs="*")
    ap.add_argument("--all", metavar="EXPANSION")
    ap.add_argument("--verbose", action="store_true")
    args = ap.parse_args()

    dims = load_dimensions()
    names = list(args.tiles)
    if args.all:
        d = os.path.join(TILES, args.all)
        names += sorted(f[:-4] for f in os.listdir(d) if f.endswith(".png"))

    for n in names:
        res = analyse(n, dims, args.verbose)
        if not res:
            print(f"{n}: NOT FOUND or no dimensions")
            continue
        marked = sum(ch != "." for row in res["grid"] for ch in row)
        total = res["wh"][0] * res["wh"][1]
        print(f'{res["tile"]:<11} img={str(res["size"]):<11} {res["wh"][0]}x{res["wh"][1]} '
              f'px/sq={res["px"]}  red={res["red_frac"]}% blue={res["blue_frac"]}%  '
              f'marked={marked}/{total}')
        for row in res["grid"]:
            print("   ", row)
        if args.verbose:
            for cells, g, prof, ins, outs in res["detail"]:
                print(f"      {g} x{len(cells)} {cells[:6]} ink_ratio={prof[chr(39)+chr(39)] if False else round(prof['ink_ratio'],2)} gaps={prof['median_gaps']} in={ins} out={outs}")

    return 0


if __name__ == "__main__":
    sys.exit(main())


# ---------------------------------------------------------------------------
# Candidate regions
#
# Measured on Core_19B against ground truth: the flood-fill component matched
# the true difficult-terrain region EXACTLY (23/23 squares, no misses, no
# extras), while the inner-ring ink heuristic that decided which component was
# terrain got it exactly backwards -- it rejected the correct region and
# accepted all three wrong ones.
#
# Ink geometry cannot answer "which side of this line is the terrain"; both
# sides share the same edges. So the detector stops guessing. It emits the
# candidate regions it found and lets a semantic reviewer pick, which turns a
# 36-square labelling problem into a 4-way choice.
# ---------------------------------------------------------------------------

def void_squares(tile, dims):
    """Squares the tile's art marks as outside the playable area."""
    import tile_shapes
    shape = tile_shapes.shape_of(tile, dims)
    if not shape:
        return frozenset()
    return frozenset((c, r) for r, row in enumerate(shape)
                     for c, ch in enumerate(row) if ch == "-")


def candidate_regions(tile, dims):
    """Return labelled candidate regions for a tile, without classifying them."""
    exp, rest = tile.split("_")
    tid = rest[:-1]
    path = os.path.join(TILES, exp, tile + ".png")
    if not os.path.exists(path) or (exp, tid) not in dims:
        return None
    w, h = dims[(exp, tid)]
    im = Image.open(path).convert("RGB")
    a = np.asarray(im)
    px, py = im.size[0] / w, im.size[1] / h
    red, blue = masks(a)
    edges = ink_edges(red, blue, px, py, w, h)
    void = void_squares(tile, dims)

    out = {"tile": tile, "size": im.size, "wh": (w, h),
           "px": (round(px, 1), round(py, 1)),
           "red_frac": round(float(red.mean()) * 100, 2),
           "blue_frac": round(float(blue.mean()) * 100, 2),
           "regions": []}

    label = ord("A")
    for colour in ("blue", "red"):
        comps = regions(edges, w, h, colour, void)
        if len(comps) < 2:
            continue  # no closed marking of this colour divides the tile
        for cells in sorted(comps.values(), key=lambda v: (-len(v), sorted(v)[0])):
            prof = boundary_profile(cells, edges, w, h, colour)
            if prof["ink"] == 0:
                continue
            out["regions"].append({
                "label": chr(label),
                "ink": colour,
                "cells": sorted(cells),
                "size": len(cells),
                "boundary_ink": prof["ink"],
                "boundary_perimeter": prof["perimeter"],
                "median_gaps": prof["median_gaps"],
                # blue border -> difficult, red border -> blocking or impassable
                "suggests": "d" if colour == "blue" else ("X" if prof["median_gaps"] <= MAX_GAPS_SOLID else "I"),
            })
            label += 1
    return out


# ---------------------------------------------------------------------------
# Wall detection
#
# Walls are the load-bearing terrain data. Measured on CORE1: with no walls
# authored, closing every door changes reachability from 203 squares to 203 --
# rooms do not exist and the AI paths straight through them. Difficult terrain
# is a refinement; walls decide whether orders are legal at all.
#
# Walls print as dark, low-saturation lines running along a square boundary.
# The tile art also carries thin light grid lines, so darkness alone is not
# enough: a wall is markedly DARKER than the squares on either side of it, and
# runs the length of the edge.
# ---------------------------------------------------------------------------

WALL_MIN_COVERAGE = 0.55
WALL_DARK_MARGIN = 26


def luminance(a):
    return (0.299 * a[:, :, 0] + 0.587 * a[:, :, 1] + 0.114 * a[:, :, 2])


def square_luma(lum, px, py, c, r, inset=0.28):
    """Median brightness of a square's interior, used as the local reference
    a wall has to be darker than."""
    h, w = lum.shape
    x0, x1 = int(round(c * px)), int(round((c + 1) * px))
    y0, y1 = int(round(r * py)), int(round((r + 1) * py))
    ix, iy = int((x1 - x0) * inset), int((y1 - y0) * inset)
    x0, x1 = max(0, x0 + ix), min(w, x1 - ix)
    y0, y1 = max(0, y0 + iy), min(h, y1 - iy)
    if x0 >= x1 or y0 >= y1:
        return None
    return float(np.median(lum[y0:y1, x0:x1]))


def wall_edges(lum, px, py, w, h, band=3):
    """Detect dark lines on interior grid edges.

    Returns {(c, r, 'N'|'W'): {'coverage', 'contrast'}} for edges that look
    like walls, using the canonical N/W ownership so each edge is judged once.
    """
    found = {}
    H, W = lum.shape
    for r in range(h):
        for c in range(w):
            for d in ("N", "W"):
                if d == "N" and r == 0:
                    continue
                if d == "W" and c == 0:
                    continue
                other = (c, r - 1) if d == "N" else (c - 1, r)
                ref_a = square_luma(lum, px, py, c, r)
                ref_b = square_luma(lum, px, py, other[0], other[1])
                if ref_a is None or ref_b is None:
                    continue
                ref = min(ref_a, ref_b)

                if d == "N":
                    y = int(round(r * py))
                    lo, hi = max(0, y - band), min(H, y + band + 1)
                    x0, x1 = int(round(c * px)), int(round((c + 1) * px))
                    if lo >= hi or x0 >= x1:
                        continue
                    strip = lum[lo:hi, x0:x1].min(axis=0)
                else:
                    x = int(round(c * px))
                    lo, hi = max(0, x - band), min(W, x + band + 1)
                    y0, y1 = int(round(r * py)), int(round((r + 1) * py))
                    if lo >= hi or y0 >= y1:
                        continue
                    strip = lum[y0:y1, lo:hi].min(axis=1)

                dark = strip < (ref - WALL_DARK_MARGIN)
                cov = float(dark.mean())
                if cov >= WALL_MIN_COVERAGE:
                    found[(c, r, d)] = {
                        "coverage": round(cov, 2),
                        "contrast": round(ref - float(np.median(strip)), 1),
                    }
    return found


def detect_walls(tile, dims):
    exp, rest = tile.split("_")
    tid = rest[:-1]
    path = os.path.join(TILES, exp, tile + ".png")
    if not os.path.exists(path) or (exp, tid) not in dims:
        return None
    w, h = dims[(exp, tid)]
    im = Image.open(path).convert("RGB")
    a = np.asarray(im).astype(float)
    px, py = im.size[0] / w, im.size[1] / h
    return {
        "tile": tile, "wh": (w, h), "size": im.size,
        "px": (round(px, 1), round(py, 1)),
        "walls": wall_edges(luminance(a), px, py, w, h),
    }


# ---------------------------------------------------------------------------
# Edge report
#
# The region pass throws away information the detector already has. Every
# interior edge is measured for ink coverage and gap count; regions then
# discard all of it except where a closed loop happened to form.
#
# That discarded measurement is exactly what is needed for the two cases
# regions structurally cannot see:
#   * DASHED markings (impassable), which never close by definition
#   * markings that close against a tile edge or void rather than into a loop
#
# Coverage says an edge is inked; the gap count says solid or dashed. Placement
# is then exact, and the reviewer only has to decide whether the ink is a real
# marking -- the judgement calls stay on the side that is good at them.
# ---------------------------------------------------------------------------

EDGE_MIN_COVERAGE = 0.30


def edge_report(tile, dims, min_cov=EDGE_MIN_COVERAGE):
    """Every inked interior edge, with coverage, gaps and a solid/dashed call."""
    exp, rest = tile.split("_")
    tid = rest[:-1]
    path = os.path.join(TILES, exp, tile + ".png")
    if not os.path.exists(path) or (exp, tid) not in dims:
        return None
    w, h = dims[(exp, tid)]
    im = Image.open(path).convert("RGB")
    a = np.asarray(im).astype(int)
    px, py = im.size[0] / w, im.size[1] / h
    red, blue = masks(a)

    void = void_squares(tile, dims)
    out = {"tile": tile, "wh": (w, h), "edges": [], "mixed": 0, "outline": 0}
    for r in range(h):
        for c in range(w):
            for d in ("N", "W"):
                if d == "N" and r == 0:
                    continue
                if d == "W" and c == 0:
                    continue
                found = []
                for colour, mask in (("red", red), ("blue", blue)):
                    vec, off = best_edge_band(mask, px, py, c, r, d)
                    cov, gaps = runs(vec)
                    if cov < min_cov:
                        continue
                    solid = gaps <= MAX_GAPS_SOLID
                    found.append({
                        "sq": (c, r), "dir": d, "ink": colour,
                        "coverage": round(cov, 2), "gaps": gaps, "offset": off,
                        "style": "solid" if solid else "dashed",
                        # solid red = blocking, dashed red = impassable,
                        # solid blue = difficult terrain boundary
                        "suggests": ("blocking" if solid else "impassable")
                                    if colour == "red" else "difficult-boundary",
                    })

                # A single pixel cannot be both red and blue, so an edge that
                # reports both means the sample band is straddling TWO nearby
                # lines -- a red marking and a blue one running close together.
                #
                # Narrowing the band does not fix this cleanly: it halves the
                # contamination but also loses real edges (Twin_1B drops from 22
                # detected edges to 11). So the ambiguity is FLAGGED rather than
                # guessed. A mixed edge must be resolved by looking at the tile,
                # never by taking whichever coverage happens to be higher.
                if len(found) > 1:
                    out["mixed"] += 1
                    for e in found:
                        e["mixed"] = True
                        e["suggests"] = "AMBIGUOUS - two inks in the band, inspect the art"

                # An edge with a VOID square on one side is the tile's own
                # outline, not an interior marking. Tile art routinely paints
                # that outline in a saturated colour -- Bespin_4A is a
                # cross-shaped viewport room whose entire boundary glows blue,
                # producing twelve high-coverage "solid" edges of which exactly
                # two are real.
                #
                # These are flagged rather than dropped. The shape already makes
                # the tile edge a wall, so an outline edge carries no extra
                # information, but silently discarding them would hide a
                # genuine marking that happens to run along the boundary.
                if any((c + dc, r + dr) in void or
                       not (0 <= c + dc < w and 0 <= r + dr < h)
                       for dc, dr in (((0, 0), (0, -1)) if d == "N"
                                      else ((0, 0), (-1, 0))))    :
                    out["outline"] += len(found)
                    for e in found:
                        e["outline"] = True
                out["edges"].extend(found)
    return out

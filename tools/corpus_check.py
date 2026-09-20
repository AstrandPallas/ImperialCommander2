"""Validate the board coordinate model against every shipped mission.

The mission format stores tile placements in editor units where 10 units == 1
board square, with rotation applied clockwise about the tile's top-left corner.
This harness reconstructs the occupied square set for every mission and reports
anything the model cannot explain, so that errors surface as a bounded list
before any engine work depends on the model.

Run:  python tools/corpus_check.py [--verbose] [--mission CORE1]
"""

import argparse
import json
import os
import re
import sys
from collections import Counter, defaultdict

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
ASSETS = os.path.join(REPO, "ImperialCommander2", "Assets")
DIMENSIONS = os.path.join(ASSETS, "Resources", "dimensions.json")
MISSIONS = os.path.join(ASSETS, "Resources", "SagaMissions")

# Assets/Scripts/Common/Common.cs:8
EXPANSIONS = ["Core", "Twin", "Hoth", "Bespin", "Jabba", "Empire", "Lothal", "Other"]

# Assets/Scripts/GameCore/Interfaces-Enums.cs:59
# Tiles live in mapSections[].mapTiles, everything else in mapEntities[].
ENTITY_KIND = {
    0: "Tile", 1: "Terminal", 2: "Crate", 3: "DeploymentPoint",
    4: "Token", 5: "Highlight", 6: "Door",
}
DOOR = 6

UNITS_PER_SQUARE = 10


def load_lenient(path):
    """Mission JSON and dimensions.json contain trailing commas, which
    Newtonsoft accepts but json.loads rejects. Strip them first."""
    raw = open(path, encoding="utf-8-sig").read()
    return json.loads(re.sub(r",(\s*[}\]])", r"\1", raw))


def load_dimensions():
    dims = {}
    for t in load_lenient(DIMENSIONS):
        dims[(t["expansion"], str(t["id"]))] = (t["width"], t["height"])
    return dims


def parse_pos(s):
    """'960,980' -> (96, 98) in square coordinates."""
    x, y = s.split(",")
    return float(x) / UNITS_PER_SQUARE, float(y) / UNITS_PER_SQUARE


def occupied_squares(x, y, w, h, rot):
    """Squares covered by a w x h tile whose unrotated top-left is (x, y),
    rotated `rot` degrees clockwise about that top-left corner."""
    rot = int(round(rot)) % 360
    if rot == 0:
        cols, rows = range(x, x + w), range(y, y + h)
    elif rot == 90:
        cols, rows = range(x - h, x), range(y, y + w)
    elif rot == 180:
        cols, rows = range(x - w, x), range(y - h, y)
    elif rot == 270:
        cols, rows = range(x, x + h), range(y - w, y)
    else:
        return None  # non-orthogonal rotation: unsupported by the model
    return [(c, r) for c in cols for r in rows]


def check_mission(path, dims):
    """Returns a result dict for one mission file."""
    name = os.path.splitext(os.path.basename(path))[0]
    res = {
        "mission": name, "squares": 0, "tiles": 0,
        "overlaps": [], "bad_rotation": [], "missing_dims": [],
        "fractional_pos": [], "entities_off_board": [], "entities_total": 0,
        "error": None,
    }
    try:
        d = load_lenient(path)
    except Exception as e:  # noqa: BLE001 - report, never abort the corpus run
        res["error"] = f"{type(e).__name__}: {e}"
        return res

    owner = {}  # square -> tile label, for attributing overlaps
    board = set()

    for section in d.get("mapSections") or []:
        for t in section.get("mapTiles") or []:
            res["tiles"] += 1
            exp = EXPANSIONS[t["expansion"]] if t["expansion"] < len(EXPANSIONS) else t["expansion"]
            label = f'{exp}_{t["tileID"]}{t.get("tileSide", "")}'
            key = (exp, str(t["tileID"]))
            if key not in dims:
                res["missing_dims"].append(label)
                continue
            w, h = dims[key]
            fx, fy = parse_pos(t["entityPosition"])
            if fx != int(fx) or fy != int(fy):
                res["fractional_pos"].append((label, t["entityPosition"]))
            x, y = int(fx), int(fy)

            squares = occupied_squares(x, y, w, h, t["entityRotation"])
            if squares is None:
                res["bad_rotation"].append((label, t["entityRotation"]))
                continue
            for sq in squares:
                if sq in board:
                    res["overlaps"].append((sq, owner.get(sq), label))
                else:
                    owner[sq] = label
                board.add(sq)

    res["squares"] = len(board)

    # Entities should resolve onto the board. Doors are edge features and are
    # expected to sit on a boundary, so they are reported but not counted.
    for e in d.get("mapEntities") or []:
        res["entities_total"] += 1
        fx, fy = parse_pos(e["entityPosition"])
        sq = (int(fx), int(fy))
        if sq not in board:
            kind = ENTITY_KIND.get(e.get("entityType"), e.get("entityType"))
            res["entities_off_board"].append((e.get("name"), kind, e["entityPosition"]))
    return res


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--verbose", action="store_true")
    ap.add_argument("--mission", help="check a single mission by name, e.g. CORE1")
    args = ap.parse_args()

    dims = load_dimensions()
    paths = []
    for root, _, files in os.walk(MISSIONS):
        for f in files:
            if f.endswith(".json"):
                if args.mission and os.path.splitext(f)[0].upper() != args.mission.upper():
                    continue
                paths.append(os.path.join(root, f))
    paths.sort()

    results = [check_mission(p, dims) for p in paths]

    parse_errors = [r for r in results if r["error"]]
    ok = [r for r in results if not r["error"]]

    tot_overlap = sum(len(r["overlaps"]) for r in ok)
    tot_offboard = sum(len(r["entities_off_board"]) for r in ok)
    tot_entities = sum(r["entities_total"] for r in ok)
    tot_squares = sum(r["squares"] for r in ok)

    print(f"missions parsed      : {len(ok)}/{len(results)}")
    if parse_errors:
        print(f"  PARSE FAILURES     : {len(parse_errors)}")
        for r in parse_errors[:10]:
            print(f"    {r['mission']}: {r['error']}")
    print(f"tiles placed         : {sum(r['tiles'] for r in ok)}")
    print(f"squares (sum)        : {tot_squares}")
    print(f"squares (mean/map)   : {tot_squares / max(len(ok), 1):.1f}")
    print(f"overlapping squares  : {tot_overlap}")
    print(f"entities             : {tot_entities}")
    print(f"entities off board   : {tot_offboard} "
          f"({100.0 * tot_offboard / max(tot_entities, 1):.1f}%)")

    missing = sorted({m for r in ok for m in r["missing_dims"]})
    badrot = [b for r in ok for b in r["bad_rotation"]]
    frac = [f for r in ok for f in r["fractional_pos"]]
    print(f"tiles missing dims   : {len(missing)}")
    print(f"non-orthogonal rots  : {len(badrot)}")
    print(f"fractional positions : {len(frac)}")

    if missing:
        print("  missing dimension entries:", ", ".join(missing[:20]))
    if badrot:
        print("  non-orthogonal:", badrot[:10])

    # Off-board entities by kind tells us whether the residue is all doors
    # (expected, they are edge features) or something the model gets wrong.
    kinds = Counter(k for r in ok for (_, k, _) in r["entities_off_board"])
    if kinds:
        print("  off-board by kind    :", dict(kinds))

    worst = sorted(ok, key=lambda r: -len(r["overlaps"]))[:10]
    if worst and worst[0]["overlaps"]:
        print("\nmissions with most overlaps:")
        for r in worst:
            if r["overlaps"]:
                print(f"  {r['mission']:<12} {len(r['overlaps']):>4} overlaps, "
                      f"{r['squares']:>4} squares, {r['tiles']:>3} tiles")

    if args.verbose or args.mission:
        print("\nper-mission:")
        for r in ok:
            print(f"  {r['mission']:<12} squares={r['squares']:<5} tiles={r['tiles']:<4} "
                  f"overlaps={len(r['overlaps']):<4} offboard={len(r['entities_off_board'])}")
            if args.mission:
                for sq, a, b in r["overlaps"]:
                    print(f"      overlap at {sq}: {a} vs {b}")
                for n, k, p in r["entities_off_board"]:
                    print(f"      off-board: {n} ({k}) at {p}")

    # Exit non-zero only on things that mean the model itself is wrong.
    fatal = bool(parse_errors or missing or badrot)
    return 1 if fatal else 0


if __name__ == "__main__":
    sys.exit(main())

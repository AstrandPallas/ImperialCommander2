"""Generate a C# test fixture of real mission geometry.

Hand-written fixtures prove the rules; this proves the bridge from mission data
to board geometry, which is where a coordinate-model error would surface. The
generated file lets the C# engine be checked against the same numbers the Python
corpus harness measures, so two independent implementations have to agree.

Run:  python tools/gen_mission_fixture.py [MISSION]     (default CORE1)
"""

import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from corpus_check import MISSIONS, EXPANSIONS, load_lenient, load_dimensions, parse_pos  # noqa: E402
import tile_shapes  # noqa: E402

DOOR = 6
OUT_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                       "tests", "BoardTests", "Generated")


def find_mission(name):
    for root, _, files in os.walk(MISSIONS):
        for f in files:
            if f.endswith(".json") and os.path.splitext(f)[0].upper() == name.upper():
                return os.path.join(root, f)
    return None


def generate(name="CORE1"):
    path = find_mission(name)
    if not path:
        raise SystemExit(f"mission {name} not found")
    dims = load_dimensions()
    d = load_lenient(path)

    tiles = []
    for s in d.get("mapSections") or []:
        for t in s.get("mapTiles") or []:
            exp = EXPANSIONS[t["expansion"]]
            fx, fy = parse_pos(t["entityPosition"])
            tiles.append((exp, str(t["tileID"]), t.get("tileSide", ""),
                          int(fx), int(fy), int(round(t["entityRotation"])), s["GUID"]))

    doors = []
    for e in d.get("mapEntities") or []:
        if e.get("entityType") != DOOR:
            continue
        fx, fy = parse_pos(e["entityPosition"])
        doors.append((int(fx), int(fy), int(round(e["entityRotation"])),
                      bool(e["entityProperties"].get("isActive"))))

    used = sorted({(t[0], t[1]) for t in tiles})
    faces = sorted({(t[0], t[1], t[2]) for t in tiles})
    cls = name.capitalize() + "Placements"
    L = [
        f"// GENERATED from {os.path.relpath(path, os.path.dirname(OUT_DIR))}",
        "// Regenerate with: python tools/gen_mission_fixture.py",
        "// Real mission geometry, so the engine is exercised on a shipped map",
        "// rather than only on hand-written fixtures.",
        "using System.Collections.Generic;",
        "",
        "namespace Saga.Board.Tests",
        "{",
        f"\tpublic static class {cls}",
        "\t{",
        "\t\tpublic static readonly TilePlacement[] Tiles = new TilePlacement[]",
        "\t\t{",
    ]
    for exp, tid, side, x, y, rot, guid in tiles:
        L.append(f'\t\t\tnew TilePlacement {{ Expansion = "{exp}", TileId = "{tid}", '
                 f'Side = "{side}", X = {x}, Y = {y}, Rotation = {rot}, SectionGuid = "{guid}" }},')
    L += ["\t\t};", "", "\t\tpublic static readonly DoorPlacement[] Doors = new DoorPlacement[]", "\t\t{"]
    for x, y, rot, op in doors:
        L.append(f'\t\t\tnew DoorPlacement {{ X = {x}, Y = {y}, Rotation = {rot}, '
                 f'Open = {str(op).lower()} }},')
    L += ["\t\t};", "",
          "\t\t/// <summary>Tile dimensions for every tile this mission uses.</summary>",
          "\t\tpublic static readonly Dictionary<string, (int w, int h)> Dimensions =",
          "\t\t\tnew Dictionary<string, (int w, int h)>", "\t\t\t{"]
    for exp, tid in used:
        w, h = dims[(exp, tid)]
        L.append(f'\t\t\t\t{{ "{exp}_{tid}", ({w}, {h}) }},')
    L += ["\t\t\t};", "",
          "\t\t/// <summary>Playable shape per tile face, taken from the art's",
          "\t\t/// alpha channel. '-' is a square outside the tile. In Imperial",
          "\t\t/// Assault the tile's SHAPE is the wall, so these are load-bearing",
          "\t\t/// rather than cosmetic: 22% of tile faces are non-rectangular,",
          "\t\t/// and honouring the shapes removes 84% of apparent tile overlaps.</summary>",
          "\t\tpublic static readonly TileTerrain[] Shapes = new TileTerrain[]",
          "\t\t{"]
    # Prefer authored terrain (shape + reviewed difficult/blocking/impassable)
    # over bare shape, so the fixture exercises what the engine will really see.
    authored = {}
    for exp in {f[0] for f in faces}:
        p = os.path.join(os.path.dirname(MISSIONS), "TerrainData", f"{exp}.json")
        if not os.path.exists(p):
            continue
        for t in json.load(open(p, encoding="utf-8"))["tiles"]:
            authored[(exp, str(t["tileId"]), t["side"])] = (t["squares"], t.get("edges", []))

    ETYPE = {"impassable": "EdgeType.Impassable", "blocking": "EdgeType.Blocking",
             "wall": "EdgeType.Wall"}
    for exp, tid, side in faces:
        got = authored.get((exp, tid, side))
        rows = got[0] if got else (tile_shapes.shape_of(f"{exp}_{tid}{side}", dims) or [])
        edges = got[1] if got else []
        w, h = dims[(exp, tid)]
        rowlit = ", ".join(f'"{r}"' for r in rows)
        edgelit = ", ".join(
            f'new TileEdge {{ C = {e["sq"][0]}, R = {e["sq"][1]}, Dir = "{e["dir"]}", '
            f'Type = {ETYPE.get(e["type"], "EdgeType.Wall")} }}'
            for e in edges)
        extra = f", Edges = new[] {{ {edgelit} }}" if edges else ""
        L.append(f'\t\t\tnew TileTerrain {{ Expansion = "{exp}", TileId = "{tid}", '
                 f'Side = "{side}", Width = {w}, Height = {h}, '
                 f'Rows = new[] {{ {rowlit} }}{extra} }},')
    L += ["\t\t};", "",
          "\t\tpublic static TerrainLibrary Library()",
          "\t\t{",
          "\t\t\tvar lib = new TerrainLibrary();",
          "\t\t\tforeach ( var t in Shapes ) lib.Add( t );",
          "\t\t\treturn lib;",
          "\t\t}", "",
          "\t\tpublic static (int w, int h)? Lookup( string exp, string id )",
          '\t\t\t=> Dimensions.TryGetValue( exp + "_" + id, out var d ) ? d : ((int, int)?)null;',
          "\t}", "}"]

    os.makedirs(OUT_DIR, exist_ok=True)
    out = os.path.join(OUT_DIR, f"{cls}.cs")
    open(out, "w", encoding="utf-8").write("\n".join(L) + "\n")
    print(f"{name}: {len(tiles)} tiles, {len(doors)} doors, {len(used)} distinct tile ids -> {out}")


if __name__ == "__main__":
    generate(sys.argv[1] if len(sys.argv) > 1 else "CORE1")

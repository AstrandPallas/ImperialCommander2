"""Every shipped mission and every tile face, as one compact test fixture.

The per-mission fixtures (gen_mission_fixture.py) are for reading; this one is
for sweeping. It exists so a test can build all 138 maps with the authored
terrain and prove that no printed wall cuts a mission off from itself: with
every door open, whatever the Rebels could reach by tile shape alone they must
still reach once the walls are in. A wall that fails that is a misread edge
or a missed doorway, and the test names the mission and the square.

Placements, doors and points of interest are string-encoded and parsed at
test time, which keeps a 2,300-tile corpus to one readable file.

    python tools/gen_corpus_fixture.py     # -> tests/BoardTests/Generated/Corpus.cs
"""

import glob
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import corpus_check as cc  # noqa: E402

OUT = os.path.join(cc.REPO, "tests", "BoardTests", "Generated", "Corpus.cs")
TERRAIN = os.path.join(cc.ASSETS, "Resources", "TerrainData")
KIND = {1: "T", 2: "C", 3: "D", 4: "K", 5: "H"}
ETYPE = {"wall": "w", "blocking": "b", "impassable": "i"}


def clean(name):
    return re.sub(r"[^A-Za-z0-9 _'()-]", "", name or "").strip()[:40]


def mission_rows():
    rows = []
    for path in sorted(glob.glob(os.path.join(cc.MISSIONS, "*", "*.json"))):
        d = cc.load_lenient(path)
        name = os.path.splitext(os.path.basename(path))[0]
        sections, tiles = {}, []
        for s in d.get("mapSections") or []:
            sid = sections.setdefault(s["GUID"], "s%d" % len(sections))
            for t in s.get("mapTiles") or []:
                exp = cc.EXPANSIONS[t["expansion"]]
                fx, fy = cc.parse_pos(t["entityPosition"])
                tiles.append("%s_%s%s@%d,%dr%d%s" % (
                    exp, t["tileID"], t.get("tileSide", "A"),
                    int(fx), int(fy), int(round(t["entityRotation"])), sid))
        doors, points = [], []
        for e in d.get("mapEntities") or []:
            fx, fy = cc.parse_pos(e["entityPosition"])
            props = e.get("entityProperties") or {}
            if e.get("entityType") == cc.DOOR:
                doors.append("%d,%dr%d%s" % (
                    int(fx), int(fy), int(round(e["entityRotation"])),
                    "o" if props.get("isActive") else "c"))
                continue
            k = KIND.get(e.get("entityType"))
            if not k:
                continue
            if k == "H" and not props.get("isActive", True):
                continue
            points.append("%s|%s|%d,%d" % (k, clean(e.get("name")), int(fx), int(fy)))
        rows.append((name, ";".join(tiles), ";".join(doors), ";".join(points)))
    return rows


def face_rows():
    rows = []
    for exp in cc.EXPANSIONS:
        p = os.path.join(TERRAIN, exp + ".json")
        if not os.path.exists(p):
            continue
        for t in json.load(open(p, encoding="utf-8"))["tiles"]:
            face = "%s_%s%s" % (exp, t["tileId"], t["side"])
            edges = " ".join("%d,%d%s%s" % (e["sq"][0], e["sq"][1], e["dir"],
                                            ETYPE.get(e["type"], "w"))
                             for e in t.get("edges", []))
            unsure = " ".join("%d,%d%s" % (c, r, d) for (c, r, d) in t.get("wallsUnsure", []))
            rows.append((face, t["width"], t["height"], "/".join(t["squares"]), edges, unsure))
    return rows


PARSERS = r'''
		/// <summary>The authored terrain; pass keepWalls false to see the map by shape alone.</summary>
		public static TerrainLibrary Library( bool keepWalls = true )
		{
			var lib = new TerrainLibrary();
			foreach ( var (face, w, h, rows, edges, _) in Faces )
			{
				int us = face.IndexOf( '_' );
				lib.Add( new TileTerrain
				{
					Expansion = face.Substring( 0, us ),
					TileId = face.Substring( us + 1, face.Length - us - 2 ),
					Side = face.Substring( face.Length - 1 ),
					Width = w, Height = h,
					Rows = rows.Split( '/' ),
					Edges = ParseEdges( edges, keepWalls ),
				} );
			}
			return lib;
		}

		private static TileEdge[] ParseEdges( string s, bool keepWalls )
		{
			var list = new List<TileEdge>();
			foreach ( var tok in s.Split( ' ', StringSplitOptions.RemoveEmptyEntries ) )
			{
				char k = tok[tok.Length - 1];
				var type = k == 'b' ? EdgeType.Blocking : k == 'i' ? EdgeType.Impassable : EdgeType.Wall;
				if ( type == EdgeType.Wall && !keepWalls ) continue;
				var cr = tok.Substring( 0, tok.Length - 2 ).Split( ',' );
				list.Add( new TileEdge
				{
					C = int.Parse( cr[0] ), R = int.Parse( cr[1] ),
					Dir = tok.Substring( tok.Length - 2, 1 ), Type = type,
				} );
			}
			return list.ToArray();
		}

		/// <summary>Was this wall edge one the detector could not settle from the art?.</summary>
		public static bool IsUnsure( string face, int c, int r, string dir )
		{
			foreach ( var f in Faces )
			{
				if ( f.Face != face ) continue;
				return (" " + f.Unsure + " ").Contains( $" {c},{r}{dir} " );
			}
			return false;
		}

		public static TilePlacement[] Tiles( string spec )
		{
			var list = new List<TilePlacement>();
			foreach ( var tok in spec.Split( ';', StringSplitOptions.RemoveEmptyEntries ) )
			{
				int at = tok.IndexOf( '@' ), r = tok.IndexOf( 'r', at ), sIdx = tok.IndexOf( 's', r );
				string face = tok.Substring( 0, at );
				int us = face.IndexOf( '_' );
				var xy = tok.Substring( at + 1, r - at - 1 ).Split( ',' );
				list.Add( new TilePlacement
				{
					Expansion = face.Substring( 0, us ),
					TileId = face.Substring( us + 1, face.Length - us - 2 ),
					Side = face.Substring( face.Length - 1 ),
					X = int.Parse( xy[0] ), Y = int.Parse( xy[1] ),
					Rotation = int.Parse( tok.Substring( r + 1, sIdx - r - 1 ) ),
					SectionGuid = tok.Substring( sIdx ),
				} );
			}
			return list.ToArray();
		}

		public static DoorPlacement[] Doors( string spec, bool? forceOpen = null )
		{
			var list = new List<DoorPlacement>();
			foreach ( var tok in spec.Split( ';', StringSplitOptions.RemoveEmptyEntries ) )
			{
				int r = tok.IndexOf( 'r' );
				var xy = tok.Substring( 0, r ).Split( ',' );
				list.Add( new DoorPlacement
				{
					X = int.Parse( xy[0] ), Y = int.Parse( xy[1] ),
					Rotation = int.Parse( tok.Substring( r + 1, tok.Length - r - 2 ) ),
					Open = forceOpen ?? tok[tok.Length - 1] == 'o',
				} );
			}
			return list.ToArray();
		}

		/// <summary>Kind is T terminal, C crate, D deployment point, K token, H highlight.</summary>
		public static (char Kind, string Name, Sq Square)[] Points( string spec )
		{
			var list = new List<(char, string, Sq)>();
			foreach ( var tok in spec.Split( ';', StringSplitOptions.RemoveEmptyEntries ) )
			{
				var parts = tok.Split( '|' );
				var cr = parts[2].Split( ',' );
				list.Add( (parts[0][0], parts[1], new Sq( int.Parse( cr[0] ), int.Parse( cr[1] ) )) );
			}
			return list.ToArray();
		}
	}
}
'''


def main():
    dims = cc.load_dimensions()
    missions = mission_rows()
    faces = face_rows()
    L = [
        "// GENERATED by tools/gen_corpus_fixture.py -- do not edit.",
        "// Every shipped mission and every authored tile face, string-encoded.",
        "using System;",
        "using System.Collections.Generic;",
        "",
        "namespace Saga.Board.Tests",
        "{",
        "\tpublic static class Corpus",
        "\t{",
        "\t\t/// <summary>Tiles \"Face@x,yr{rot}s{section};...\", doors \"x,yr{rot}{o|c};...\",",
        "\t\t/// points \"{kind}|name|c,r;...\".</summary>",
        "\t\tpublic static readonly (string Name, string Tiles, string Doors, string Points)[] Missions =",
        "\t\t{",
    ]
    for name, tiles, doors, points in missions:
        L.append('\t\t\t( "%s", "%s", "%s", "%s" ),' % (name, tiles, doors, points))
    L += ["\t\t};", "",
          "\t\t/// <summary>Face, width, height, rows joined by '/', edges \"c,r{dir}{w|b|i} ...\",",
          "\t\t/// and the wall edges the art did not settle, \"c,r{dir} ...\".</summary>",
          "\t\tpublic static readonly (string Face, int W, int H, string Rows, string Edges, string Unsure)[] Faces =",
          "\t\t{"]
    for face, w, h, rows, edges, unsure in faces:
        L.append('\t\t\t( "%s", %d, %d, "%s", "%s", "%s" ),' % (face, w, h, rows, edges, unsure))
    L += ["\t\t};", "",
          "\t\tpublic static readonly Dictionary<string, (int w, int h)> Dimensions =",
          "\t\t\tnew Dictionary<string, (int w, int h)>",
          "\t\t\t{"]
    for (exp, tid), (w, h) in sorted(dims.items()):
        L.append('\t\t\t\t{ "%s_%s", (%d, %d) },' % (exp, tid, w, h))
    L += ["\t\t\t};", "",
          "\t\tpublic static (int w, int h)? Lookup( string exp, string id )",
          '\t\t\t=> Dimensions.TryGetValue( exp + "_" + id, out var d ) ? d : ((int, int)?)null;']
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    open(OUT, "w", encoding="utf-8").write("\n".join(L) + PARSERS)
    print("%d missions, %d faces -> %s (%d KB)" % (
        len(missions), len(faces), os.path.relpath(OUT, cc.REPO), os.path.getsize(OUT) // 1024))
    return 0


if __name__ == "__main__":
    sys.exit(main())

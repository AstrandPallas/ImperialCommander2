"""Assemble a terrain library from tile shapes plus reviewed terrain verdicts.

Two very different data sources are merged here, and the difference matters:

  * SHAPE ('-' void squares) comes from the art's alpha channel. It is exact,
    deterministic, needs no review, and covers all 276 tile faces.
  * TERRAIN (difficult / blocking / impassable) needs a semantic reading of the
    art, because ink geometry alone cannot say which side of a marking line the
    terrain is on, and coloured ARTWORK is indistinguishable from markings
    without understanding what is depicted.

Every terrain verdict below records the reasoning that produced it, so a later
reviewer can check the claim instead of re-deriving it. Tiles absent from
VERDICTS are unreviewed and get shape only.

Run:  python tools/author_terrain.py            # write Assets/Resources/TerrainData/Core.json
      python tools/author_terrain.py --report   # show coverage without writing
"""

import json
import os
import sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import terrain_detect as td  # noqa: E402
import tile_shapes as ts  # noqa: E402

OUT_DIR = os.path.join(td.ASSETS, "Resources", "TerrainData")
WALLS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "walls.json")


def load_walls():
    """Printed wall lines per face, from tools/walls.json (see wall_detect.py)."""
    if not os.path.exists(WALLS):
        return {}
    return json.load(open(WALLS, encoding="utf-8")).get("faces", {})

# glyph -> cells, plus the reasoning. "none" means reviewed and found to have
# no printed terrain at all, which is a real result and not the same as
# "unreviewed".
VERDICTS = {
    "Core_1A": {
        "blocking": [(1, 1), (2, 1), (3, 2), (4, 1)],
        "why": "Solid red boxes ring the crashed-wreckage squares. Verified by eye "
               "and independently by the detector's red flood-fill regions.",
        "confidence": "high",
    },
    "Core_5A": {
        "difficult": [(0, 3), (1, 2), (1, 3), (2, 1), (2, 2), (2, 3), (3, 1), (3, 2)],
        "why": "A blue line splits the tile diagonally: forest floor to the upper left, a "
               "rock outcrop to the lower right. Two comparable regions of 8 "
               "and 7 squares, neither enclosed (boundary_perimeter 6 and 8), so the "
               "geometry cannot say which side is marked. Magnified, the marked squares "
               "are rough broken lichen-covered rock and the unmarked ones are leaf litter "
               "and ferns, so the outcrop is the awkward footing and the reading stands. "
               "(3,3) is NOT difficult despite being blue-bounded: (3,3)N and (3,3)W are "
               "the outcrop's own south-east corner, where it stops short of the tree "
               "roots, rather than a box round (3,3).",
        "confidence": "high",
    },
    "Core_19B": {
        "difficult": [(2, 0), (3, 0), (4, 0), (5, 0), (2, 1), (3, 1),
                      (1, 2), (2, 2), (3, 2), (1, 3), (2, 3), (3, 3), (4, 3),
                      (0, 4), (1, 4), (2, 4), (3, 4), (4, 4), (5, 4),
                      (0, 5), (1, 5), (4, 5), (5, 5)],
        "why": "The flooded centre is the difficult terrain; its boundary is formed by "
               "the black walls TOGETHER with the blue lines. Hand-verified ground "
               "truth. Reading the blue in isolation inverts the answer, and both the "
               "detector heuristic and an unaided review got it backwards.",
        "confidence": "verified",
    },
    "Core_28B": {
        "why": "NO terrain. The 7.4% blue is a glowing orb at (2,2) -- artwork, not a "
               "marking. No blue line runs along any grid edge.",
        "confidence": "high",
    },
    "Core_33A": {
        "why": "NO terrain. 14.9% red is a glowing vent. Negative control for the "
               "detector: hue alone would mislabel the whole tile.",
        "confidence": "verified",
    },
    "Core_25B": {
        "why": "NO terrain markings. Non-rectangular: (0,0) and (3,0) are void, which "
               "comes from the alpha channel rather than review.",
        "confidence": "high",
    },
    "Core_8A": {
        "blocking": [(2, 2)],
        "why": "Single solid red box around the tree roots and rock at (2,2). No dashed "
               "edges. Plus-shaped tile: the four corners are void, from the alpha "
               "channel.",
        "confidence": "high",
    },
    "Core_23A": {
        "blocking": [(1, 2)],
        "why": "Single solid red box around the stacked crates at (1,2). The tile also "
               "reads 0.87% 'blue', but that is the cold ambient lighting of an interior "
               "tile, not a marking -- region detection correctly finds no blue region. "
               "Another way colour alone misleads: a whole tile tinted toward a marking "
               "colour.",
        "confidence": "high",
    },
    "Core_2A": {
        "blocking": [(2, 2), (0, 4)],
        "impassableEdges": [(2, 1, "N"), (3, 1, "N"), (2, 5, "N"), (3, 5, "N")],
        "why": "Two solid red boxes over the stone ruins at (2,2) and (0,4) -- blocking, "
               "and exactly the two single-square regions the detector found. Dashed red "
               "runs along the boundary between the raised stone pavement and the grass "
               "at the top and bottom of the tile -- impassable EDGES, which the region "
               "detector cannot see because a dashed line does not close.",
        "confidence": "high",
    },
    "Core_1B": {
        "blocking": [(2, 1), (3, 1), (2, 2), (3, 2), (1, 5)],
        "impassable": [(5, 2), (5, 3)],
        "why": "Solid red box over the shuttle, a 2x2 block at (2,1)-(3,2), plus a small "
               "one at (1,5) -- matching the detector's 4-square and 1-square regions. A "
               "DASHED outline rings the vegetation: impassable squares, not blocking, so "
               "line of sight still passes through them. CORRECTED by edge_report -- the "
               "dashed run covers (5,2)N, (5,2)W and (5,3)W, so the region is TWO squares "
               "(5,2) and (5,3), bounded on the right by the tile edge. The by-eye pass "
               "saw only one.",
        "confidence": "high",
    },
    "Core_4A": {
        "blocking": [(1, 1)],
        "difficult": [(2, 1), (1, 2), (2, 2)],
        "why": "Both conventions as real markings on one tile: a solid RED box round the "
               "stone statue at (1,1) -- blocking -- and a solid BLUE box round the pool "
               "at (2,1),(1,2),(2,2) -- difficult. The detector's red 1-square and blue "
               "3-square regions match exactly, which is what a clean tile looks like.",
        "confidence": "high",
    },
    "Core_7A": {
        "difficult": [(2, 2), (3, 2), (2, 3)],
        "why": "Blue border encloses the water in the bottom-right corner. The detector "
               "reports a 4-square region including (3,3), but (3,3) is VOID from the "
               "alpha channel, so the real answer is 3 squares. A good reminder to apply "
               "shape before terrain: otherwise terrain gets authored onto squares that "
               "are not part of the tile.",
        "confidence": "high",
    },
    "Core_22B": {
        "blockingEdges": [(2, 1, "W"), (1, 2, "N"), (2, 2, "N")],
        "why": "Solid red lines run along EDGES rather than round any square, tracing the "
               "lip of a raised shelf: vertical west of (2,1), horizontal across the top "
               "of (1,2) and (2,2). This is exactly why the tile reports 0 regions -- "
               "edge markings enclose nothing, so region detection cannot see them. "
               "CONFIRMED by edge_report: all three at coverage 0.99-1.00, gaps 0-1.",
        "confidence": "high",
    },
    "Core_33B": {
        "why": "NO terrain. The 0.63% red is machinery panel lights scattered down both "
               "walls; no line follows any grid edge. Same family as the glowing vent and "
               "lit orb, and rejected by the same structural test.",
        "confidence": "high",
    },
    "Core_27A": {
        "why": "NO terrain. 13.5% red is a wall of glowing grilles spanning the whole "
               "tile -- the largest artwork false positive found so far, and the same "
               "family as Core_33A. No line follows any grid edge.",
        "confidence": "high",
    },
    "Core_20B": {
        "blocking": [(2, 3), (3, 4)],
        "why": "Solid red boxes over the debris at (2,3) and (3,4). NOT fully resolved: "
               "the detector also reports a region at (3,5), but the right-hand columns "
               "sit under a bright furnace glow that contaminates red detection, and the "
               "boundary there could not be confirmed against the art. (3,5) is "
               "omitted: it needs a second look at higher magnification.",
        "confidence": "medium",
    },
    "Core_19A": {
        "impassable": [(3, 3), (4, 3), (3, 4), (4, 4)],
        "why": "A large dark chasm in the centre-right, outlined in DASHED red -- "
               "impassable, so figures cannot enter but the spaces stay adjacent and "
               "line of sight passes across it. The detector's region reports gaps=4.5, "
               "independently confirming the line is broken rather than solid. It reads "
               "visually as a pit; if a mission's rules treat it as one (figures may be "
               "pushed in) that belongs in a per-mission override, not here.",
        "confidence": "high",
    },
    "Core_26A": {
        "blocking": [(2, 1)],
        "why": "Single solid red box round the crates and machinery at (2,1). "
               "Plus-shaped tile with four void corners from the alpha channel.",
        "confidence": "high",
    },
    "Core_10B": {
        "difficult": [(1, 1)],
        "why": "Solid BLUE box round the wreckage at the centre -- difficult, not "
               "blocking. A heap of machinery looks impassable, but the printed colour "
               "decides the terrain type.",
        "confidence": "high",
    },
    "Core_3B": {
        "blocking": [(2, 2)],
        "why": "Single solid red box round the turbine at (2,2), matching the detector's "
               "1-square region on a 6x5 tile.",
        "confidence": "high",
    },
    "Core_22A": {
        "blocking": [(1, 2), (2, 2)],
        "why": "Solid red box over the 2x1 crate block. The detector reports gaps=0.0, "
               "the cleanest solid reading seen, independently confirming blocking "
               "rather than impassable.",
        "confidence": "high",
    },
    "Core_2B": {
        "blocking": [(2, 4), (3, 4)],
        "impassableEdges": [(1, 2, "W"), (1, 3, "W"), (1, 4, "W"),
                            (2, 1, "W"), (1, 2, "N"), (3, 2, "N")],
        "why": "Solid red over the crates at (2,4),(3,4) -- blocking, matching the "
               "detector's 2-square region. Separately, DASHED red traces a ridge across "
               "the tile: down the west side of column 1 and along the top of (2,2),(3,2). "
               "Those are impassable edges and are invisible to region detection because "
               "a dashed line never closes. VERIFIED against edge_report: 5 of 6 hand "
               "placements were right; (2,2)N was wrong and is corrected to (1,2)N.",
        "confidence": "high",
    },
    "Core_20A": {
        "blocking": [(0, 1), (0, 3)],
        "why": "Two solid red boxes over the turbine units on the left wall. The 2.52% "
               "'blue' is the cold ambient lighting of the tile's left half, not a "
               "marking -- region detection correctly finds no blue region, the same as "
               "on Core_23A.",
        "confidence": "high",
    },
    "Core_23B": {
        "blocking": [(2, 2)],
        "why": "Single solid red box over the engine assembly at (2,2) in the workshop.",
        "confidence": "high",
    },
    "Core_5B": {
        "blocking": [(1, 1)],
        "impassableEdges": [(0, 2, "N"), (3, 3, "N")],
        "why": "Solid red box over the circular hatch at (1,1) -- blocking. Two short "
               "DASHED segments also run along edges: north of (0,2) on the left and "
               "north of (3,3) on the right, marking small ledges. Neither forms a "
               "region, so only the visual pass finds them.",
        "confidence": "medium",
    },
    "Core_6A": {
        "blocking": [(1, 1)],
        "why": "Solid red box over the tree roots at (1,1). No dashed marks. (0,0) is "
               "void, from the alpha channel.",
        "confidence": "high",
    },
    "Core_26B": {
        "blocking": [(1, 1)],
        "why": "Solid red box over the crate and droid at (1,1). Plus-shaped tile with "
               "four void corners.",
        "confidence": "high",
    },
    "Core_8B": {
        "blocking": [(1, 1)],
        "why": "Solid red box over the wrecked machinery at (1,1). Plus-shaped tile with "
               "four void corners.",
        "confidence": "high",
    },
    "Core_24A": {
        "why": "NO terrain. The 3.75% blue is a glowing holographic projector table in "
               "the centre. It spans grid boundaries rather than following them, which "
               "is why it encloses no region. Corners (0,0) and (3,3) are void.",
        "confidence": "high",
    },
    "Core_28A": {
        "why": "NO terrain. The 1.56% blue is cold ambient lighting plus lit floor panels "
               "and window strips; the 0.12% red is console indicator lights. Nothing "
               "follows a grid edge.",
        "confidence": "high",
    },
    "Core_9A": {
        "difficult": [(1, 2), (1, 3), (2, 3)],
        "why": "REAL terrain despite the tile reporting 0 regions. A blue border traces "
               "the stream across the lower-left. It reports no region because the "
               "marking closes against the TILE EDGE and the void quadrant rather than "
               "forming a loop inside the grid, so the flood fill leaks around it. This "
               "is a second, distinct reason region detection misses real markings -- and "
               "the second proof that the 'ink, no region' bucket cannot be auto-cleared.",
        "confidence": "high",
    },
    "Core_37B": {
        "why": "NO terrain. The 0.96% blue is glowing strip lights along the right-hand "
               "wall. Nothing follows a grid edge.",
        "confidence": "high",
    },
    "Core_25A": {
        "why": "NO terrain. The 0.46% red is accent striping on the wall panels plus "
               "machinery detail on the central console and equipment at the foot of the "
               "tile. Nothing follows a grid edge. Corners (0,0) and (3,0) are void.",
        "confidence": "high",
    },
    "Core_38A": {
        "why": "NO terrain. Same glowing strip-light family as Core_37B: blue lighting "
               "down the right-hand column only, none of it on a grid edge.",
        "confidence": "high",
    },
    "Core_21A": {
        "why": "NO terrain. The 0.33% red is indicator lights on a scatter of small "
               "consoles across the floor. Nothing follows a grid edge.",
        "confidence": "high",
    },
    "Core_36A": {
        "why": "NO terrain. Blue wall strip lights and red floor-panel indicators, the "
               "same lighting family as Core_37B and Core_38A.",
        "confidence": "high",
    },
    "Core_24B": {
        "why": "NO terrain. Ink is scattered workshop detail -- a blue tank at (3,1) and "
               "tools along the bench -- none of it on a grid edge. Corners (3,0) and "
               "(0,3) are void.",
        "confidence": "high",
    },
    "Core_34A": {
        "why": "NO terrain. A plain interior room; the only ink is indicator dots on the "
               "wall consoles.",
        "confidence": "high",
    },
    "Core_29A": {
        "why": "NO terrain. Wall strip lights and floor-panel indicators.",
        "confidence": "high",
    },
    "Core_30A": {
        "why": "NO terrain. Same interior lighting family as Core_29A: wall strips and "
               "console indicators.",
        "confidence": "high",
    },
    "Core_32A": {
        "why": "NO terrain. A large blast door fills the tile; the only ink is indicator "
               "dots on the left-hand console.",
        "confidence": "high",
    },
    "Core_34B": {
        "why": "NO terrain. Blast-door tile with red and green signal lamps at the "
               "corners. The lamps are the entire red reading.",
        "confidence": "high",
    },
    "Core_29B": {
        "why": "NO terrain. Same blast-door and signal-lamp family as Core_34B.",
        "confidence": "high",
    },
    "Twin_6B": {
        "blocking": [(2, 0), (2, 1)],
        "why": "Solid red over the rock pile forming a 2-square column at (2,0),(2,1). "
               "The detector reports them as two separate 1-square regions, which is the "
               "same answer.",
        "confidence": "high",
    },
    "Twin_5B": {
        "blocking": [(1, 2), (1, 3), (0, 6), (0, 7)],
        "why": "Two solid red rock piles. The detector found only the lower one at "
               "(0,6),(0,7); the upper pile at (1,2),(1,3) closes against the VOID column "
               "at (2,2)-(2,5) rather than into a loop, so no region forms. Third "
               "confirmed instance of that failure mode, after Core_9A and Core_22B.",
        "confidence": "high",
    },
    "Twin_4B": {
        "why": "NO terrain. An L-shaped dune tile; the 0.17% red is scattered rock and "
               "debris detail, none of it on a grid edge. 16 void squares from alpha.",
        "confidence": "high",
    },
    "Bespin_2A": {
        "blocking": [(2, 0), (2, 1), (5, 0)],
        "impassableEdges": [(3, 3, "N"), (4, 3, "N")],
        "why": "A gantry corridor. Two solid red boxes: a conduit assembly over (2,0) and "
               "(2,1), closed by (2,0)W, (2,1)W, (3,0)W, (3,1)W and (2,2)N against the "
               "north tile edge, and a tool rack at (5,0). As on Twin_6A the detector "
               "splits the 1x2 because of a dashed reading at (2,1)N inside it, which is "
               "the conduit's own banding -- an impassable edge between two blocking "
               "squares would mean nothing. A red dashed railing at (3,3)N and (4,3)N "
               "(0.48-0.52, 4-5 gaps) runs along the walkway lip. "
               "This tile reads 6.31% blue and "
               "produced eleven blue edges, two of them at 0.91-0.92 coverage with 2 gaps "
               "-- indistinguishable by measurement from a solid marking. Magnified they "
               "are the seams between HEX DECK PLATES, a regular floor texture whose "
               "panel edges happen to run along the square grid. This defeats coverage "
               "and gap count together, so it is the first false positive neither "
               "automatic test can catch. What gives it away is distribution rather than "
               "shape: a printed marking is isolated and internally consistent, while the "
               "plating lit up eleven edges across one contiguous block at every style "
               "and coverage from 0.31 to 0.92. (3,2)N at 0.78 is excluded on the same "
               "grounds -- it is the soft lower edge of the red glow in column 3 and "
               "encloses nothing.",
        "confidence": "high",
    },
    "Bespin_1B": {
        "difficult": [(1, 1), (4, 1), (1, 4), (4, 4)],
        "why": "A Cloud City lounge with four seating clusters arranged symmetrically "
               "round a central sculpture. Each cluster is a single square boxed in solid "
               "blue at 0.98-0.99 coverage with 1 gap, and all four come back with "
               "boundary_perimeter 0. The sculpture in the middle is NOT marked, which is "
               "a useful negative: the detector finds no edges there either, so art and "
               "measurement agree on the absence as well as on the four positives.",
        "confidence": "high",
    },
    "Bespin_2B": {
        "blocking": [(2, 0), (5, 0)],
        "difficult": [(0, 2), (7, 2), (1, 3), (6, 3)],
        "why": "Two solid RED boxes along the top wall at (2,0) and (5,0), each closing "
               "against the north tile edge, and four solid BLUE single squares in the "
               "corners. Everything measures 0.91-1.00 with 0-2 gaps. The corner squares "
               "are worth noting: (1,3) and (6,3) close against the voids at (0,3) and "
               "(7,3) rather than against ink, so they only register as regions at all "
               "because the fill treats void squares as off-board.",
        "confidence": "high",
    },
    "Bespin_4A": {
        "difficult": [(0, 1), (0, 2)],
        "why": "A cross-shaped viewport room, and the clearest case yet of the tile's OWN "
               "OUTLINE reading as a marking: the boundary glows blue, so 10 of the 16 "
               "detected edges sit on it at 0.91-1.00 coverage and look exactly like "
               "printed lines. They are not -- the shape already makes the tile edge a "
               "wall, so they carry nothing. edge_report now flags them, which is what "
               "this tile prompted. Of the six genuinely interior edges, (1,1)W and (1,2)W "
               "are solid blue at 0.91-0.92 and enclose the alcove at (0,1),(0,2) against "
               "the west tile edge. The other four run 0.35-0.49 coverage with 4-7 gaps "
               "and are the station ring in the window behind.",
        "confidence": "high",
    },
    "Bespin_4B": {
        "impassableEdges": [(2, 1, "W"), (2, 2, "W")],
        "why": "A Cloud City control room. A red DASHED line runs down the west edge of "
               "column 2 through rows 1 and 2, cutting across the curved console bank, at "
               "0.64-0.65 coverage with 4-6 gaps. Impassable, so the console blocks "
               "movement but not sight -- an operator behind it can still be shot. The "
               "blue reading at (3,3)W is not a third marking: (3,3) is void, so that edge "
               "is the tile's own outline.",
        "confidence": "high",
    },
    "Bespin_5A": {
        "why": "No terrain. A cross-shaped junction round an OCTAGONAL lit platform, and "
               "the octagon is the whole story: its facets run diagonally across grid "
               "lines, so they clip six edges without ever lying along one. Same class as "
               "Bespin_6B's circular rotunda. Note (2,2)W is reported as 'solid' on 2 "
               "gaps, which is a reminder that the gap count only separates solid from "
               "dashed ONCE an edge is known to be a marking -- it cannot establish that "
               "one exists. Coverage is the check that fails here: 0.48, against the "
               "0.86-1.00 every confirmed solid marking measures.",
        "confidence": "high",
    },
    "Bespin_5B": {
        "why": "No terrain. The reverse of 5A and the same story with circles instead of "
               "facets: a turbolift platform of concentric rings, 2.41% blue, four edges "
               "clipped at 0.48-0.55 and none of them aligned to the grid.",
        "confidence": "high",
    },
    "Bespin_3B": {
        "difficult": [(1, 2), (2, 2), (1, 3), (2, 3)],
        "impassableEdges": [(2, 1, "W"), (2, 2, "W"), (2, 3, "W"), (2, 4, "W")],
        "why": "Cloud City's dining room, and it carries BOTH marking types at once. A "
               "solid blue box encloses the middle of the banquet table -- all 8 of its "
               "boundary edges inked at coverage 0.91-0.94, gaps 1, and the region comes "
               "back with boundary_perimeter 0, i.e. fully closed. That is difficult "
               "terrain over (1,2),(2,2),(1,3),(2,3). Separately a DASHED red line runs "
               "the length of the table's spine down the west edge of column 2, rows 1-4, "
               "at coverage ~0.5 with 4-5 gaps. Dashed is impassable, not blocking, so "
               "the table stays adjacent and does not stop line of sight -- you simply "
               "cannot step across it. Rows 0 and 5 carry nothing even at a 0.10 "
               "threshold, which matches the art: the table tapers to ordinary floor at "
               "both ends.",
        "confidence": "high",
    },
    "Bespin_6A": {
        "impassable": [(1, 1)],
        "why": "A glowing floor vent at (1,1), ringed by a dashed red box on all four "
               "sides -- (1,1)N, (1,1)W, (2,1)W, (1,2)N, coverage 0.42-0.50 with 4 gaps "
               "each. The tile is 4.37% red because of the glow, so colour alone would "
               "have called the whole tile terrain; the marking is real regardless. "
               "Dashed means "
               "IMPASSABLE, so the vent stays adjacent and line of sight passes over it. "
               "(2,0) is void.",
        "confidence": "high",
    },
    "Bespin_6B": {
        "why": "No terrain. The reverse of 6A: a circular Cloud City platform on a "
               "blue-violet tile that reads 19.1% blue, by far the highest blue fraction "
               "anywhere in the corpus. The four edges that clear the coverage floor are "
               "the platform's circular rim clipping through them, and the gap counts say "
               "so plainly -- 10, 14, 17 and 19, against 4-5 for a genuine dashed line "
               "and 0-1 for a solid one. This adds a false-positive class to the four "
               "already on record: a CURVED feature crossing grid edges. A printed "
               "marking is drawn along square boundaries by definition, so it runs "
               "parallel to the edge it inks; a curve only ever crosses one, which is "
               "what produces the shredded gap count.",
        "confidence": "high",
    },
    "Empire_8B": {
        "difficult": [(0, 2)],
        "blockingEdges": [(1, 0, "W"), (2, 2, "N")],
        "why": "The tile that exposed the offset-search defect. Its blue box round the "
               "green-lit console at (0,2) -- (0,2)N plus (1,2)W, closing against the west "
               "and south tile edges -- was INVISIBLE to the old fixed band: the printed "
               "line sits 13 pixels inside the square, and the band only reached 4, so "
               "(1,2)W measured 0.00 even at a 0.12 floor. With the offset search both "
               "edges read 0.91 at 2 gaps. Of the four red readings only two are printed: "
               "(1,0)W and (2,2)N, both 0.91, and magnifying them shows IA's border style "
               "on Empire tiles, a solid red bar paired with a light grey stripe. (1,1)N "
               "at 0.71 and (1,1)W at 0.41 show no bar at all under magnification -- they "
               "are the edge of the magenta floor glow. Coverage could not have settled "
               "that on its own, since genuine dashed markings run 0.42-0.64; only the art "
               "could.",
        "confidence": "high",
    },
    "Empire_11B": {
        "why": "No terrain. A corridor dead-end lit red from the floor, 11.39% red and not "
               "one grid-aligned line. The single edge that clears the floor, (1,1)W at "
               "0.56, is the tile's interior vertical passing through that glow. Same "
               "family as Empire_9A through 12A.",
        "confidence": "high",
    },
    "Empire_13A": {
        "why": "No terrain. Death Star corridor, red-lit wall panel, one interior edge at "
               "0.38 crossing the fill. See Empire_11B.",
        "confidence": "high",
    },
    "Empire_13B": {
        "why": "No terrain. As 13A: 6.85% red is a lit panel, one edge at 0.38.",
        "confidence": "high",
    },
    "Empire_14A": {
        "why": "No terrain. As 13A: 5.84% red is a lit panel, one edge at 0.39.",
        "confidence": "high",
    },
    "Empire_15A": {
        "why": "No terrain. As 13A: 5.83% red is a lit panel, one edge at 0.39.",
        "confidence": "high",
    },
    "Empire_7A": {
        "why": "No terrain. A corridor with a circular blast-door ring: the three blue "
               "readings sit at 12, 13 and 17 gaps, which is the ring clipping grid edges "
               "diagonally rather than any line running along one. The red at 0.33 is "
               "under the floor.",
        "confidence": "high",
    },
    "Empire_1B": {
        "difficult": [(1, 0), (5, 0), (0, 3), (7, 3), (1, 4), (6, 4)],
        "blocking": [(1, 1), (3, 3), (4, 3)],
        "why": "The reverse of 1A on the same cardboard, and the two faces INVERT each "
               "other's colours: where 1A has red-boxed emplacements round the walls and a "
               "blue console in the middle, 1B has blue-boxed alcoves and a red-boxed "
               "console at (3,3),(4,3). Plus a third red box round (1,1). Every region "
               "here comes back at boundary_perimeter 0 or 1 with 0.86-1.00 coverage. The "
               "six dashed-range reds are the emplacements' own glow, which is why so many "
               "edges report both inks.",
        "confidence": "high",
    },
    "Empire_2A": {
        "difficult": [(1, 1), (4, 1), (1, 4), (4, 4)],
        "blocking": [(0, 0), (5, 0), (0, 5), (5, 5)],
        "why": "An Imperial lounge with a mosaic floor: four armchairs blue-boxed at "
               "boundary_perimeter 0 (difficult), four corner machine banks red-boxed at "
               "boundary_perimeter 2 against the tile edges (blocking). Perfectly "
               "symmetric, same construction as Bespin_1B. The eight dashed-range red "
               "readings all sit on the armchairs' own boxes and are their upholstery "
               "catching the red channel, not a second marking.",
        "confidence": "high",
    },
    "Empire_1A": {
        "blocking": [(1, 0), (6, 0), (0, 3), (7, 3), (1, 4), (6, 4)],
        "difficult": [(3, 3), (4, 3)],
        "why": "A Death Star control chamber. Six gun emplacements set into the walls, "
               "each in a solid red box at 0.91-1.00 with 1-2 gaps closing against the "
               "tile edge, and a console at (3,3),(4,3) blue-boxed with "
               "boundary_perimeter 0. Voids at (0,4) and (7,4).",
        "confidence": "high",
    },
    "Empire_2B": {
        "blocking": [(4, 4)],
        "difficult": [(0, 4), (0, 5), (1, 5)],
        "blockingEdges": [(4, 0, "W"), (3, 1, "W"), (3, 2, "N"), (4, 2, "W"),
                          (5, 2, "N"), (1, 3, "N"), (2, 3, "W"), (4, 3, "N"),
                          (2, 4, "N"), (3, 4, "W")],
        "why": "A hangar workshop, and the hardest tile in the expansion to read because "
               "the floor is strewn with RED PIPES that are the same colour as a marking. "
               "The split is clean once measured, though: every genuine line sits at "
               "0.91-1.00 coverage with 0-2 gaps, and all ten discarded readings sit at "
               "0.34-0.71 with 2-6. A round tank at (4,4) is solid-boxed (blocking), three "
               "crate stacks along the bottom-left are blue-boxed (difficult), and two "
               "stepped solid red lines trace the raised walkway edges. Those enclose "
               "nothing, so they are edges rather than marked spaces -- which is also why "
               "the region fill finds only the tank.",
        "confidence": "high",
    },
    "Empire_3B": {
        "impassable": [(1, 1), (2, 1), (3, 1), (4, 1),
                       (1, 2), (2, 2), (3, 2), (4, 2)],
        "why": "An open shaft in the middle of a Death Star chamber, machinery visible "
               "below, ringed by a red-and-white DASHED border on all of its boundary "
               "edges at 0.60-0.78 coverage with 4-5 gaps and boundary_perimeter 0. "
               "Dashed means impassable, which is exactly right for a railed shaft: "
               "figures cannot walk into it but can shoot straight across. Calling it "
               "blocking would cut every sight line through the middle of the room. "
               "(0,3)N is excluded at 0.53 -- it is the edge of the orange floor glow in "
               "the corner and encloses nothing.",
        "confidence": "high",
    },
    "Empire_4A": {
        "difficult": [(2, 0), (3, 0), (1, 2), (2, 2), (1, 3), (2, 3)],
        "blockingEdges": [(1, 2, "W"), (1, 3, "W"), (3, 2, "W"), (3, 3, "W")],
        "impassableEdges": [(2, 2, "W"), (2, 3, "W")],
        "why": "A conveyor assembly in a corridor, plus a storage rack at (2,0),(3,0). The "
               "machine's 2x2 is blue-boxed at 0.98-1.00 with boundary_perimeter 0, so "
               "difficult -- you clamber over it. A red DASHED line at 0.57-0.59 with 5 "
               "gaps runs down its spine, so the two halves are not connected. And its "
               "east and west edges carry solid red at 0.98-1.00 ALONGSIDE the blue. "
               "That last point is a third distinct cause of a MIXED edge, after the two "
               "already on record. It is not two nearby lines contaminating one band, and "
               "not two adjacent terrains meeting as on Hoth_1A -- it is a region boundary "
               "that ALSO carries an edge marking. Magnified, both inks are printed lines: "
               "a bright blue one and a red bar with the light stripe that is IA's border "
               "style on Empire tiles. The result reads correctly as a machine: enter from "
               "north or south, clamber across, cannot cross the spine, cannot step out "
               "sideways.",
        "confidence": "high",
    },
    "Empire_6A": {
        "difficult": [(1, 1), (2, 1), (1, 2), (2, 2)],
        "blocking": [(0, 0), (0, 3)],
        "impassableEdges": [(2, 1, "W"), (2, 2, "W"), (1, 2, "N"), (2, 2, "N")],
        "why": "A circular shield generator in a well. Its 2x2 is blue-boxed at 0.98-1.00 "
               "with boundary_perimeter 0 (difficult), and a red DASHED cross at "
               "0.59-0.61 with 5-6 gaps runs through it on both axes, splitting it into "
               "four quadrants that cannot be crossed between. Two gun emplacements in the "
               "west corners sit in solid red boxes at 0.91 and are blocking.",
        "confidence": "high",
    },
    "Empire_4B": {
        "blocking": [(1, 0), (3, 1)],
        "blockingEdges": [(1, 2, "W"), (1, 3, "N"), (2, 3, "W"), (2, 4, "N")],
        "why": "Imperial interior. Two consoles in closed solid red boxes at (1,0) and "
               "(3,1), each shutting against the tile edge; their six boundary edges are "
               "not repeated below. What is left is a stepped line tracing the lip of a "
               "raised walkway -- (1,2)W, (1,3)N, (2,3)W, (2,4)N, all at 0.99-1.00 "
               "coverage with 0-1 gaps, so solid, so blocking. The eleventh edge, (1,4)W, "
               "is discarded: it measures 7 gaps, and (0,4) is a glowing furnace. That is "
               "the shredded-ink rule earning its place on a tile that also carries real "
               "markings, which is exactly where a blunter filter would have gone wrong.",
        "confidence": "high",
    },
    "Empire_5B": {
        "blocking": [(3, 0), (1, 2)],
        "impassableEdges": [(2, 1, "N"), (3, 1, "W"), (1, 3, "W")],
        "why": "A boulder in an alcove at (3,0) and a machine crate at (1,2), both in "
               "closed solid red boxes -- (1,2) comes back with boundary_perimeter 0. "
               "Three further edges are dashed at 4-5 gaps and are impassable. Note "
               "(3,0)W is solid at 0.93 while (3,1)W directly below it is dashed at 0.61: "
               "one vertical line that is wall beside the alcove and railing below it, the "
               "same construction as Jabba_5B. (0,3)N is dropped at 7 gaps -- that corner "
               "holds glowing red pipework.",
        "confidence": "high",
    },
    "Empire_6B": {
        "difficult": [(3, 3)],
        "impassableEdges": [(3, 0, "W"), (2, 1, "N"), (2, 1, "W"), (0, 2, "N")],
        "why": "Overgrown ruin lit in magenta, green and blue. (3,3) holds a blue-lit "
               "machine and is boxed by (3,3)N and (3,3)W at 0.91 coverage with 1-2 gaps, "
               "closing against the south and east tile edges. The blue ink and the blue "
               "light share a source here, so this is the Core_28B trap, but the two are "
               "distinguishable: the box edges are straight, grid-aligned and solid, while "
               "the machine's own glow is diffuse and contributes no aligned run. Four red "
               "edges at 4-5 gaps are dashed and therefore impassable.",
        "confidence": "medium",
    },
    "Hoth_3A": {
        "impassableEdges": [(1, 1, "N"), (2, 1, "N"), (1, 3, "N"), (2, 3, "N")],
        "why": "A narrow ice trench across the middle of a snowfield, its two lips printed "
               "as red dashed lines at 0.50-0.52 with 4 gaps each -- rows 1 and 3, columns "
               "1-2. Impassable rather than blocking, so figures on opposite sides of the "
               "trench can still shoot each other. The blue reading at (2,2)W is the "
               "wrecked speeder lying in the trench.",
        "confidence": "high",
    },
    "Hoth_8A": {
        "why": "No terrain. An ice cave floor with no printed marking anywhere; the 2.73% "
               "blue is the ice itself. Three edges clear the coverage floor along the "
               "bottom of the tile at 0.45-0.51, and magnification shows no line there. "
               "Voids at (0,0), (3,0), (3,1).",
        "confidence": "high",
    },
    "Hoth_8B": {
        "difficult": [(1, 1), (2, 1), (3, 1), (1, 2), (2, 2)],
        "why": "A field of icy rubble in the upper middle, ringed in solid blue at "
               "0.89-1.00 with 0-2 gaps. The strip above it at (1,0),(2,0) is the leftover "
               "floor between the marking and the tile edge, not a second region.",
        "confidence": "high",
    },
    "Hoth_9A": {
        "impassableEdges": [(3, 1, "W"), (2, 2, "N"), (2, 2, "W")],
        "why": "Three red dashed edges at 0.58-0.62 with 4-5 gaps tracing a ledge. The six "
               "blue readings on the same tile all sit at 0.31-0.40, below the 0.42 floor "
               "every confirmed dashed marking clears, and are ice texture.",
        "confidence": "high",
    },
    "Hoth_7B": {
        "why": "No terrain. A single edge clears the detector at 0.33 coverage with 6 "
               "gaps, under the 0.42 floor; the 4.88% blue is ice.",
        "confidence": "high",
    },
    "Hoth_14A": {
        "why": "No terrain. One edge at 0.33 coverage, under the floor. 3.9% blue is ice.",
        "confidence": "high",
    },
    "Hoth_1A": {
        "blocking": [(1, 2), (1, 3)],
        "difficult": [(0, 3), (0, 4), (0, 5)],
        "why": "An ice formation at (1,2),(1,3) in a solid red box at 0.99-1.00 with 0-1 "
               "gaps, and a lavender ice column down the left edge in solid blue at "
               "0.91-1.00. The tile is full of MIXED edges and this explains why: the two "
               "regions are ADJACENT, so (1,3)W is simultaneously the blue region's "
               "eastern boundary and the red box's western one, and both inks sit in the "
               "same sample band. Mixed does not mean ambiguous here -- it means two "
               "different terrains meet along that line, which is exactly what the art "
               "shows.",
        "confidence": "high",
    },
    "Hoth_5B": {
        "difficult": [(3, 4)],
        "blockingEdges": [(4, 2, "N"), (5, 2, "N"), (6, 4, "N"), (7, 4, "N")],
        "why": "An Echo Base armoury. Two equipment racks are printed as solid red edges "
               "at 0.92-1.00 with 0-1 gaps, and a single blue-boxed console at (3,4) comes "
               "back with boundary_perimeter 0. This tile reads 8.59% blue, nearly all of "
               "it the hangar's cold lighting, which is why most of its edges land at "
               "0.33-0.86 with 3-9 gaps and are discarded.",
        "confidence": "high",
    },
    "Hoth_6A": {
        "blocking": [(2, 3), (3, 3), (2, 4), (3, 4)],
        "difficult": [(0, 0), (1, 0), (3, 0), (4, 0),
                      (0, 1), (1, 1), (3, 1), (4, 1),
                      (0, 2), (1, 2), (3, 2), (4, 2),
                      (0, 4), (1, 4),
                      (0, 5), (1, 5), (3, 5), (4, 5),
                      (0, 6), (1, 6), (3, 6), (4, 6)],
        "why": "A crashed snowspeeder in a trench network. The wreck is a solid red 2x2 at "
               "1.00 coverage with 0 gaps: blocking. The blue lines outline a plus-shaped "
               "TRENCH -- column 2 for its full height, widening across rows 3-4 at the "
               "crash site -- and the difficult terrain is everything OUTSIDE it: the four "
               "banks of undisturbed snow, 22 squares, exactly the four 6/6/6/4-square "
               "components the region fill returns. "
               "boundary_perimeter is 0-1 for the trench pieces and 5 for the outer "
               "banks, which is why it cannot decide this one: a trench is a CLEARED "
               "path, dug out and walked along, with the deep snow beside it. See the "
               "note on enclosure in triage().",
        "confidence": "high",
    },
    "Hoth_2A": {
        "difficult": [(4, 2), (5, 2), (4, 3), (5, 3), (4, 4), (5, 4), (4, 5)],
        "impassableEdges": [(4, 1, "W"), (1, 2, "W"), (1, 3, "N"), (2, 3, "N"),
                            (3, 3, "N")],
        "why": "An ice cavern. The right-hand side is a lavender ice formation, visibly a "
               "different surface from the blue-white cavern floor, and it is ringed in "
               "solid blue at 0.92-1.00 with 0-1 gaps: difficult. Separately a dashed red "
               "line at 0.47-0.52 with 3-5 gaps traces a raised shelf down the left and "
               "across row 3. (4,2)W reports BOTH inks -- red dashed at 0.52 and blue "
               "solid at 1.00 -- and is taken as the blue region boundary only: the red "
               "there is the tail of the shelf line in the row above bleeding into the "
               "band, and 0.52 against a perfect 1.00 is not a close call. The blue "
               "readings at 6-10 gaps elsewhere are ice texture.",
        "confidence": "high",
    },
    "Hoth_6B": {
        "blocking": [(1, 3), (1, 4), (4, 2)],
        "why": "Two solid red boxes: a 1x2 at (1,3),(1,4) with boundary_perimeter 0, every "
               "edge 0.99-1.00 at 0-1 gaps, and a single square at (4,2) closing against "
               "the tile edge. Several edges report blue as well, but all of it sits at "
               "0.32-0.67 coverage -- below the 0.86 floor every confirmed solid marking "
               "clears -- and it is the cavern's ice texture, not a second marking.",
        "confidence": "high",
    },
    "Hoth_10B": {
        "difficult": [(0, 0)],
        "blocking": [(3, 3)],
        "why": "Two opposite corners, marked differently. (0,0) is boxed in solid blue by "
               "(1,0)W and (0,1)N, (3,3) in solid red by (3,3)N and (3,3)W, all four at "
               "0.91-0.92 with 2 gaps, each closing against the tile edges. A snowdrift "
               "you wade through and a rock you do not.",
        "confidence": "high",
    },
    "Hoth_12B": {
        "difficult": [(0, 0), (2, 1), (1, 2), (2, 2)],
        "why": "Snow piled against the walls of an Echo Base corridor. A solid blue box "
               "round the drift at (0,0), and a second line closing the L-shaped drift at "
               "(2,1),(1,2),(2,2) against the east and south tile edges. All six edges at "
               "0.91-1.00 with 0-2 gaps. The middle of the tile is metal grating and "
               "carries no marking, which matches: that is the part you walk on.",
        "confidence": "high",
    },
    "Jabba_1B": {
        "impassableEdges": [(0, 1, "N"), (0, 3, "N"), (1, 4, "N"), (2, 4, "N"),
                            (3, 4, "N"), (4, 4, "W"), (4, 5, "N")],
        "why": "A palace corridor with a raised dais in the lower middle and stairs on "
               "either side. All seven edges are red DASHED at 0.45-0.55 coverage with 4-5 "
               "gaps: five trace the dais lip, two the stair edges. Impassable, so figures "
               "cannot step up but can see and shoot over -- which is the point of a dais.",
        "confidence": "high",
    },
    "Jabba_2A": {
        "blockingEdges": [(3, 1, "W"), (3, 2, "W"), (2, 4, "N"), (3, 4, "N")],
        "impassableEdges": [(1, 1, "N"), (4, 1, "N"), (1, 3, "N"), (4, 3, "N")],
        "why": "Four solid edges at 0.99-1.00 with 0-1 gaps and four dashed at 0.57-0.59 "
               "with 5 gaps. Two tight clusters, nothing in between, so the split is the "
               "print rather than a threshold: walls where the palace has masonry, step "
               "edges where it has a level change. Nothing encloses, so all eight are "
               "edges rather than marked spaces.",
        "confidence": "high",
    },
    "Jabba_1A": {
        "difficult": [(2, 0), (3, 0), (3, 2), (2, 3), (1, 4), (0, 5), (1, 5)],
        "blocking": [(3, 1), (2, 2)],
        "why": "Palace floor with scattered obstacles. Four blue regions and two red ones, "
               "every edge solid at 0.86-1.00 and not one dashed reading on the whole "
               "tile. (3,2) and (2,3) come back at boundary_perimeter 0, the others close "
               "against tile edges.",
        "confidence": "high",
    },
    "Jabba_3B": {
        "difficult": [(0, 1), (2, 3), (0, 5), (1, 5), (1, 6)],
        "blocking": [(3, 2)],
        "why": "Three blue patches and one red rock, all solid at 0.86-1.00. (2,3) and "
               "(3,2) are fully enclosed; the rest close against the tile edge. The single "
               "dashed reading at (3,3)W is the rock's own shadow.",
        "confidence": "high",
    },
    "Jabba_4A": {
        "difficult": [(3, 2)],
        "blocking": [(0, 0)],
        "impassable": [(2, 1), (3, 1), (4, 1), (2, 4), (3, 4), (4, 4)],
        "why": "Two red DASHED bands across the tile at 4-5 gaps, three squares each, "
               "enclosing at boundary_perimeter 0 and 3: impassable, so figures cannot "
               "cross them but can shoot over. One blue-boxed square at (3,2) and a solid "
               "red box at (0,0). Voids at (3,0), (4,0), (5,0).",
        "confidence": "high",
    },
    "Jabba_4B": {
        "difficult": [(2, 3), (3, 3), (1, 4), (2, 4), (3, 4), (4, 4)],
        "blocking": [(1, 1)],
        "impassableEdges": [(2, 3, "N"), (2, 3, "W"), (3, 3, "N"), (4, 3, "W"),
                            (4, 4, "N"), (5, 4, "W")],
        "why": "The Sarlacc pit, and the only tile in the corpus where BOTH inks enclose "
               "exactly the same six squares: a solid blue boundary at 0 gaps and a red "
               "DASHED one at 5, running side by side round the depression. That is not a "
               "contradiction, it is the pit described precisely -- the sand inside is "
               "difficult, and the rim cannot be crossed. A figure already in there wades; "
               "a figure outside cannot walk in, but can shoot across. Machinery at (1,1) "
               "is solid-boxed. Voids at (0,0), (1,0), (2,0).",
        "confidence": "high",
    },
    "Jabba_5A": {
        "difficult": [(2, 0), (3, 0), (4, 0), (2, 2), (3, 2)],
        "impassableEdges": [(2, 1, "N"), (3, 1, "N"), (2, 2, "N"), (3, 2, "N")],
        "why": "Swamp with a wrecked structure lying across the middle. Two blue-boxed "
               "marsh patches at 0.86-1.00, above and below the wreck, and the wreck's own "
               "two long edges printed red dashed at 0.42-0.70 with 4-5 gaps. Note (2,2)N "
               "and (3,2)N carry both: they are the lower marsh region's northern boundary "
               "AND the wreck's southern lip, the same construction as Empire_4A.",
        "confidence": "high",
    },
    "Jabba_6A": {
        "difficult": [(1, 1), (0, 2), (1, 2), (0, 3), (0, 4), (1, 4)],
        "blocking": [(3, 2)],
        "impassableEdges": [(2, 0, "W"), (2, 1, "W")],
        "why": "Swamp floor. The blue outlines the wet ground down the west side -- "
               "visibly darker and greener than the dry brown to the east -- plus a "
               "separate reed patch at (1,1), all at 0.86-1.00. (1,3) is NOT included: (1,3)N and (1,3)W cut it off from the marsh, and the art shows "
               "drier, rockier ground there, so the marking goes round it. A rock at (3,2) "
               "is red-boxed, and the dashed vertical at the top is the same feature as on "
               "the 6B face.",
        "confidence": "high",
    },
    "Jabba_6B": {
        "blocking": [(1, 3), (3, 2)],
        "difficult": [(2, 2), (3, 3)],
        "blockingEdges": [(0, 2, "N"), (1, 2, "N")],
        "impassableEdges": [(2, 0, "W"), (2, 1, "W")],
        "why": "Desert rubble outside the palace. Two rock piles in solid red boxes, two "
               "patches of loose scree in solid blue boxes, all at 0.86-1.00. A red dashed "
               "vertical at (2,0)W and (2,1)W runs down from the top edge: impassable. "
               "(1,2)N reads dashed while (0,2)N beside it reads solid, but they are one "
               "continuous line with rubble lying across part of it -- the Lothal_3B case "
               "-- so both are taken as solid. A wall that stopped sight for half its "
               "length would be the odd reading here, not the consistent one.",
        "confidence": "high",
    },
    "Jabba_7A": {
        "blocking": [(2, 2)],
        "why": "One square boxed in solid red at 0.84-1.00, boundary_perimeter 0.",
        "confidence": "high",
    },
    "Jabba_9B": {
        "blockingEdges": [(2, 1, "W"), (2, 2, "W")],
        "why": "A single solid red vertical at 0.99-1.00 with 0-1 gaps, spanning rows 1-2 "
               "on the west edge of column 2. Encloses nothing: a wall.",
        "confidence": "high",
    },
    "Jabba_10A": {
        "blockingEdges": [(1, 1, "N"), (2, 1, "W"), (1, 2, "W"), (1, 3, "N")],
        "why": "Four solid red edges at 0.98-1.00 with 0-1 gaps, forming two L-shaped "
               "corners rather than a closed box, which is why the tile reports no region.",
        "confidence": "high",
    },
    "Jabba_10B": {
        "blockingEdges": [(1, 1, "W"), (0, 2, "N")],
        "impassableEdges": [(1, 1, "N"), (2, 1, "W")],
        "why": "The reverse of 10A, and it carries both styles: two solid at 0.92-0.99 "
               "with 1-2 gaps (walls) and two dashed at 0.49 with 4-5 (a step).",
        "confidence": "high",
    },
    "Jabba_12B": {
        "blockingEdges": [(2, 0, "W")],
        "why": "A single solid red edge at 0.91 with 1 gap. One wall, nothing else on the "
               "tile.",
        "confidence": "high",
    },
    "Jabba_2B": {
        "blocking": [(2, 4), (3, 4)],
        "blockingEdges": [(4, 3, "N")],
        "impassableEdges": [(4, 1, "W"), (4, 2, "W"), (1, 4, "N"), (1, 4, "W"),
                            (4, 4, "N"), (5, 4, "W")],
        "why": "Palace hall with a mounted gun and a staircase at each side. The gun sits "
               "in a closed solid red box -- (2,4)N, (2,4)W, (3,4)N, (4,4)W, (2,5)N, "
               "(3,5)N, every one at 0.91-1.00 coverage with 0-1 gaps and "
               "boundary_perimeter 0 -- so those two squares are blocking. Everything "
               "else is an edge marking and splits by gap count exactly as on 5B: (4,3)N "
               "solid at 0.91 is the landing wall, while the six edges round the two "
               "staircases sit at 0.49-0.55 with 4-5 gaps and are impassable. Only the "
               "gun box is listed as squares; the box's own six edges are not repeated as "
               "blockingEdges, since the region boundary already carries them.",
        "confidence": "high",
    },
    "Jabba_3A": {
        "impassableEdges": [(4, 2, "N"), (4, 2, "W"), (0, 3, "N"), (1, 3, "W"),
                            (3, 3, "N"), (3, 3, "W"), (1, 4, "N"), (2, 4, "W"),
                            (3, 4, "W"), (2, 5, "N")],
        "why": "A jungle ravine. All ten red edges are dashed -- coverage 0.55-0.64 with "
               "3-5 gaps, tightly clustered -- and they trace the lip of the drop rather "
               "than ringing any square, which is why the tile reports no small region. "
               "Dashed is IMPASSABLE, so the two sides of the ravine stay adjacent and "
               "line of sight crosses it; a figure just cannot step over the edge. Calling "
               "this blocking instead would silently cut sight lines across half the "
               "tile.",
        "confidence": "high",
    },
    "Jabba_5B": {
        "blocking": [(2, 0), (3, 0)],
        "blockingEdges": [(0, 2, "N"), (5, 2, "N"), (2, 3, "N"), (3, 3, "N")],
        "impassableEdges": [(1, 2, "N"), (4, 2, "N")],
        "why": "The rancor pit. It carries solid and dashed red on what looks like one "
               "continuous line. The "
               "creature alcove at the top is a closed solid box -- (2,0)W, (4,0)W, (2,1)N, "
               "(3,1)N against the tile edge -- so those two squares are blocking. Below "
               "it a ledge runs across the tile, stepping down to row 3 where the "
               "portcullis stands. The detector splits that ledge cleanly: 0.91-1.00 "
               "coverage with 0-1 gaps at (0,2)N, (5,2)N, (2,3)N and (3,3)N, against "
               "0.48-0.53 with 4-5 gaps at (1,2)N and (4,2)N. Two tight clusters with "
               "nothing between them, so this is a real distinction and not a threshold "
               "artefact: wall where the art shows masonry, railing where it shows an "
               "opening. The line deliberately does NOT close, because the gate at columns "
               "2-3 is the way through, which is also why region detection finds only the "
               "alcove.",
        "confidence": "high",
    },
    "Jabba_9A": {
        "difficult": [(1, 1), (2, 1), (1, 2), (2, 2)],
        "why": "A misty pool in a forest clearing, ringed by solid blue on all eight "
               "boundary edges -- boundary_perimeter 0, the cleanest possible enclosure. "
               "The strip at (1,0),(2,0) is not a second marking: it is the leftover floor "
               "between the box and the tile edge, hemmed in by the voids at (0,0) and "
               "(3,0), and it carries ink on only 2 of its boundary edges against the "
               "pool's 8.",
        "confidence": "high",
    },
    "Jabba_8A": {
        "difficult": [(0, 1), (1, 1), (2, 2), (3, 2), (2, 3)],
        "why": "Misty forest floor with two haze patches, both ringed in solid blue at "
               "coverage 0.92-1.00. This tile is why the region fill now honours void "
               "squares. Both patches close on one side against a void corner -- (0,0) "
               "for the left patch, (3,3) for the right -- so against the bare bounding "
               "box neither formed a loop and the fill leaked out and swallowed the whole "
               "top half. With voids treated as off-board the components come back as "
               "exactly {(0,1),(1,1)} and {(2,2),(3,2),(2,3)}, which is what the art "
               "shows.",
        "confidence": "high",
    },
    "Jabba_11A": {
        "difficult": [(2, 0), (3, 0)],
        "why": "Solid blue box, sides at (2,0)W and (4,0)W, floor at (2,1)N and (3,1)N, "
               "lid is the tile edge. Encloses two squares of piled junk. Coverage "
               "0.93-1.00, gaps 0-2.",
        "confidence": "high",
    },
    "Jabba_11B": {
        "difficult": [(0, 1), (1, 1), (3, 0), (3, 1)],
        "why": "Palace storeroom with TWO separate solid blue boxes, each round a heap of "
               "containers. The left one is closed by (0,1)N, (1,1)N and (2,1)W against "
               "the tile's west and south edges; the right one by (3,0)W, (3,1)W, (4,0)W "
               "and (4,1)W against the north and south edges. Coverage 0.86-0.98 "
               "throughout.",
        "confidence": "high",
    },
    "Jabba_12A": {
        "difficult": [(1, 0)],
        "why": "A single square of standing water in the jungle floor, boxed by (1,0)W, "
               "(2,0)W and (1,1)N with the tile edge closing the top. The enclosed "
               "component is 1 square against a 7-square complement, so which side is "
               "marked is not in question here.",
        "confidence": "high",
    },
    "Jabba_8B": {
        "difficult": [(1, 0), (0, 1), (1, 1)],
        "why": "Solid BLUE box round the raised machinery platform in the top-left. The "
               "detector reports a 4-square region including (0,0), but (0,0) is VOID "
               "from the alpha channel, so the real answer is THREE squares. Same trap "
               "as Core_7A: apply shape before terrain, or terrain lands on squares that "
               "are not part of the tile. (3,3) is also void.",
        "confidence": "high",
    },
    "Lothal_1A": {
        "blockingEdges": [(2, 2, "W"), (2, 3, "W"), (5, 2, "W"), (5, 3, "W")],
        "why": "Two solid red verticals at 1.00 coverage with 0 gaps, one down the west "
               "edge of column 2 and one down column 5, each spanning rows 2-3. They "
               "enclose nothing between them, so these are wall edges rather than a "
               "marked space. The blue reading at (3,4)W is discarded at 0.34.",
        "confidence": "high",
    },
    "Lothal_3A": {
        "blockingEdges": [(1, 2, "N"), (1, 2, "W"), (3, 3, "W"), (2, 4, "N")],
        "why": "Four solid red edges at 0.96-1.00 with 0-1 gaps, scattered round the "
               "machinery rather than ringing any square. No region, so no marked space: "
               "wall edges.",
        "confidence": "high",
    },
    "Lothal_3B": {
        "blockingEdges": [(2, 1, "N"), (3, 1, "W"), (1, 2, "W"), (0, 3, "N"),
                          (3, 3, "N"), (3, 3, "W"), (1, 4, "W"), (1, 5, "N")],
        "why": "Workbenches round the walls of a Lothal workshop, their edges printed in "
               "solid red. Six read cleanly at 0.93-1.00 with 0-2 gaps. The other two, "
               "(1,2)W and (3,3)W, measure 0.62-0.67 with 4 gaps and the detector calls "
               "them dashed -- but magnified they are CONTINUOUS solid lines interrupted "
               "where tools lying on the bench overlap them. So they are blocking too. "
               "This is the gap metric's blind spot in the opposite direction from the "
               "usual one: an occluded solid line lands in the same 0.42-0.66 coverage "
               "and 3-5 gap band as a genuine dashed one, and nothing about the numbers "
               "separates them. Two things settled it here. Under magnification the ink "
               "is continuous with objects on top, not a row of printed dashes. And the "
               "same bench is bounded by unambiguous solid lines on its other sides, so a "
               "single impassable edge would mean a bench that stops sight on three sides "
               "and not the fourth.",
        "confidence": "high",
    },
    "Lothal_1B": {
        "blocking": [(3, 1), (2, 2), (3, 2), (4, 2), (3, 3)],
        "impassable": [(2, 1), (4, 1), (2, 3), (4, 3)],
        "why": "A big circular rock formation, and the print distinguishes its core from "
               "its rim. The detector returns five regions all at boundary_perimeter 0: a "
               "PLUS of five squares and the four diagonal corners that complete the 3x3. "
               "The plus is bounded entirely by solid edges at 0.99-1.00 with 0-1 gaps, "
               "so blocking -- the solid rock. Each corner has its two inner edges shared "
               "with the plus and its two OUTER edges dashed at 0.58-0.62 with 3-4 gaps, "
               "so impassable -- the sloping rim, which you cannot climb but can see over. "
               "Getting this backwards would wall off sight lines across a quarter of the "
               "tile.",
        "confidence": "high",
    },
    "Lothal_2B": {
        "difficult": [(1, 1), (2, 1), (3, 1), (1, 2), (3, 2), (1, 3), (2, 3), (3, 3)],
        "blocking": [(2, 2)],
        "why": "The same rock formation as 1B seen from a different tile, and marked with "
               "two inks rather than two line styles. A solid blue ring at 0.99-1.00 "
               "encloses the 3x3 of broken ground round the sinkhole, and a solid red box "
               "at 0.99-1.00 marks its centre. So the rim is difficult and the core is "
               "blocking. The centre is listed only as blocking: it sits inside the blue "
               "ring, but a square that cannot be entered has no use for a movement "
               "surcharge.",
        "confidence": "high",
    },
    "Lothal_4A": {
        "blockingEdges": [(2, 1, "W"), (2, 2, "W"), (1, 2, "N"), (2, 2, "N")],
        "why": "Two solid red lines at 0.98-1.00 with 0-1 gaps meeting at a corner: a "
               "vertical down the west edge of column 2 across rows 1-2, and a horizontal "
               "along the top of row 2 across columns 1-2. They enclose nothing, which is "
               "why the tile reports no region -- this is a ledge corner, not a marked "
               "space. The two blue readings are discarded at 0.30 coverage, below the "
               "0.42 floor every confirmed dashed marking clears.",
        "confidence": "high",
    },
    "Lothal_4B": {
        "difficult": [(3, 0), (2, 1), (1, 2), (0, 3)],
        "why": "A rocky crevasse cutting diagonally across a Lothal canyon floor. Four "
               "single squares boxed in solid blue at 0.91-1.00 coverage with 0-2 gaps, "
               "and they trace the ravine exactly: (3,0), (2,1), (1,2), (0,3). The two "
               "interior ones come back with boundary_perimeter 0 and the two on the tile "
               "edge with 2, which is what a marking closing against the tile boundary "
               "should look like.",
        "confidence": "high",
    },
    "Lothal_5A": {
        "blocking": [(1, 1)],
        "why": "One square of rock boxed in solid red on all four sides at 0.98-0.99 "
               "coverage with 1 gap, boundary_perimeter 0.",
        "confidence": "high",
    },
    "Lothal_5B": {
        "difficult": [(0, 2)],
        "why": "A single blue-boxed square in the bottom-left, closed by (0,2)N and "
               "(1,2)W against the west and south tile edges, both at 0.91 with 2 gaps.",
        "confidence": "high",
    },
    "Lothal_6A": {
        "impassable": [(1, 1)],
        "why": "The centre square ringed by red, exactly like Lothal_6B on the reverse of "
               "the same cardboard -- but DASHED rather than solid, so impassable rather "
               "than blocking. This pair is the cleanest validation of the gap metric in "
               "the corpus, because the geometry is identical and only the print differs: "
               "6B measures 0.98-0.99 coverage with 1 gap on all four edges, 6A measures "
               "0.48-0.51 with 3-4. Same four edges, same enclosure, opposite line-of-"
               "sight behaviour.",
        "confidence": "high",
    },
    "Lothal_6B": {
        "blocking": [(1, 1)],
        "why": "Four solid red edges enclose (1,1) exactly, all at coverage 0.98-0.99 "
               "with 1 gap. The detector's 1-square region agrees.",
        "confidence": "high",
    },
    "Hoth_1B": {
        "why": "UNRESOLVED, left without terrain. The tile's 1.91% red comes "
               "from a large GLOWING GENERATOR at (3,3), and the two detected edges -- "
               "(3,3)N solid at 0.92 and (2,3)N dashed at 0.44 -- are as plausibly its "
               "housing and glow spill as they are markings. No box closes anywhere. "
               "Given Core_33A and Core_28B, glowing machinery is the single most "
               "reliable source of false positives, so this needs a closer look rather "
               "than a guess. Authoring it wrong would be invisible at runtime.",
        "confidence": "unresolved",
    },
    "Twin_1B": {
        "difficult": [(2, 2), (3, 2), (2, 3), (3, 3), (2, 4), (3, 4)],
        "blockingEdges": [(2, 0, "W"), (4, 0, "W"), (2, 1, "W"), (4, 1, "W"),
                          (3, 2, "W"), (3, 3, "W"),
                          (0, 4, "N"), (5, 4, "N"),
                          (0, 6, "N"), (1, 6, "N"), (4, 6, "N"), (5, 6, "N")],
        "why": "A cantina: booths round the walls and a horseshoe bar down the middle. "
               "The bar is a solid blue 2x3 at coverage 0.97-1.00 with 0-1 gaps and "
               "boundary_perimeter 0, so difficult -- you clamber over it. Twelve further "
               "edges are solid red and are walls: the back-room frame at the top, the "
               "booth dividers at rows 4 and 6, and (3,2)W and (3,3)W, which run down the "
               "INSIDE of the difficult region and are the bar's own solid back. An edge "
               "marking inside a terrain region is not a contradiction -- the squares are "
               "difficult to stand on and the counter still cannot be crossed.",
        "confidence": "high",
    },
    "Twin_4A": {
        "impassable": [(1, 3), (2, 3), (1, 4), (2, 4)],
        "why": "A circular holotable on a raised plinth, ringed by all eight edges of a "
               "red-and-white DASHED box at 0.51-0.56 coverage with 4-6 gaps, "
               "boundary_perimeter 0. Dashed means impassable, so the table stays adjacent "
               "and line of sight crosses it -- which matters here, because calling it "
               "blocking would cut every sight line across the middle of the room. The "
               "tile also reads 5.55% blue, all of it the table's own glow: those readings "
               "run 5 to 15 gaps and none of them is a marking. (4,2)W is the clearest "
               "case, since (4,2) is void -- that edge is the tile's own outline lit from "
               "beneath.",
        "confidence": "high",
    },
    "Twin_6A": {
        "blocking": [(2, 0), (2, 1)],
        "why": "An engine assembly filling the middle column of the top two rows, boxed by "
               "(2,0)W, (3,0)W, (2,1)W, (3,1)W and (2,2)N at 0.92-1.00 coverage with 0-1 "
               "gaps, closing against the north tile edge. The detector splits the two "
               "squares into separate regions because of a dashed red reading at (2,1)N, "
               "but that is the machine's own banding: a dashed impassable edge BETWEEN "
               "two blocking squares would be meaningless, since neither can be entered "
               "from either side. The mixed blue at (2,2)N is the lit floor panel below "
               "the box, not a second marking.",
        "confidence": "high",
    },
    "Twin_2B": {
        "difficult": [(1, 1), (2, 1), (1, 2), (2, 2)],
        "why": "A 2x2 spill of crates, tools and scrap across the middle of a storeroom, "
               "ringed in solid blue on all six boundary edges at 0.91-1.00 coverage with "
               "0-2 gaps. boundary_perimeter 0, so the enclosure is complete and which "
               "side is marked is not in question. Voids at (3,2), (2,3) and (3,3).",
        "confidence": "high",
    },
    "Twin_3B": {
        "blocking": [(0, 2)],
        "why": "One square of solid machinery in the bottom-left corner, boxed by (0,2)N "
               "at 0.91 and (1,2)W at 0.88, both 1 gap, closing against the west and "
               "south tile edges. The third reading on this tile, (1,2)N in blue, is "
               "discarded at 13 gaps -- far outside the 3-5 a dashed marking measures. "
               "Void at (2,0).",
        "confidence": "high",
    },
    "Twin_7A": {
        "blocking": [(1, 2)],
        "why": "A single blocking square at (1,2), enclosed by four solid red edges at "
               "0.98-0.99 with 1 gap each and boundary_perimeter 0. The two blue readings "
               "at 9 and 17 gaps are art, not markings. The shredded-ink rule does not "
               "auto-clear the tile despite those two, because it only clears when no "
               "edge on the tile looks like a line.",
        "confidence": "high",
    },
    "Twin_1A": {
        "blocking": [(0, 7), (5, 7)],
        "impassableEdges": [(0, 2, "N"), (1, 2, "W"), (5, 2, "N"), (5, 2, "W"),
                            (1, 3, "N"), (2, 3, "W"), (4, 3, "N"), (4, 3, "W"),
                            (2, 4, "W"), (4, 4, "W"), (2, 5, "W"), (4, 5, "W"),
                            (2, 6, "W"), (4, 6, "W")],
        "why": "A hangar. DASHED red traces the stepped boundary between the central "
               "corridor and the equipment alcoves down both flanks -- 14 impassable "
               "edges, all measured at coverage 0.46-0.61 with 4-6 gaps. Four SOLID "
               "edges at the bottom corners enclose blocking squares (0,7) and (5,7), "
               "their remaining sides being the tile boundary. Authored from "
               "edge_report; the image was used only to confirm the ink is a marking "
               "rather than decoration.",
        "confidence": "high",
    },
    "Lothal_2A": {
        "blocking": [(1, 1), (3, 1), (1, 3), (3, 3)],
        "why": "Four solid red boxes over rubble piles, matching the detector's four "
               "single-square regions exactly. All 16 bounding edges measure coverage "
               "0.98-1.00 with 0-1 gaps: unambiguously solid.",
        "confidence": "high",
    },
    "Lothal_7B": {
        "impassable": [(2, 2)],
        "why": "A DASHED red boundary rings the rubble in the bottom-right corner, "
               "closing against the tile edges on its other two sides. Measured at "
               "(2,2)N coverage 0.52 / 7 gaps and (2,2)W coverage 0.47 / 5 gaps -- "
               "clearly dashed, so impassable rather than blocking, and line of sight "
               "still passes across it.",
        "confidence": "high",
    },
    "Hoth_5A": {
        "impassableEdges": [(0, 2, "N"), (1, 2, "N"), (6, 2, "W"),
                            (4, 3, "N"), (4, 3, "W"), (5, 3, "N"),
                            (2, 4, "N"), (3, 4, "N")],
        "why": "A stepped ridge marked in DASHED red -- impassable edges, placed from "
               "edge_report rather than by eye (coverage ~0.50, gaps 4-5 on every one). "
               "The tile also reports blue on many edges, but Hoth tiles are blue-white "
               "SNOW and that tint contaminates blue detection across the expansion; "
               "those readings are treated as noise, not markings. One exception is "
               "(2,4) W at coverage 0.97 gaps 1 -- genuinely solid blue -- which is left "
               "UNRESOLVED rather than guessed.",
        "confidence": "medium",
    },
    "Hoth_20A": {
        "why": "NO terrain. A plain snow tile. The 1.03% 'blue' is the SNOW ITSELF -- "
               "Hoth tiles are inherently blue-white, so ambient tint will register "
               "across that entire expansion and must never be read as a marking. "
               "Nothing follows a grid edge.",
        "confidence": "high",
    },
    "Empire_18A": {
        "why": "NO terrain. A Cloud City balcony in pale blue and lavender; 0.15% blue "
               "and nothing on a grid edge.",
        "confidence": "high",
    },
    "Core_6B": {
        "blocking": [(2, 2)],
        "impassableEdges": [(0, 1, "N"), (1, 1, "W"), (3, 1, "W"), (1, 2, "W")],
        "why": "TWO red conventions on one tile. A SOLID red box rings the machinery at "
               "(2,2) -- blocking terrain. Separately, DASHED red lines run along square "
               "EDGES on the left flank and beside (3,1) -- impassable edges, which stop "
               "movement but leave the spaces adjacent and let line of sight through. "
               "Square-only authoring would have silently dropped the dashed edges. "
               "(3,0) is void, from the alpha channel. CORRECTED: the edge placements "
               "here were originally eyeballed and 2 of 3 were wrong -- they now come "
               "from edge_report, which measures coverage and gap count per edge exactly.",
        "confidence": "high",
    },
}

# Ink below this is indistinguishable from compression noise and stray palette
# pixels; measured, no tile with a real marking falls under it.
INK_FLOOR = 0.05

# Mean saturated ink inside the squares themselves, above which the ink is a
# FILL rather than a marking.
#
# A printed marking is a thin line drawn ALONG square boundaries, so it leaves
# the square interiors clean and this average stays near zero. Coloured artwork
# -- a lit panel, a glowing vent, a tinted floor -- floods the interiors instead.
#
# Measured over every reviewed face: the 35 that carry real terrain top out at
# 0.056 (Core_20B), while the artwork that defeats a plain hue threshold sits at
# 0.097 (Core_28B's lit orb), 0.139 (Core_27A's grille), 0.152 (Bespin_6B's
# tinted rotunda) and 0.181 (Core_33A's glowing vent). 0.08 sits in that gap.
#
# The test is ONE-DIRECTIONAL and must stay that way: high means artwork, low
# means nothing at all, since a plain unmarked tile also scores near zero.
#
# Note this is the average and not the maximum. The maximum does not separate
# the two cases at all (0.12-0.36 on both sides), because even a correctly
# marked tile has some square whose inset box the line clips.
FILL_MEAN_INK = 0.08

# Gap count, above which an inked edge is too shredded to be a printed line.
#
# "Gaps" counts the breaks in the run of ink along an edge's sample band. A
# solid marking measures 0-2 and a dashed one 3-5, tightly. Artwork that merely
# crosses or floods an edge measures far higher, because nothing about it is
# aligned to the grid: a circular platform rim, a lit panel's soft border, a
# textured floor.
#
# Measured over every reviewed face that registers any edge at all: of those
# carrying real terrain, the WORST case still has some edge down at 5
# (Lothal_7B), while the artwork sits at 7, 10 and 11. 7 is the cut.
#
# The test is over the MINIMUM across the tile's edges, so it clears a tile only
# when NOT ONE of its edges looks like a line. A tile with both -- Empire_8B has
# two solid markings at gaps 1-2 alongside a piece of red art at gaps 8 -- is
# correctly kept for review rather than cleared on the strength of the art.
#
# One-directional, like the two tests above it.
SHREDDED_MIN_GAPS = 7


def mean_square_ink(face, dims):
    """Average saturated ink inside the squares, ignoring their boundaries.

    This is what distinguishes a printed marking from coloured art. See
    FILL_MEAN_INK for the measurement the threshold rests on.
    """
    exp, rest = face.split("_")
    tid = rest[:-1]
    if (exp, tid) not in dims:
        return 0.0
    path = os.path.join(td.TILES, exp, face + ".png")
    if not os.path.exists(path):
        return 0.0
    w, h = dims[(exp, tid)]
    im = Image.open(path).convert("RGB")
    a = np.asarray(im).astype(int)
    px, py = im.size[0] / w, im.size[1] / h
    best = 0.0
    for mask in td.masks(a):
        vals = [td.square_ink(mask, px, py, c, r) for r in range(h) for c in range(w)]
        best = max(best, sum(vals) / len(vals))
    return best


# Above this fraction of a tile, a marking PARTITIONS the tile rather than
# ringing something on it, and "which side is the terrain" stops being obvious.
#
# The distinction is not cosmetic. When ink rings a crate, a rock or a console,
# the marked side is the object and nothing else is plausible. When it splits a
# tile into two comparable halves, both readings are coherent, the geometry is
# silent, and a wrong choice is invisible -- the engine is perfectly
# self-consistent either way.
#
# Two measurements support treating this as its own category, and both are
# negative results:
#
#   Enclosure does not decide it. boundary_perimeter picks the ENCLOSED side on
#   Core_19B (correct) and the enclosed side on Hoth_6A (wrong). Opposite
#   answers on the two known cases, so it carries no signal here.
#
#   Neither does which side the ink sits on. "IA draws a space's border just
#   inside that space" predicts the marked square 8 times in 17 on Core_19B and
#   14 in 25 on Hoth_6A -- 47% and 56%, indistinguishable from a coin toss.
#
# So only the ART decides, and only by asking what the ground IS rather than
# where the line runs. Both known inversions were semantic rather than
# geometric: the swamp in the middle of Core_19B, and the trench on Hoth_6A,
# which is a cleared path with the deep snow piled beside it.
PARTITION_FRACTION = 0.40


def partition_style(face, dims, verdict):
    """Does this verdict mark a partition rather than an object on the tile?"""
    if not verdict:
        return False
    marked = sum(len(verdict.get(k, [])) for k in
                 ("difficult", "blocking", "impassable", "pit"))
    if marked == 0:
        return False
    exp, rest = face.split("_")
    tid = rest[:-1]
    if (exp, tid) not in dims:
        return False
    w, h = dims[(exp, tid)]
    shape = ts.shape_of(face, dims) or []
    playable = w * h - sum(row.count("-") for row in shape)
    return playable > 0 and marked / playable >= PARTITION_FRACTION


def triage(face, dims):
    """Classify how much review a tile face actually needs.

    Three automatic exits, all of which are real results rather than shortcuts:

      no-ink          no saturated red or blue anywhere, so there is nothing
                      printed to interpret.
      no-inked-edge   ink exists, but no grid edge anywhere on the tile carries
                      a run of it. A terrain marking is drawn ALONG square
                      boundaries by definition, so ink that never lands on an
                      edge cannot be a marking -- it is art. This is the
                      strongest automatic clear available, and unlike the ink
                      fraction it was measured rather than assumed: across the
                      reviewed faces that carry real terrain, 31 of 31 register
                      at least one inked edge, so the test has no known false
                      clear, and one-directional: edges==0
                      clears a tile, edges>0 proves nothing (7 of 25 confirmed
                      terrain-free faces still register an edge), which is why
                      the other direction still goes to the art.
      no-closed-region  ink is present but forms no closed region. A terrain
                      marking SURROUNDS a space; ink that encloses nothing is
                      artwork -- a glowing vent, a lit orb, a painted banner.
                      This still warrants a glance, so it is reported, not
                      silently cleared.

    Anything else needs a semantic pass over the art.
    """
    r = td.candidate_regions(face, dims)
    if not r:
        return "unknown", None
    ink = max(r["red_frac"], r["blue_frac"])
    if ink <= INK_FLOOR:
        return "no-ink", r
    rep = td.edge_report(face, dims)
    if rep is not None and not rep["edges"]:
        return "no-inked-edge", r
    if mean_square_ink(face, dims) >= FILL_MEAN_INK:
        return "ink-fills-squares", r
    if min(e["gaps"] for e in rep["edges"]) >= SHREDDED_MIN_GAPS:
        return "ink-too-shredded", r
    if not r["regions"]:
        return "no-closed-region", r
    return "needs-review", r


def build(expansion="Core"):
    dims = td.load_dimensions()
    walls = load_walls()
    tiles = []
    faces = sorted(
        f[:-4] for f in os.listdir(os.path.join(td.TILES, expansion)) if f.endswith(".png")
    )

    reviewed = unreviewed = 0
    for face in faces:
        exp, rest = face.split("_")
        tid, side = rest[:-1], rest[-1]
        if (exp, tid) not in dims:
            continue
        w, h = dims[(exp, tid)]
        shape = ts.shape_of(face, dims) or ["." * w for _ in range(h)]
        grid = [list(row) for row in shape]

        v = VERDICTS.get(face)
        kind, _ = triage(face, dims)
        source = "shape-only"
        if v:
            for glyph, key in (("d", "difficult"), ("X", "blocking"),
                               ("I", "impassable"), ("P", "pit")):
                for (c, r) in v.get(key, []):
                    if 0 <= r < h and 0 <= c < w and grid[r][c] != "-":
                        grid[r][c] = glyph
            source = "auto-shape+reviewed"
            reviewed += 1
        elif kind == "no-ink":
            source = "auto-shape+no-ink"
            reviewed += 1
        elif kind == "no-inked-edge":
            source = "auto-shape+no-inked-edge"
            reviewed += 1
        elif kind == "ink-fills-squares":
            source = "auto-shape+ink-fills-squares"
            reviewed += 1
        elif kind == "ink-too-shredded":
            source = "auto-shape+ink-too-shredded"
            reviewed += 1
        elif kind == "no-closed-region":
            source = "auto-shape+ink-no-region"
            unreviewed += 1
        else:
            unreviewed += 1

        entry = {
            "tileId": tid,
            "side": side,
            "width": w,
            "height": h,
            "squares": ["".join(row) for row in grid],
            "source": source,
            "verified": bool(v and v.get("confidence") == "verified"),
        }

        # Edge-level terrain. Imperial Assault prints impassable and blocking on
        # a single EDGE of a space as often as around a whole space, and a
        # square-only schema drops those silently -- the AI then walks straight
        # through a dashed red line nobody told it about.
        edges = []
        for key, etype in (("impassableEdges", "impassable"),
                           ("blockingEdges", "blocking"),
                           ("wallEdges", "wall")):
            for (c, r, d) in (v or {}).get(key, []):
                edges.append({"sq": [c, r], "dir": d, "type": etype})
        # The printed border. Without it every tile blends into its
        # neighbours and a closed door can be walked around.
        wf = walls.get(face)
        if wf:
            seen = {(e["sq"][0], e["sq"][1], e["dir"]) for e in edges}
            for (c, r, d) in wf.get("walls", []):
                if (c, r, d) not in seen:
                    edges.append({"sq": [c, r], "dir": d, "type": "wall"})
            if wf.get("unsure"):
                entry["wallsUnsure"] = [[c, r, d] for (c, r, d) in wf["unsure"]]
        if edges:
            entry["edges"] = edges
        if v and v.get("why"):
            entry["why"] = v["why"]
        if v and v.get("confidence"):
            entry["confidence"] = v["confidence"]
        entry["triage"] = kind
        tiles.append(entry)

    return {
        "schemaVersion": 1,
        "expansion": expansion,
        "_comment": [
            "Shape ('-') is exact, from the art's alpha channel, and needs no review.",
            "Terrain glyphs come from a reading of the art and carry a reason.",
            "Entries with source 'shape-only' are UNREVIEWED for terrain: they are not",
            "asserting the tile has no terrain, only that nobody has looked yet.",
        ],
        "tiles": tiles,
    }, reviewed, unreviewed


def expansions():
    return sorted(d for d in os.listdir(td.TILES)
                  if os.path.isdir(os.path.join(td.TILES, d)))


def main():
    report_only = "--report" in sys.argv
    only = [a for a in sys.argv[1:] if not a.startswith("--")]
    targets = only or expansions()

    # Partition-style verdicts are listed every build rather than counted
    # once, because they are the only class of terrain error that has actually
    # shipped here: twice, both caught by a person at the table rather than by
    # anything in this pipeline.
    dims_all = td.load_dimensions()
    partitions = [f for f in sorted(VERDICTS)
                  if partition_style(f, dims_all, VERDICTS[f])]

    grand = {"faces": 0, "reviewed": 0, "unreviewed": 0, "void": 0, "terrain": 0}
    for exp in targets:
        data, reviewed, unreviewed = build(exp)
        tiles = data["tiles"]
        voids = sum(row.count("-") for t in tiles for row in t["squares"])
        terr = sum(sum(row.count(g) for g in "dXIP") for t in tiles for row in t["squares"])
        grand["faces"] += len(tiles)
        grand["reviewed"] += reviewed
        grand["unreviewed"] += unreviewed
        grand["void"] += voids
        grand["terrain"] += terr
        print(f"{exp:<8} faces={len(tiles):<4} reviewed={reviewed:<4} "
              f"unreviewed={unreviewed:<4} void={voids:<4} terrain={terr}")
        if not report_only:
            os.makedirs(OUT_DIR, exist_ok=True)
            out = os.path.join(OUT_DIR, f"{exp}.json")
            with open(out, "w", encoding="utf-8") as f:
                json.dump(data, f, indent="\t")
                f.write("\n")
    if partitions:
        print()
        print(f"PARTITION-STYLE ({len(partitions)}): the marking splits the tile rather "
              "than ringing an object on it,")
        print("so which side is terrain rests on the ART alone -- no metric decides it.")
        for f in partitions:
            print(f"   {f:12s} {VERDICTS[f].get('confidence', '?')}")

    print(f"{'TOTAL':<8} faces={grand['faces']:<4} reviewed={grand['reviewed']:<4} "
          f"unreviewed={grand['unreviewed']:<4} void={grand['void']:<4} "
          f"terrain={grand['terrain']}")
    return 0


def _old_main():
    report_only = "--report" in sys.argv
    data, reviewed, unreviewed = build()
    tiles = data["tiles"]
    voids = sum(row.count("-") for t in tiles for row in t["squares"])
    terrain = sum(sum(row.count(g) for g in "dXIP") for t in tiles for row in t["squares"])

    from collections import Counter
    kinds = Counter(t["triage"] for t in tiles)
    print(f"tile faces        : {len(tiles)}")
    print(f"reviewed          : {reviewed}")
    print(f"unreviewed        : {unreviewed}")
    print(f"  no ink printed  : {kinds['no-ink']}  (nothing to interpret)")
    print(f"  ink, no region  : {kinds['no-closed-region']}  (likely artwork, worth a glance)")
    print(f"  needs a pass    : {kinds['needs-review']}")
    print(f"void squares      : {voids}   (from alpha, exact)")
    print(f"terrain squares   : {terrain} (from review)")

    if report_only:
        return 0
    os.makedirs(OUT_DIR, exist_ok=True)
    out = os.path.join(OUT_DIR, f"{data['expansion']}.json")
    with open(out, "w", encoding="utf-8") as f:
        json.dump(data, f, indent="\t")
        f.write("\n")
    print(f"wrote {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())

"""Turn a dispute captured at the table into a regression test.

This is the point of capturing one. Simulation is perfectly self-consistent
with whatever terrain it was told about, so terrain misread from a picture
stays misread forever -- the people at the table are the only ones who can see
it, and they see it once, mid-mission. Without this step a dispute file is a
bug report that ages; with it, the argument becomes a test that cannot regress.

    python tools/import_dispute.py <dispute.json> [--name Something]

Writes tests/BoardTests/Generated/Dispute_<name>.cs. The generated test asserts
only that the board REBUILDS and that the recorded orders replay legally -- it
deliberately does NOT assert the recorded order was correct, because the whole
reason the file exists is that somebody thought it was wrong. A human decides
what the right answer was and edits the expectation in.
"""

import argparse
import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUTDIR = os.path.join(ROOT, "tests", "BoardTests", "Generated")


def safe(name):
    cleaned = re.sub(r"[^A-Za-z0-9]+", "_", name or "").strip("_")
    return cleaned or "Unnamed"


def cs(value):
    return '"%s"' % (value or "").replace("\\", "\\\\").replace('"', '\\"')


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("snapshot")
    ap.add_argument("--name", default=None)
    args = ap.parse_args()

    snap = json.load(io.open(args.snapshot, encoding="utf-8-sig"))

    name = safe(args.name or "%s_r%s" % (snap.get("missionId") or "mission",
                                         snap.get("round") or 0))
    squares = snap.get("squares") or []
    edges = snap.get("edges") or []
    figures = snap.get("figures") or []
    if not squares:
        print("the snapshot has no board in it; nothing to import")
        return 1

    lines = []
    for s in squares:
        flags = " | ".join("SquareFlags." + f.strip()
                           for f in (s.get("flags") or "None").split(",")) or "SquareFlags.None"
        lines.append("\t\t\tb.SetSquare( new Sq( %d, %d ), %s );" % (s["c"], s["r"], flags))
    for e in edges:
        lines.append("\t\t\tb.SetEdge( new Sq( %d, %d ), EdgeDir.%s, EdgeType.%s );"
                     % (e["c"], e["r"], e["dir"], e["type"]))

    figure_lines = []
    for f in figures:
        figure_lines.append("\t\t\t\t// %s %s at (%d,%d)"
                            % ("REBEL " if f.get("hostile") else "enemy",
                               f.get("name") or f.get("id") or "?", f["c"], f["r"]))

    orders = "\n".join("\t\t\t//   " + (o or "").replace("*/", "*_/")
                       for o in (snap.get("orders") or []))
    trace = "\n".join("\t\t\t//   " + (t or "").replace("*/", "*_/")
                      for t in (snap.get("trace") or []))

    text = '''// GENERATED from a rules dispute captured at the table.
// Regenerate with: python tools/import_dispute.py <dispute.json>
//
// Captured %s, mission %s, round %s.
// Reason given: %s
//
// ORDERS AS GIVEN
%s
//
// WHY THE APP SAID SO
%s
//
// This test asserts the board rebuilds and the recorded orders replay legally.
// It does NOT assert the order was right -- the file exists because somebody
// thought it was wrong. Decide what should have happened and add it below.
using static Saga.Board.Tests.Harness;

namespace Saga.Board.Tests
{
\tpublic static class Dispute_%s
\t{
\t\tpublic static BoardModel Board()
\t\t{
\t\t\tvar b = new BoardModel();
%s
\t\t\treturn b;
\t\t}

\t\tpublic static void Register()
\t\t{
\t\t\tSuite( "dispute: %s" );

\t\t\tTest( "the disputed board rebuilds", () =>
\t\t\t{
\t\t\t\tvar b = Board();
\t\t\t\tEq( %d, b.Count, "every square recorded is on the board" );
%s
\t\t\t} );
\t\t}
\t}
}
''' % (snap.get("capturedUtc"), snap.get("missionId"), snap.get("round"),
       (snap.get("reason") or "(none given)").replace("\n", " "),
       orders or "//   (none recorded)",
       trace or "//   (none recorded)",
       name, "\n".join(lines), name.replace("_", " "), len(squares),
       "\n".join(figure_lines))

    os.makedirs(OUTDIR, exist_ok=True)
    out = os.path.join(OUTDIR, "Dispute_%s.cs" % name)
    io.open(out, "w", encoding="utf-8", newline="\n").write(text)
    print("wrote %s" % os.path.relpath(out, ROOT))
    print("   %d squares, %d edges, %d figures, %d order(s)"
          % (len(squares), len(edges), len(figures), len(snap.get("orders") or [])))
    print("   register it in tests/BoardTests/Program.cs as Dispute_%s.Register();" % name)
    return 0


if __name__ == "__main__":
    sys.exit(main())

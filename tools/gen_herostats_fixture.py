"""Generate a test fixture from the checked-in hero sheets.

Reads the JSON that tools/fetch_herostats.py produced, so the suite exercises
the data the game actually ships without needing a network. Refreshing the
numbers is a separate, manual step.
"""

import io
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(ROOT, "ImperialCommander2", "Assets", "Resources",
                      "CardData", "herostats.json")
OUT = os.path.join(ROOT, "tests", "BoardTests", "Generated", "HeroSheets.cs")


def main():
    doc = json.load(io.open(SOURCE, encoding="utf-8-sig"))
    heroes = doc["heroes"]

    rows = []
    for h in heroes:
        defense = ", ".join('"%s"' % d for d in h.get("defense") or [])
        rows.append(
            '\t\t\tnew HeroSheet { Id = "%s", Name = "%s", Health = %d, '
            'Endurance = %d, Speed = %d, Defense = new string[] { %s } },'
            % (h["id"], h["name"].replace('"', '\\"'), h["health"],
               h["endurance"], h["speed"], defense))

    text = '''// GENERATED from ImperialCommander2/Assets/Resources/CardData/herostats.json
// Regenerate with: python tools/gen_herostats_fixture.py
// Refresh the numbers themselves with: python tools/fetch_herostats.py
namespace Saga.Board.Tests
{
\tpublic sealed class HeroSheet
\t{
\t\tpublic string Id;
\t\tpublic string Name;
\t\tpublic int Health;
\t\tpublic int Endurance;
\t\tpublic int Speed;
\t\tpublic string[] Defense;
\t}

\tpublic static class HeroSheets
\t{
\t\tpublic static readonly HeroSheet[] All = new HeroSheet[]
\t\t{
%s
\t\t};
\t}
}
''' % "\n".join(rows)

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    io.open(OUT, "w", encoding="utf-8", newline="\n").write(text)
    health = [h["health"] for h in heroes]
    print("%d hero sheets -> %s" % (len(heroes), os.path.relpath(OUT, ROOT)))
    print("   health %d-%d, %d distinct values"
          % (min(health), max(health), len(set(health))))
    return 0


if __name__ == "__main__":
    sys.exit(main())

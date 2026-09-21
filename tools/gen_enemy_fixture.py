"""Generate a test fixture of every shipped enemy card's board profile.

The engine takes speed, attack type, footprint and keywords from the deployment
card. Those fields are FFG's data, not ours, so the mapping is only correct for
as long as the spellings hold: a new miniSize value or a renamed keyword would
silently fall back to the defaults and every figure of that card would be
planned as a ranged 1x1 moving 4, which is exactly the failure this generator
exists to catch.

Emitting the cards as a fixture keeps file I/O out of the test project, the
same way the mission placements are handled.
"""

import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(ROOT, "ImperialCommander2", "Assets", "Resources",
                      "CardData", "enemies.json")
OUT = os.path.join(ROOT, "tests", "BoardTests", "Generated", "EnemyCards.cs")

# The shipped JSON carries trailing commas, which Newtonsoft accepts and a
# strict parser does not.
TRAILING_COMMA = re.compile(r",(\s*[}\]])")


def load(path):
    raw = io.open(path, encoding="utf-8-sig").read()
    return json.loads(TRAILING_COMMA.sub(r"\1", raw))


def escape(s):
    return (s or "").replace("\\", "\\\\").replace('"', '\\"')


def main():
    cards = load(SOURCE)

    rows = []
    for c in cards:
        keywords = ", ".join('"%s"' % escape(k) for k in (c.get("keywords") or []))
        rows.append(
            '\t\t\tnew EnemyCard { Name = "%s", Id = "%s", AttackType = "%s", '
            'MiniSize = "%s", Speed = %d, Keywords = new string[] { %s } },'
            % (escape(c.get("name")), escape(c.get("id")),
               escape(c.get("attackType")), escape(c.get("miniSize")),
               int(c.get("speed") or 0), keywords))

    body = "\n".join(rows)
    text = '''// GENERATED from ImperialCommander2/Assets/Resources/CardData/enemies.json
// Regenerate with: python tools/gen_enemy_fixture.py
// FFG's own card data, so the mapping is exercised against every unit that
// actually ships rather than against hand-written examples.
namespace Saga.Board.Tests
{
\tpublic sealed class EnemyCard
\t{
\t\tpublic string Name;
\t\tpublic string Id;
\t\tpublic string AttackType;
\t\tpublic string MiniSize;
\t\tpublic int Speed;
\t\tpublic string[] Keywords;
\t}

\tpublic static class EnemyCards
\t{
\t\tpublic static readonly EnemyCard[] All = new EnemyCard[]
\t\t{
%s
\t\t};
\t}
}
''' % body

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    io.open(OUT, "w", encoding="utf-8", newline="\n").write(text)

    melee = sum(1 for c in cards if c.get("attackType") == "Melee")
    large = sum(1 for c in cards if c.get("miniSize") != "Small1x1")
    print("%d enemy cards -> %s" % (len(cards), os.path.relpath(OUT, ROOT)))
    print("   %d melee, %d larger than 1x1, speeds %d-%d"
          % (melee, large,
             min(c.get("speed") or 0 for c in cards),
             max(c.get("speed") or 0 for c in cards)))
    return 0


if __name__ == "__main__":
    sys.exit(main())

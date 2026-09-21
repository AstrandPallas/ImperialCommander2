"""Generate a fixture of every hero's class deck from the English skills file.

Every one of the shipped decks obeys the same shape: one card at 0 XP, then
exactly two each at 1, 2, 3 and 4. A deck that breaks it desynchronises its
hero's XP economy from all the others, which is the failure mode worth a test
rather than a comment -- especially now that this fork authors one.
"""

import io
import json
import os
import re
import sys
from collections import Counter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(ROOT, "ImperialCommander2", "Assets", "Resources",
                      "Languages", "En", "CampaignData", "skills.json")
OUT = os.path.join(ROOT, "tests", "BoardTests", "Generated", "ClassDecks.cs")

TRAILING_COMMA = re.compile(r",(\s*[}\]])")


def main():
    raw = io.open(SOURCE, encoding="utf-8-sig").read()
    rows = json.loads(TRAILING_COMMA.sub(r"\1", raw))

    lines = []
    for r in rows:
        lines.append(
            '\t\t\tnew ClassCard { Owner = "%s", Id = "%s", Name = "%s", Cost = %d },'
            % (r.get("owner", ""), r.get("id", ""),
               (r.get("name") or "").replace("\\", "\\\\").replace('"', '\\"'),
               int(r.get("cost") or 0)))

    text = '''// GENERATED from ImperialCommander2/Assets/Resources/Languages/En/CampaignData/skills.json
// Regenerate with: python tools/gen_skills_fixture.py
namespace Saga.Board.Tests
{
\tpublic sealed class ClassCard
\t{
\t\tpublic string Owner;
\t\tpublic string Id;
\t\tpublic string Name;
\t\tpublic int Cost;
\t}

\tpublic static class ClassDecks
\t{
\t\tpublic static readonly ClassCard[] All = new ClassCard[]
\t\t{
%s
\t\t};
\t}
}
''' % "\n".join(lines)

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    io.open(OUT, "w", encoding="utf-8", newline="\n").write(text)

    owners = Counter(r.get("owner") for r in rows)
    odd = {o: n for o, n in owners.items() if n != 9}
    print("%d class cards across %d heroes -> %s"
          % (len(rows), len(owners), os.path.relpath(OUT, ROOT)))
    if odd:
        print("   decks that are not 9 cards: %s" % odd)
    return 0


if __name__ == "__main__":
    sys.exit(main())

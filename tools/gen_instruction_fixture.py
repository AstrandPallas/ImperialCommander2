"""Generate a fixture of every shipped activation instruction line.

The planner now reads a card's lines and does the first it can. The grammar
that reads them is small on purpose, so this fixture exists to measure it
against all 521 real lines rather than the handful in the tests: a change to
the wording upstream, or a shape the parser never saw, shows up as a count
here instead of as a figure that quietly stops following its card.
"""

import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(ROOT, "ImperialCommander2", "Assets", "Resources",
                      "Languages", "En", "instructions.json")
OUT = os.path.join(ROOT, "tests", "BoardTests", "Generated", "InstructionLines.cs")
TRAILING_COMMA = re.compile(r",(\s*[}\]])")


def cs(s):
    return '"%s"' % (s or "").replace("\\", "\\\\").replace('"', '\\"')


def main():
    raw = io.open(SOURCE, encoding="utf-8-sig").read()
    cards = json.loads(TRAILING_COMMA.sub(r"\1", raw))
    rows = []
    for c in cards:
        for i, opt in enumerate(c.get("content") or []):
            for j, line in enumerate(opt.get("instruction") or []):
                rows.append('\t\t\tnew InstructionLine { CardId = %s, Card = %s, Option = %d, Index = %d, Text = %s },'
                            % (cs(c.get("instID")), cs(c.get("instName")), i, j, cs(line)))
    text = '''// GENERATED from ImperialCommander2/Assets/Resources/Languages/En/instructions.json
// Regenerate with: python tools/gen_instruction_fixture.py
namespace Saga.Board.Tests
{
\tpublic sealed class InstructionLine
\t{
\t\tpublic string CardId;
\t\tpublic string Card;
\t\tpublic int Option;
\t\tpublic int Index;
\t\tpublic string Text;
\t}

\tpublic static class InstructionLines
\t{
\t\tpublic static readonly InstructionLine[] All = new InstructionLine[]
\t\t{
%s
\t\t};
\t}
}
''' % "\n".join(rows)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    io.open(OUT, "w", encoding="utf-8", newline="\n").write(text)
    print("%d instruction lines from %d cards -> %s" % (len(rows), len(cards), os.path.relpath(OUT, ROOT)))
    return 0


if __name__ == "__main__":
    sys.exit(main())

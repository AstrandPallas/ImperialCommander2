"""Every card must have a translation entry in every language.

A card added to CardData without a matching row in
Languages/<lang>/DeploymentGroups/ loads, plays, and logs

    ***TRANSLATION ERROR***: SetCardTranslations()::langcard is null

on every launch. Nothing in the test suite can see that, because it is a
mismatch between two data files rather than a defect in any code -- it only
shows up when the built game starts. Which is exactly what happened when this
fork added a hero.
"""

import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARDS = os.path.join(ROOT, "ImperialCommander2", "Assets", "Resources", "CardData")
LANGS = os.path.join(ROOT, "ImperialCommander2", "Assets", "Resources", "Languages")
FILES = ("heroes.json", "enemies.json", "allies.json", "villains.json")

TRAILING_COMMA = re.compile(r",(\s*[}\]])")

# Newtonsoft accepts a lone backslash before an ordinary character; strict JSON
# does not, and some shipped translations contain one. Escaping it back is the
# same lenient read the game already performs, not a change to the file.
LONE_BACKSLASH = re.compile(r'\\(?!["\\/bfnrtu])')


def load(path):
    raw = io.open(path, encoding="utf-8-sig").read()
    raw = TRAILING_COMMA.sub(r"\1", raw)
    raw = LONE_BACKSLASH.sub(r"\\\\", raw)
    return json.loads(raw)


def ids(path):
    return {c.get("id") for c in load(path) if c.get("id")}


def main():
    problems = []
    checked = 0

    languages = sorted(
        d for d in os.listdir(LANGS)
        if os.path.isdir(os.path.join(LANGS, d, "DeploymentGroups")))

    for name in FILES:
        master = os.path.join(CARDS, name)
        if not os.path.exists(master):
            continue
        want = ids(master)

        for lang in languages:
            path = os.path.join(LANGS, lang, "DeploymentGroups", name)
            if not os.path.exists(path):
                problems.append("%s/%s is missing entirely" % (lang, name))
                continue
            have = ids(path)
            checked += 1
            missing = sorted(want - have)
            if missing:
                problems.append("%s/%s has no entry for %s"
                                % (lang, name, ", ".join(missing)))

    if problems:
        print("%d translation gap(s):" % len(problems))
        for p in problems:
            print("   ", p)
        return 1

    print("%d card files across %d languages, every card translated"
          % (checked, len(languages)))
    return 0


if __name__ == "__main__":
    sys.exit(main())

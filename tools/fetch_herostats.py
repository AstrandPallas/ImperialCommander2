"""Fetch the 21 campaign heroes' printed sheet statistics.

heroes.json carries no combat numbers at all -- only name, id, traits and
campaign bookkeeping -- so every hero reached the tracker as 10 health and 4
endurance. That is not cosmetic: the Imperial target priority chain ranks
Rebels by health remaining and by total health, so giving every hero the same
numbers quietly corrupts which hero the AI decides to attack.

The numbers are printed on the physical hero sheets and are not in this repo,
so they are read from the Imperial Assault Wiki's hero infoboxes, which carry
them as structured template fields rather than prose.

Run manually, NOT from tools/verify.py -- the suite must not need a network.
The output is checked in; this exists to regenerate and re-check it.

    python tools/fetch_herostats.py
"""

import io
import json
import os
import re
import sys
import time
import urllib.parse
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "ImperialCommander2", "Assets", "Resources",
                   "CardData", "herostats.json")

API = "https://imperial-assault.fandom.com/api.php"
SOURCE = "https://imperial-assault.fandom.com/wiki/%s_(Hero)"

# id -> the wiki page's hero name. Taken from heroes.json, which is the roster
# the app itself ships.
HEROES = [
    ("H1", "Diala Passil"), ("H2", "Fenn Signis"), ("H3", "Gaarkhan"),
    ("H4", "Gideon Argus"), ("H5", "Jyn Odan"), ("H6", "Mak Eshka'rey"),
    ("H7", "Biv Bodhrik"), ("H8", "Saska Teft"), ("H9", "Loku Kanoloa"),
    ("H10", "MHD-19"), ("H11", "Verena Talos"), ("H12", "Davith Elso"),
    ("H13", "Murne Rin"), ("H14", "Onar Koma"), ("H15", "Shyla Varad"),
    ("H16", "Vinto Hreeda"), ("H17", "Drokkatta"), ("H18", "Jarrod Kelvin"),
    ("H19", "Ko-Tun Feralo"), ("H20", "CT-1701"), ("H21", "Tress Hacnua"),
]

# Template:Infobox_hero documents its own codes: "W for white, K for black".
# An en-dash means the sheet prints no defense die at all, which is Onar Koma.
DEFENSE = {"W": ["White"], "K": ["Black"], "–": [], "—": [], "": []}

# Sanity bounds. These are not the rules, they are a tripwire: a hero sheet
# outside them means the page was parsed wrong, not that the hero is unusual.
BOUNDS = {"health": (8, 22), "endurance": (3, 7), "speed": (3, 6)}


def wikitext(name):
    page = urllib.parse.quote(name + " (Hero)")
    url = "%s?action=parse&page=%s&prop=wikitext&format=json" % (API, page)
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=45) as r:
        return json.load(r)["parse"]["wikitext"]["*"]


def field(text, name):
    m = re.search(r"\|\s*" + re.escape(name) + r"\s*=\s*([^|\n}]*)", text)
    return m.group(1).strip() if m else None


def main():
    heroes = []
    problems = []

    for hid, name in HEROES:
        try:
            text = wikitext(name)
        except Exception as exc:                      # noqa: BLE001
            problems.append("%s %s: could not be read (%s)" % (hid, name, exc))
            continue

        row = {"id": hid, "name": name}
        for stat in ("health", "endurance", "speed"):
            raw = field(text, "f " + stat)
            try:
                row[stat] = int(raw)
            except (TypeError, ValueError):
                problems.append("%s %s: %s is %r" % (hid, name, stat, raw))
                continue
            lo, hi = BOUNDS[stat]
            if not lo <= row[stat] <= hi:
                problems.append("%s %s: %s of %d is outside %d-%d, so the page "
                                "was probably parsed wrong"
                                % (hid, name, stat, row[stat], lo, hi))

        code = (field(text, "f defense") or "").strip()
        if code not in DEFENSE:
            problems.append("%s %s: defense code %r is not one the template "
                            "documents" % (hid, name, code))
        row["defense"] = DEFENSE.get(code, [])
        row["source"] = SOURCE % urllib.parse.quote(name.replace(" ", "_"))
        heroes.append(row)
        print("%-4s %-16s health %-3d endurance %-2d speed %-2d defense %s"
              % (hid, name, row.get("health", -1), row.get("endurance", -1),
                 row.get("speed", -1), row["defense"] or ["none"]))
        time.sleep(0.4)

    if problems:
        print("\n%d problem(s); nothing written:" % len(problems))
        for p in problems:
            print("   ", p)
        return 1

    missing = {h for h, _ in HEROES} - {h["id"] for h in heroes}
    if missing:
        print("\nmissing heroes, nothing written: %s" % sorted(missing))
        return 1

    doc = {
        "schemaVersion": 1,
        "note": ("Printed hero sheet statistics, which heroes.json does not "
                 "carry. Defense is the sheet's die colour, not a number. "
                 "Campaign upgrades change these, so the tracker treats them "
                 "as starting values a session may override."),
        "heroes": heroes,
    }
    io.open(OUT, "w", encoding="utf-8", newline="\n").write(
        json.dumps(doc, indent=1, ensure_ascii=False) + "\n")
    print("\n%d heroes -> %s" % (len(heroes), os.path.relpath(OUT, ROOT)))
    return 0


if __name__ == "__main__":
    sys.exit(main())

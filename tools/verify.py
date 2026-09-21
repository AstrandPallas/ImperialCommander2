"""Run every check, in the order that fails cheapest first.

    python tools/verify.py

The order is deliberate. Regenerating terrain and fixtures takes seconds and a
break there invalidates everything downstream, so it goes first. The Unity
language gate is next because it is a one-second compile that catches a whole
class of breakage the test runner cannot see. The suite itself runs last.
"""

import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
STEPS = [
    ("terrain library", [sys.executable, "tools/author_terrain.py"], ROOT),
    ("mission fixture CORE1", [sys.executable, "tools/gen_mission_fixture.py", "CORE1"], ROOT),
    ("mission fixture TWIN1", [sys.executable, "tools/gen_mission_fixture.py", "TWIN1"], ROOT),
    ("enemy card fixture", [sys.executable, "tools/gen_enemy_fixture.py"], ROOT),
    # Compiles the engine the way Unity 2020.3 will: C# 8.0, netstandard2.1.
    # The test runner uses LangVersion latest, so without this a C# 9 feature
    # or a post-2.1 BCL call passes here and fails on import into the editor.
    ("unity language gate", ["dotnet", "build", "-v", "q", "--nologo"],
     os.path.join(ROOT, "tests", "UnityLangCheck")),
    # No extra arguments here: Program.Main treats argv[0] as a test-name
    # filter, so passing one silently selects nothing and the suite "passes"
    # with zero tests run. MIN_TESTS below is the guard against that.
    ("board and tracking tests", ["dotnet", "run"],
     os.path.join(ROOT, "tests", "BoardTests")),
]

# A suite that runs NO tests exits 0 and reads as a pass: an argument meant for
# `dotnet` can reach Program.Main and be taken as a test-name filter that
# matches nothing. An exit code cannot tell the two apart, so check the count.
MIN_TESTS = 100


def main():
    failures = []
    for name, cmd, cwd in STEPS:
        p = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True)
        out = p.stdout or ""
        tail = out.strip().splitlines()[-1:] or [""]
        ok = p.returncode == 0
        note = ""
        if ok and "passed," in tail[0]:
            passed = int(tail[0].split()[0])
            if passed < MIN_TESTS:
                ok, note = False, f"  <- only {passed} tests ran, expected >= {MIN_TESTS}"
        status = "ok" if ok else "FAILED"
        print(f"[{status:6s}] {name:26s} {tail[0][:95]}{note}")
        if not ok:
            failures.append((name, out, p.stderr))
    for name, out, err in failures:
        print(f"\n===== {name} =====\n{out[-3000:]}\n{err[-2000:]}")
    print("\nall checks passed" if not failures else f"\n{len(failures)} check(s) failed")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())

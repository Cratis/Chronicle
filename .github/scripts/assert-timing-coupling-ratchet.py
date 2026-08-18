# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Fails the build when a spec or integration file gains a sleep it did not already have.

Twenty of the last seventy-three regressions were fixes to the timing-coupled observer and
projection core that broke a sibling path, because the suites wait on the clock instead of on
a completion signal - `63cf43e38` states outright that the integration suite depends on "the
implicit 50ms settling buffer". Deterministic completion signals are the real repair; until
they exist this holds the line so the class stops growing while it is being dismantled.

The baseline is per file rather than a global total. A global number lets one file's
improvement pay for another file's regression and says nothing about where the failure is; a
per-file number localizes the failure and lets the migration retire files one at a time.
Entries may only go down, and a file that is not listed may not sleep at all.

Run with `--report` to print the current counts in baseline format. That is how the baseline
was seeded and how it is regenerated after a rename - it only writes to stdout, so the change
still arrives as a reviewable diff.
"""

import os
import re
import sys

BASELINE = ".github/timing-coupling-baseline.txt"
SEARCH_ROOTS = ("Source", "Integration")
EXCLUDED_SEGMENTS = {"bin", "obj", "node_modules"}
EXCLUDED_PATHS = (".claude/worktrees/",)
SLEEPS = re.compile(r"\b(?:Thread\.Sleep|Task\.Delay|SpinWait|Task\.Yield)\b")

RULE = ".ai/rules/specs.csharp.md"


def normalize(path):
    """Returns a repository-relative path with forward slashes and no leading `./`."""
    normalized = path.replace("\\", "/")
    return normalized[2:] if normalized.startswith("./") else normalized


def is_in_scope(path):
    """Returns whether a repository-relative path belongs to the spec and integration suites."""
    if any(_ in path for _ in EXCLUDED_PATHS):
        return False

    if path.startswith("Integration/") or path.startswith("Source/Clients/XUnit.Integration/"):
        return True

    return any(_.endswith(".Specs") for _ in path.split("/"))


def sleep_lines(path):
    """Returns the line numbers where a file waits on the clock, ignoring commented-out code."""
    with open(path, encoding="utf-8", errors="ignore") as file:
        lines = file.read().splitlines()

    found = []
    for number, line in enumerate(lines, 1):
        stripped = line.strip()
        if stripped.startswith("//") or stripped.startswith("*"):
            continue

        found.extend([number] * len(SLEEPS.findall(line)))

    return found


def counts_on_disk(root):
    """Returns the sleep line numbers per in-scope file that has any."""
    found = {}
    for search_root in SEARCH_ROOTS:
        for directory, directories, files in os.walk(os.path.join(root, search_root)):
            directories[:] = [_ for _ in directories if _ not in EXCLUDED_SEGMENTS]
            relative = normalize(os.path.relpath(directory, root))
            for name in files:
                path = f"{relative}/{name}"
                if not name.endswith(".cs") or not is_in_scope(path):
                    continue

                lines = sleep_lines(os.path.join(root, relative, name))
                if lines:
                    found[path] = lines

    return found


def baseline_counts(root):
    """Returns the allowed sleep count per file as recorded in the committed baseline."""
    path = os.path.join(root, BASELINE)
    if not os.path.isfile(path):
        print(f"::error::{BASELINE} is missing. The ratchet cannot run without its baseline.")
        sys.exit(1)

    allowed = {}
    with open(path, encoding="utf-8") as file:
        for line in file:
            entry = line.split("#")[0].strip()
            if not entry:
                continue

            count, name = entry.split(None, 1)
            allowed[normalize(name.strip())] = int(count)

    return allowed


def report(on_disk):
    """Prints the current counts in baseline format, for seeding or regenerating the baseline."""
    for path in sorted(on_disk):
        print(f"{len(on_disk[path])} {path}")


def raised(on_disk, allowed):
    """Reports every file that gained a sleep, and returns whether any did."""
    failed = False
    for path in sorted(on_disk):
        current = len(on_disk[path])
        if path not in allowed:
            failed = True
            print(
                f"::error file={path}::{path} is not in {BASELINE} and waits on the clock "
                f"{current} time(s), on line(s) {', '.join(str(_) for _ in on_disk[path])}. A spec "
                f"awaits a fact, not a duration - see {RULE}. If the delay is genuinely not a wait "
                f"(a test double that is slow on purpose, an infrastructure readiness backoff, or a "
                f"`Task.Delay(1)` widening an interleaving window), add the entry to {BASELINE} and "
                f"state which one it is in the pull request."
            )
            continue

        if current > allowed[path]:
            failed = True
            print(
                f"::error file={path}::{path} waits on the clock {current} time(s) but "
                f"{BASELINE} allows {allowed[path]}. The new site(s) are among line(s) "
                f"{', '.join(str(_) for _ in on_disk[path])}. Baseline entries may only decrease - "
                f"await a fact instead, see {RULE}."
            )

    return failed


def lowered(on_disk, allowed):
    """Reports every baseline entry that is now too high, so the author can ratchet it down."""
    for path in sorted(allowed):
        current = len(on_disk.get(path, []))
        if current < allowed[path]:
            remaining = f"lower its entry to {current}" if current else f"delete its entry from {BASELINE}"
            print(f"::notice file={path}::{path} now waits on the clock {current} time(s) instead of {allowed[path]} - {remaining}.")


def main():
    """Reports every file that exceeds its baseline as an error and every improvement as a notice."""
    root = sys.argv[1] if len(sys.argv) > 1 and not sys.argv[1].startswith("-") else "."
    on_disk = counts_on_disk(root)

    if "--report" in sys.argv:
        report(on_disk)
        return 0

    allowed = baseline_counts(root)
    total = sum(len(_) for _ in on_disk.values())
    print(f"{total} sleep site(s) across {len(on_disk)} file(s); {BASELINE} allows {sum(allowed.values())} across {len(allowed)}")

    failed = raised(on_disk, allowed)
    lowered(on_disk, allowed)

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())

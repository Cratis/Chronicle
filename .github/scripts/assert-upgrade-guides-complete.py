#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Assert that every released major boundary has an entry in the upgrade guide.

Nineteen majors exist and only the newest had a guide, because nothing ever said one was
missing. A page listing major boundaries is exactly the kind of documentation that looks
complete while silently falling a version behind - the gap appears only when someone upgrading
goes looking for the row that was never written, which is the worst possible moment.

Boundaries are read from the guide's own `### N to M` headings rather than from a parallel data
file, so the page stays the single source of truth and cannot disagree with its own index.

Exit codes follow the house contract: 0 clean, 1 missing entries, 2 could not determine the
released majors and therefore checked nothing.
"""

import argparse
import json
import os
import re
import subprocess
import sys

GUIDE = "Documentation/upgrading/major-versions.md"
BOUNDARY = re.compile(r"^###\s+(\d+)\s+to\s+(\d+)\s*$", re.MULTILINE)
VERSION_TAG = re.compile(r"^v?(\d+)\.\d+\.\d+$")


def boundaries_in(text):
    """Return the (from, to) major pairs the guide documents."""
    return {(int(a), int(b)) for a, b in BOUNDARY.findall(text)}


def majors_from_tags(tags):
    """Return the set of majors that have at least one released tag."""
    majors = set()

    for tag in tags:
        match = VERSION_TAG.match(tag.strip())
        if match:
            majors.add(int(match.group(1)))

    return majors


def released_tags(runner=subprocess.run):
    """Read released tags from git, falling back to the GitHub API when tags are not fetched.

    A shallow Actions checkout has no tags, so neither source alone is reliable. Returning
    nothing is reported as could-not-run rather than as a clean result - a check that silently
    examined an empty set is the failure this script exists to prevent.
    """
    completed = runner(["git", "tag", "--list", "v*"], capture_output=True, text=True, check=False)
    tags = [line for line in completed.stdout.splitlines() if line.strip()]
    if tags:
        return tags

    completed = runner(
        ["gh", "api", "repos/{owner}/{repo}/tags?per_page=100", "--paginate", "--jq", ".[].name"],
        capture_output=True,
        text=True,
        check=False,
    )
    return [line for line in completed.stdout.splitlines() if line.strip()]


def missing_boundaries(majors, documented, oldest):
    """Return every consecutive major boundary at or above `oldest` that the guide omits."""
    relevant = sorted(major for major in majors if major >= oldest)
    expected = [(a, b) for a, b in zip(relevant, relevant[1:])]
    return [pair for pair in expected if pair not in documented]


def main(argv=None):
    """Fail when a released major boundary has no entry in the guide."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--guide", default=GUIDE, help="Path to the major version guide")
    parser.add_argument(
        "--oldest",
        type=int,
        default=6,
        help="Oldest major the guide covers. Releases below it predate the recorded history.",
    )
    parser.add_argument("--majors", help="Comma separated majors, bypassing tag discovery")
    args = parser.parse_args(argv)

    if not os.path.isfile(args.guide):
        print(f"::error::{args.guide} does not exist, so no upgrade boundary could be checked.")
        return 2

    documented = boundaries_in(open(args.guide, encoding="utf-8").read())
    if not documented:
        print(f"::error::{args.guide} documents no `### N to M` boundary at all.")
        return 1

    if args.majors:
        majors = {int(value) for value in args.majors.split(",") if value.strip()}
    else:
        majors = majors_from_tags(released_tags())

    if not majors:
        print(
            "::error::Could not determine the released majors from git tags or the GitHub API, "
            "so nothing was checked. Fetch tags (fetch-depth: 0) or pass --majors."
        )
        return 2

    missing = missing_boundaries(majors, documented, args.oldest)
    newest = max(majors)

    if missing:
        listed = ", ".join(f"{a} to {b}" for a, b in missing)
        print(
            f"::error::The upgrade guide has no entry for: {listed}. Every released major "
            f"boundary needs one, so someone upgrading across it can see what it costs. "
            f"Add a `### N to M` section to {args.guide}."
        )
        return 1

    print(
        f"Checked {len(majors)} released major(s) up to {newest}: every boundary from {args.oldest} "
        f"onward has an entry ({len(documented)} documented)."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())

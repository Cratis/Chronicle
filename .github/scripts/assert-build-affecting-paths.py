# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Fails the build when a path the repository declares build-affecting is not in the build trigger.

`.github/build-affecting-paths.txt` is a committed contract: every path listed there must reach a build
and a test run. The `paths:` filter on the build workflow is what decides that, and narrowing it for
speed silently creates holes - a `Directory.Packages.props` bump once merged with no build at all
because a change confined to that file matched none of the patterns the filter had been reduced to.
This compares the two and fails on any required path no pattern covers, so the next narrowing has to
state in the pull request what stops being covered.

Coverage is decided by glob semantics, never string equality: a required entry denotes a set of files,
and a pattern covers it only when it matches every file in that set. GitHub's filter patterns are read
as documented - `**` matches any character including `/`, `*` matches any character except `/`, `?`
matches zero or one character, and a leading `!` excludes.
"""

import os
import re
import sys

CONTRACT = ".github/build-affecting-paths.txt"
WORKFLOW = ".github/workflows/dotnet-build.yml"
PROBE = "probe-for-coverage"


def read_contract(root):
    """Returns the required path entries, in file order, with comments and blank lines dropped."""
    with open(os.path.join(root, CONTRACT), encoding="utf-8") as file:
        return [stripped for stripped in (_.split("#")[0].strip() for _ in file) if stripped]


def block(lines, start, indent):
    """Yields the lines below `start` that are indented deeper than `indent`, stopping at the first that is not."""
    for line in lines[start + 1:]:
        if not line.strip():
            continue

        if len(line) - len(line.lstrip()) <= indent:
            return

        yield line


def child(lines, start, indent, key):
    """Returns the index of `key` directly inside the block below `start`, or None."""
    for offset, line in enumerate(lines[start + 1:], start + 1):
        current = len(line) - len(line.lstrip())
        if line.strip() and current <= indent:
            return None

        if line.strip().startswith(key):
            return offset

    return None


def read_trigger_paths(root, workflow):
    """Returns the `paths:` and `paths-ignore:` patterns of the workflow's `pull_request:` trigger."""
    with open(os.path.join(root, workflow), encoding="utf-8") as file:
        lines = [_ for _ in file.read().splitlines() if not _.strip().startswith("#")]

    on = next((_ for _, line in enumerate(lines) if line.rstrip() in ("on:", '"on":', "'on':")), None)
    if on is None:
        raise SystemExit(f"::error::{workflow} has no `on:` trigger block.")

    pull_request = child(lines, on, 0, "pull_request:")
    if pull_request is None:
        raise SystemExit(f"::error::{workflow} has no `pull_request:` trigger to read a path filter from.")

    indent = len(lines[pull_request]) - len(lines[pull_request].lstrip())
    found = {}
    for key in ("paths:", "paths-ignore:"):
        at = child(lines, pull_request, indent, key)
        items = [] if at is None else [_.strip()[1:].strip().strip("\"'") for _ in block(lines, at, indent + 1)]
        found[key.rstrip(":")] = items

    return found["paths"], found["paths-ignore"]


def to_regex(pattern):
    """Compiles a GitHub path filter pattern into a regular expression that matches a whole path."""
    parts, index = [], 0
    while index < len(pattern):
        if pattern.startswith("**", index):
            parts.append(".*")
            index += 2
        elif pattern[index] == "*":
            parts.append("[^/]*")
            index += 1
        elif pattern[index] == "?":
            parts.append("[^/]?")
            index += 1
        else:
            parts.append(re.escape(pattern[index]))
            index += 1

    return re.compile("".join(parts) + r"\Z")


def witnesses(required):
    """Returns concrete paths the required entry denotes - enough of them to defeat a narrower pattern."""
    if required.endswith("/**"):
        prefix = required[:-2]
        return [f"{prefix}{PROBE}", f"{prefix}{PROBE}.probe", f"{prefix}{PROBE}/{PROBE}/{PROBE}.probe"]

    if "*" in required or "?" in required:
        raise SystemExit(f"::error::`{required}` in {CONTRACT} must be a literal path or end in `/**`.")

    return [required]


def probed(pattern):
    """Returns a concrete path a pattern denotes, by standing a probe segment in for every wildcard."""
    return pattern.replace("**", PROBE).replace("*", PROBE).replace("?", PROBE)


def uncovered(required, patterns):
    """Returns the paths the required entry denotes that no pattern in the filter reaches."""
    include = [to_regex(_) for _ in patterns if not _.startswith("!")]
    exclusions = [_[1:] for _ in patterns if _.startswith("!")]
    exclude = [to_regex(_) for _ in exclusions]

    # An exclusion carves a hole out of the middle of a subtree, which the probes above would step over.
    # Aim one probe at each exclusion so a `!` narrowing is caught the same way a removed pattern is.
    inside = to_regex(required)
    probes = set(witnesses(required)) | {_ for _ in map(probed, exclusions) if inside.match(_)}

    return sorted(_ for _ in probes if not any(r.match(_) for r in include) or any(r.match(_) for r in exclude))


def missing_from_disk(root, required):
    """Returns whether the required entry names something that is not actually in the repository."""
    target = required[:-3] if required.endswith("/**") else required
    return not os.path.exists(os.path.join(root, target))


def main():
    """Reports every required path the build workflow's pull request filter fails to cover."""
    root = sys.argv[1] if len(sys.argv) > 1 else "."
    required = read_contract(root)
    patterns, ignored = read_trigger_paths(root, WORKFLOW)
    failures = 0

    if ignored:
        print(f"::error::{WORKFLOW} uses `paths-ignore:`, which can exclude a required path. Express the filter as `paths:` so this contract can be checked.")
        failures += 1

    for entry in required:
        if missing_from_disk(root, entry):
            print(f"::error::`{entry}` is listed in {CONTRACT} but does not exist in the repository. Remove it or correct it - the contract may not list paths that are not real.")
            failures += 1
            continue

        holes = uncovered(entry, patterns)
        if holes:
            print(
                f"::error::`{entry}` is required to trigger a build and a test run, but no pattern in the "
                f"`paths:` filter of {WORKFLOW} covers it (for example `{holes[0]}` matches nothing there). "
                f"A change confined to it would merge without ever being built. Add a pattern that covers it, "
                f"or remove it from {CONTRACT} and say in the pull request what stops being covered."
            )
            failures += 1

    print(f"{len(required)} required path(s) checked against {len(patterns)} pattern(s) in {WORKFLOW}")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())

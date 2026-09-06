# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Decides whether a change reaches the hot core, and says which paths made it decide that.

`.github/hot-core-paths.txt` is a committed contract naming the areas where a fix breaks a sibling
path. `.github/workflows/hot-core-gate.yml` asks this script one question per pull request - does
this diff touch any of them - and runs the full out-of-process integration matrix when the answer is
yes. That matrix is the only oracle that has reliably caught the class, and a pull request otherwise
runs the default storage backend alone.

The answer is never just a boolean. A pull request that suddenly waits on ninety integration jobs
needs to know why, so every run prints the paths it matched under the area heading they belong to,
to the log and to the job summary. An unexplained wait is a gate people learn to route around.

`--verify` checks the contract itself instead of a diff: every pattern still matches a tracked file,
and every pattern is owned in `.github/CODEOWNERS`. Both fail closed. A pattern that matches nothing
is a rename that quietly disarmed the gate - the path list this replaced had already gone stale that
way - and a pattern missing from CODEOWNERS is the same disarming on the review side.
"""

import argparse
import os
import re
import subprocess
import sys

CONTRACT = ".github/hot-core-paths.txt"
CODEOWNERS = ".github/CODEOWNERS"
PLACEHOLDER_MARKER = "PLACEHOLDER-OWNER"
AREA_PREFIX = "# area:"


def read_contract(root):
    """Returns the contract as a list of `(area, pattern)` pairs, in file order."""
    areas = []
    current = None
    with open(os.path.join(root, CONTRACT), encoding="utf-8") as file:
        for line in file:
            stripped = line.strip()
            if stripped.startswith(AREA_PREFIX):
                current = stripped[len(AREA_PREFIX):].strip()
                continue

            if not stripped or stripped.startswith("#"):
                continue

            if current is None:
                raise SystemExit(f"::error::{CONTRACT} has a pattern before any `{AREA_PREFIX}` heading: {stripped}")

            areas.append((current, stripped))

    if not areas:
        raise SystemExit(f"::error::{CONTRACT} lists no patterns, so the hot-core gate would never fire.")

    return areas


def to_regex(pattern):
    """Translates a contract pattern to a regex; `**` crosses `/`, `*` does not."""
    compiled = ""
    index = 0
    while index < len(pattern):
        if pattern.startswith("**", index):
            compiled += ".*"
            index += 2
        elif pattern[index] == "*":
            compiled += "[^/]*"
            index += 1
        elif pattern[index] == "?":
            compiled += "[^/]"
            index += 1
        else:
            compiled += re.escape(pattern[index])
            index += 1

    return re.compile(f"^{compiled}$")


def git(root, *arguments):
    """Runs git in the repository and returns its stdout, failing loudly rather than silently empty."""
    result = subprocess.run(
        ("git", *arguments),
        cwd=root,
        capture_output=True,
        text=True,
        check=False)

    if result.returncode != 0:
        raise SystemExit(f"::error::`git {' '.join(arguments)}` failed: {result.stderr.strip()}")

    return result.stdout


def tracked_files(root):
    """Returns every file git tracks, as repository-relative forward-slash paths."""
    return [_ for _ in git(root, "ls-files").splitlines() if _]


def changed_files(root, base, head, files_from):
    """Returns the changed paths, either read from a file/stdin or taken from a diff range."""
    if files_from:
        text = sys.stdin.read() if files_from == "-" else open(files_from, encoding="utf-8").read()
        return [_.strip() for _ in text.splitlines() if _.strip()]

    if not base:
        raise SystemExit("::error::Pass --base (and optionally --head), or --files-from.")

    return [_ for _ in git(root, "diff", "--name-only", f"{base}...{head}").splitlines() if _]


def matches(contract, paths):
    """Returns `{area: [path, ...]}` for every path a contract pattern matches, areas in contract order."""
    expressions = [(area, to_regex(pattern)) for area, pattern in contract]
    found = {}
    for area, expression in expressions:
        hits = sorted(path for path in paths if expression.match(path))
        if hits:
            found.setdefault(area, [])
            found[area].extend(hit for hit in hits if hit not in found[area])

    return found


def emit(text):
    """Writes a line to the log and, when running in Actions, to the job summary."""
    print(text)
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as file:
            file.write(text + "\n")


def output(name, value):
    """Writes a step output when running in Actions."""
    path = os.environ.get("GITHUB_OUTPUT")
    if path:
        with open(path, "a", encoding="utf-8") as file:
            file.write(f"{name}={value}\n")


def counted(count, noun):
    """Returns `count` and `noun`, pluralized, so the printed explanation reads as a sentence."""
    return f"{count} {noun}" if count == 1 else f"{count} {noun}s"


def report(found, changed):
    """Prints why the gate decided what it decided, and returns whether the hot core was touched."""
    if not found:
        emit("## Hot-core gate: no hot-core path touched")
        emit("")
        emit(f"None of the {counted(len(changed), 'changed file')} is listed in `{CONTRACT}`, so the "
             "full out-of-process integration matrix is skipped and this gate passes immediately.")
        return False

    total = sum(len(_) for _ in found.values())
    emit("## Hot-core gate: the full out-of-process integration matrix runs")
    emit("")
    emit(f"This pull request changes {counted(total, 'hot-core file')} of "
         f"{counted(len(changed), 'changed file')}, so it runs every storage backend out of process "
         "rather than the default one only. That matrix is the only check that has reliably caught a "
         "fix breaking a sibling path.")
    emit("")
    for area, paths in found.items():
        emit(f"**{area}**")
        emit("")
        for path in paths:
            emit(f"- `{path}`")
        emit("")

    return True


def verify(root, contract):
    """Fails when a pattern matches nothing, or is not owned in CODEOWNERS."""
    problems = []

    tracked = tracked_files(root)
    for area, pattern in contract:
        expression = to_regex(pattern)
        if not any(expression.match(_) for _ in tracked):
            problems.append(
                f"`{pattern}` (area: {area}) matches no tracked file. Either the code moved and the "
                f"gate is now disarmed for that area, or the pattern was never right - fix "
                f"{CONTRACT} rather than deleting the entry.")

    owned = owned_paths(root)
    for area, pattern in contract:
        expected = as_codeowners_path(pattern)
        if expected not in owned:
            problems.append(
                f"`{pattern}` (area: {area}) has no owner. Add `{expected} <owner>` to {CODEOWNERS} "
                f"so a change there cannot merge without a maintainer looking at it.")

    if problems:
        for problem in problems:
            print(f"::error::{problem}", file=sys.stderr)
        raise SystemExit(1)

    print(f"{len(contract)} hot-core patterns all match tracked files and all have an owner in {CODEOWNERS}.")

    if PLACEHOLDER_MARKER in read_text(root, CODEOWNERS):
        print(f"::warning file={CODEOWNERS}::{CODEOWNERS} still carries the {PLACEHOLDER_MARKER} "
              "marker, so its owner does not resolve. GitHub ignores a rule whose owner is not a real "
              "user or team with read access - the file parses and enforces nothing. Set the real "
              "owner and delete the marker.")


def read_text(root, path):
    """Returns the contents of a repository file."""
    with open(os.path.join(root, path), encoding="utf-8") as file:
        return file.read()


def owned_paths(root):
    """Returns the set of paths CODEOWNERS assigns an owner to."""
    found = set()
    for line in read_text(root, CODEOWNERS).splitlines():
        stripped = line.split("#")[0].strip()
        if not stripped:
            continue

        parts = stripped.split()
        if len(parts) >= 2 and any(_.startswith("@") for _ in parts[1:]):
            found.add(parts[0])

    return found


def as_codeowners_path(pattern):
    """Returns the CODEOWNERS spelling of a contract pattern - a directory, or an anchored glob."""
    return "/" + (pattern[:-2] if pattern.endswith("/**") else pattern)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", help="Base revision of the diff to inspect.")
    parser.add_argument("--head", default="HEAD", help="Head revision of the diff to inspect.")
    parser.add_argument("--files-from", help="Read changed paths from this file, or `-` for stdin.")
    parser.add_argument("--verify", action="store_true", help="Check the contract itself instead of a diff.")
    arguments = parser.parse_args()

    root = git(os.getcwd(), "rev-parse", "--show-toplevel").strip()
    contract = read_contract(root)

    if arguments.verify:
        verify(root, contract)
        return

    changed = changed_files(root, arguments.base, arguments.head, arguments.files_from)
    touched = report(matches(contract, changed), changed)
    output("touched", "true" if touched else "false")


if __name__ == "__main__":
    main()

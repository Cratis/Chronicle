#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Derive the version CI stamps into every assembly it builds (#5353).

Prints ``CRATIS_CHRONICLE_BUILD_VERSION=<major>.<minor + 1>.0-ci.<run number>`` on stdout so the
output can be appended to ``$GITHUB_ENV``. The base is the highest stable ``vX.Y.Z`` release tag.
"""

import os
import re
import subprocess
import sys

STABLE_TAG = re.compile(r"^v(\d+)\.(\d+)\.(\d+)$")
TAG_PREFIX = "refs/tags/"


class DeriveError(Exception):
    """Raised when a version cannot be derived."""


def next_version(tag_refs, run_number):
    """Return (version, latest release tag) from ls-remote lines and the run number."""
    if run_number is None or not re.fullmatch(r"\d+", run_number):
        raise DeriveError(f"GITHUB_RUN_NUMBER must be a number, got {run_number!r}")

    best = None
    for line in tag_refs:
        name = line.split("\t")[-1].strip()
        if not name.startswith(TAG_PREFIX):
            continue
        tag = name[len(TAG_PREFIX):]
        match = STABLE_TAG.match(tag)
        if match is None:
            continue
        key = tuple(int(part) for part in match.groups())
        if best is None or key > best[0]:
            best = (key, tag)

    if best is None:
        raise DeriveError("No stable vX.Y.Z release tag found on origin")

    (major, minor, _), tag = best
    return f"{major}.{minor + 1}.0-ci.{int(run_number)}", tag


def main():
    try:
        result = subprocess.run(
            ["git", "ls-remote", "--tags", "--refs", "origin", "refs/tags/v*"],
            check=True, capture_output=True, text=True, timeout=60)
        version, tag = next_version(result.stdout.splitlines(), os.environ.get("GITHUB_RUN_NUMBER"))
    except (DeriveError, subprocess.SubprocessError, OSError) as error:
        print(f"::error::Could not derive the build version: {error}", file=sys.stderr)
        return 1

    print(f"Stamping build version {version} (latest release {tag})", file=sys.stderr)
    print(f"CRATIS_CHRONICLE_BUILD_VERSION={version}")
    return 0


if __name__ == "__main__":
    sys.exit(main())

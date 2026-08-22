# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Prints the public type surface of the shipped client packages, one fully-qualified name per line.

`.github/public-api-surface.txt` is what lets the coverage ratchet tell a consumer-facing type from
an `internal` one. Cobertura instruments both and reports them under the same namespace, so without
this list the ratchet would demand consumer-facing specs for types like `InMemoryEventCursor` and
`NoOpEventTypesCacheClient` - both `internal sealed`, neither referenceable by anyone who installs
the package.

The surface is read from source rather than from a built assembly on purpose: reflecting over the
assembly needs `MetadataLoadContext`, which is not in `Directory.Packages.props`, and adding a
package reference to satisfy a CI script is a worse trade than a declaration scan. The two were
compared when this was written - the scan finds all 35 public types the built
`Cratis.Chronicle.Testing` assembly exports, differing only by `Cratis.Arc.Generated.GeneratedMarker`,
which a source generator emits and no consumer writes code against.

Run it and commit the result:

    python3 .github/scripts/report-public-api-surface.py > .github/public-api-surface.txt
"""

import os
import re
import subprocess
import sys

# The projects whose public surface consumers can call. Kept beside SHIPPED_PREFIXES in the ratchet;
# both have to name the same packages or the ratchet measures one thing and reports another.
SHIPPED_PROJECTS = ("Source/Clients/Testing",)

DECLARATION = re.compile(
    r"^\s*public\s+(?:sealed\s+|static\s+|abstract\s+|partial\s+|readonly\s+|ref\s+|unsafe\s+)*"
    r"(?:class|record|struct|interface|enum|delegate)\s+([A-Za-z_][A-Za-z0-9_]*)",
    re.M)

NAMESPACE = re.compile(r"^\s*namespace\s+([A-Za-z0-9_.]+)", re.M)

EXCLUDED_SEGMENTS = ("/bin/", "/obj/")


def tracked_sources(project):
    """Returns the git-tracked C# files of a project, so build output can never enter the surface."""
    listed = subprocess.run(
        ["git", "ls-files", f"{project}/*.cs"],
        capture_output=True, text=True, check=True).stdout.split()

    return [_ for _ in listed if not any(segment in _ for segment in EXCLUDED_SEGMENTS)]


class SourceUnreadable(Exception):
    """Raised when a tracked source file cannot be read, which is never the same as it declaring nothing."""


def public_types(path):
    """Returns the fully-qualified names of the public types declared in one file.

    A file that cannot be read is an error rather than an empty result: silently skipping it would
    drop its types from the surface, and the ratchet reads a missing type as `internal` and stops
    demanding coverage for it. That is the failure this whole check exists to prevent, so it fails
    loudly instead.
    """
    try:
        with open(path, encoding="utf-8", errors="ignore") as file:
            text = file.read()
    except OSError as error:
        raise SourceUnreadable(f"`{path}` is tracked but could not be read ({error}).") from error

    namespace = NAMESPACE.search(text)
    prefix = f"{namespace.group(1)}." if namespace else ""

    return [f"{prefix}{name}" for name in DECLARATION.findall(text)]


def main():
    """Prints every public type in the shipped projects, sorted, one per line."""
    found = set()
    try:
        for project in SHIPPED_PROJECTS:
            if not os.path.isdir(project):
                print(f"`{project}` does not exist - the shipped project list is stale.", file=sys.stderr)
                return 1

            for path in tracked_sources(project):
                found.update(public_types(path))
    except (SourceUnreadable, subprocess.CalledProcessError) as error:
        print(f"The surface could not be produced ({error}).", file=sys.stderr)
        return 1

    for name in sorted(found):
        print(name)

    return 0


if __name__ == "__main__":
    sys.exit(main())

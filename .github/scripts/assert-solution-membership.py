# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Fails the build when a spec project on disk is not a member of the solution.

The zero-test guard catches a test step that ran nothing, but it can only see steps that ran.
A project absent from `Chronicle.slnx` is never compiled and never invoked, so it emits no TRX
at all - there is nothing for that guard to read, and the suite stays green while the specs
have literally never executed. This closes that blind spot at the only point where it is
visible: the project files on disk versus the projects the solution lists.

Non-spec projects are reported as warnings when nothing reaches them, since a library outside
the solution can still be legitimately consumed through a `ProjectReference` chain.
"""

import os
import sys
import xml.etree.ElementTree as ET

SOLUTION = "Chronicle.slnx"
ALLOWLIST = ".github/scripts/solution-membership-allowlist.txt"
SEARCH_ROOTS = ("Source", "Integration")
EXCLUDED_SEGMENTS = {"Benchmarks", "Samples", "TestApps", "bin", "obj", "node_modules"}
EXCLUDED_PATHS = (".claude/worktrees/",)

# Pre-existing `when_*` spec folders that do not sit under a `for_*` folder. These were already
# in the tree when this gate was added; the check fails on any NEW one. Fix the folder (move it
# under a `for_*` parent and update its namespace) and delete the entry - never extend this list.
KNOWN_SPEC_STRUCTURE_DEVIATIONS = {
    "Source/Kernel/Core.Specs/Services/Observation/when_resolving_grpc_observation_services",
}


def normalize(path):
    """Returns a repository-relative path with forward slashes and no leading `./`."""
    normalized = path.replace("\\", "/")
    return normalized[2:] if normalized.startswith("./") else normalized


def is_excluded(path):
    """Returns whether a repository-relative path is outside the scope of this check."""
    segments = set(path.split("/"))
    return bool(segments & EXCLUDED_SEGMENTS) or any(_ in path for _ in EXCLUDED_PATHS)


def solution_projects(root):
    """Returns the normalized project paths the solution file lists."""
    tree = ET.parse(os.path.join(root, SOLUTION))
    return {normalize(_.get("Path")) for _ in tree.iter("Project") if _.get("Path")}


def projects_on_disk(root):
    """Returns every non-excluded csproj found under the search roots."""
    found = set()
    for search_root in SEARCH_ROOTS:
        for directory, directories, files in os.walk(os.path.join(root, search_root)):
            directories[:] = [_ for _ in directories if _ not in EXCLUDED_SEGMENTS]
            relative = normalize(os.path.relpath(directory, root))
            found.update(f"{relative}/{_}" for _ in files if _.endswith(".csproj"))

    return {_ for _ in found if not is_excluded(_)}


def references_of(root, project):
    """Returns the normalized paths of the projects a project file references."""
    full = os.path.join(root, project)
    if not os.path.isfile(full):
        return set()

    try:
        tree = ET.parse(full)
    except ET.ParseError as error:
        print(f"::warning::Could not parse {project}: {error}")
        return set()

    directory = os.path.dirname(project)
    includes = (_.get("Include") for _ in tree.iter("ProjectReference"))
    return {normalize(os.path.normpath(os.path.join(directory, _.replace("\\", "/")))) for _ in includes if _}


def reachable_from(root, seeds):
    """Returns every project reachable from the seeds by walking ProjectReference includes."""
    visited = set()
    pending = list(seeds)
    while pending:
        project = pending.pop()
        if project in visited:
            continue

        visited.add(project)
        pending.extend(references_of(root, project) - visited)

    return visited


def allowlisted(root):
    """Returns the spec projects deliberately kept out of the solution."""
    path = os.path.join(root, ALLOWLIST)
    if not os.path.isfile(path):
        return set()

    with open(path, encoding="utf-8") as file:
        lines = (_.split("#")[0].strip() for _ in file)
        return {normalize(_) for _ in lines if _}


def structure_violations(root, spec_projects):
    """Returns every `when_*` folder in the spec projects that does not sit under a `for_*` folder."""
    violations = set()
    for project in spec_projects:
        for directory, directories, _ in os.walk(os.path.join(root, os.path.dirname(project))):
            directories[:] = [d for d in directories if d not in EXCLUDED_SEGMENTS]
            relative = normalize(os.path.relpath(directory, root))
            if any(segment.startswith("for_") for segment in relative.split("/")):
                continue

            violations.update(f"{relative}/{d}" for d in directories if d.startswith("when_"))

    return violations - KNOWN_SPEC_STRUCTURE_DEVIATIONS


def main():
    """Reports spec projects missing from the solution as errors and other orphans as warnings."""
    root = sys.argv[1] if len(sys.argv) > 1 else "."
    in_solution = solution_projects(root)
    on_disk = projects_on_disk(root)
    reachable = reachable_from(root, in_solution)

    specs = {_ for _ in on_disk if _.endswith(".Specs.csproj")}
    missing_specs = sorted(specs - in_solution - allowlisted(root))
    unreachable = sorted(on_disk - reachable - specs)
    deviations = sorted(structure_violations(root, specs))

    print(f"{len(on_disk)} project(s) on disk, {len(in_solution)} in {SOLUTION}, {len(reachable)} reachable")

    for project in unreachable:
        print(
            f"::warning::{project} is not in {SOLUTION} and no project in the solution references it. "
            f"Nothing builds it, so it can break without anyone noticing."
        )

    for folder in deviations:
        print(f"::error::{folder} is a `when_*` spec folder that does not sit under a `for_*` folder.")

    for project in missing_specs:
        print(
            f"::error::{project} is a spec project that is not in {SOLUTION}. A spec project outside "
            f"the solution is never built and never invoked, so it runs nowhere - and the zero-test "
            f"guard cannot detect it, because a project that never runs produces no TRX file to read. "
            f"Add it to {SOLUTION}, or record it in {ALLOWLIST} with the issue that tracks fixing it."
        )

    return 1 if missing_specs or deviations else 0


if __name__ == "__main__":
    sys.exit(main())

# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Fails a pull request that deletes a `for_*` spec folder while the type it specifies stays in the tree.

A stale worktree once pushed a change that silently reverted a sibling branch's merged hardening.
CI stayed green, because the specs that proved the hardening were deleted along with the code it
guarded - nothing was left to fail. `git log -- <file>` hid it too, since the release merge was
TREESAME on those paths. Parallel agent branches editing the same files make this a recurring
shape rather than a one-off, and no existing gate can see it: a deleted spec does not fail, it
simply stops being counted.

The signal this looks for is the asymmetry that made the incident detectable in hindsight - a
`for_<TypeName>` folder disappearing entirely while `<TypeName>` itself is still in the tree.
Deleting a type together with its specs is ordinary housekeeping and is silent here; deleting only
the specs means the type kept its behavior and lost its proof.

The rule is deliberately narrow, because a noisy tripwire gets switched off and then guards
nothing. Three conditions must all hold before it fires:

  1. Every file under some `for_<TypeName>` folder is gone at the head of the branch. Removing
     individual `when_*` cases from a folder that survives is normal spec refactoring.
  2. No `for_<TypeName>` folder for that same type name survives anywhere else in the repository,
     so a folder that moved, was renamed or was split is not mistaken for one that vanished.
  3. A file named `<TypeName>.cs`, `.ts` or `.tsx` still exists at a path that mirrors the spec
     folder - either as a sibling of it (the colocated Workbench layout) or in the matching
     non-spec project (`<Project>.Specs/<dirs>/for_<T>` mirrors `<Project>/<dirs>/<T>.<ext>`).

Subject resolution is intentionally path-anchored rather than a repository-wide search by file
name. Chronicle has many same-named types across projects - storage providers implementing one
interface for MongoDB, SQL and in-memory, contracts mirrored between kernel and clients - and
matching on the bare name would fire whenever specs for one of them were retired while the others
remained. That trade buys precision at the cost of missing cross-project spec projects that
specify a type living somewhere else; those are false negatives, which cost nothing here, whereas
a false positive costs the whole gate.

Deliberate spec removals proceed by labelling the pull request `specs-removal-intended`.
"""

import json
import os
import re
import subprocess
import sys

ESCAPE_HATCH_LABEL = "specs-removal-intended"
LABELS_VARIABLE = "PR_LABELS"
SPEC_EXTENSIONS = (".cs", ".ts", ".tsx")
SPEC_PROJECT_SUFFIX = ".Specs"
FOR_FOLDER = re.compile(r"^for_[A-Za-z_][A-Za-z0-9_]*$")


def git(root, *arguments):
    """Returns the non-empty output lines of a git command run against the repository at `root`."""
    completed = subprocess.run(
        ("git", "-C", root) + arguments,
        capture_output=True,
        text=True,
        check=True,
    )
    return [line for line in completed.stdout.splitlines() if line]


def deleted_paths(root, base, head):
    """Returns the paths the change deletes, with renames resolved so a moved file is not a deletion."""
    deleted = []
    for line in git(root, "diff", "--name-status", "--find-renames", f"{base}...{head}"):
        fields = line.split("\t")
        if fields[0] == "D":
            deleted.append(fields[1])

    return deleted


def for_folder_of(path):
    """Returns the `for_<TypeName>` folder a spec file sits under, or None when it is not a spec file."""
    if not path.endswith(SPEC_EXTENSIONS):
        return None

    segments = path.split("/")
    for index, segment in enumerate(segments[:-1]):
        if FOR_FOLDER.match(segment):
            return "/".join(segments[: index + 1])

    return None


def type_name_of(for_folder):
    """Returns the type name a `for_<TypeName>` folder specifies."""
    return for_folder.rsplit("/", 1)[-1][len("for_") :]


def subject_candidates(for_folder):
    """Returns the paths a `for_<TypeName>` folder's subject may occupy, mirroring the spec layout.

    A spec folder inside a `<Project>.Specs` project mirrors that project, so
    `<Project>.Specs/<dirs>/for_<T>` maps to `<Project>/<dirs>/<T>.<ext>`. A spec folder outside one
    is colocated and sits beside the file it specifies, which is how the Workbench arranges its
    TypeScript specs. The two are mutually exclusive, so a support class parked beside a spec folder
    inside a spec project is never mistaken for the subject.
    """
    parent, _ = for_folder.rsplit("/", 1)
    name = type_name_of(for_folder)

    segments = parent.split("/")
    for index, segment in enumerate(segments):
        if segment.endswith(SPEC_PROJECT_SUFFIX):
            mirrored = segments[:index] + [segment[: -len(SPEC_PROJECT_SUFFIX)]] + segments[index + 1 :]
            parent = "/".join(mirrored)
            break

    return [f"{parent}/{name}{extension}" for extension in SPEC_EXTENSIONS]


def spec_folders(paths):
    """Returns the set of `for_<TypeName>` folders the given paths contribute spec files to."""
    return {folder for folder in (for_folder_of(_) for _ in paths) if folder}


def orphaned_subjects(root, base, head):
    """Returns the (spec folder, surviving subject) pairs where specs were deleted but the type was kept."""
    at_head = set(git(root, "ls-tree", "-r", "--name-only", head))
    surviving = spec_folders(at_head)
    surviving_names = {type_name_of(_) for _ in surviving}

    orphaned = []
    for folder in sorted(spec_folders(deleted_paths(root, base, head))):
        if folder in surviving or type_name_of(folder) in surviving_names:
            continue

        subject = next((_ for _ in subject_candidates(folder) if _ in at_head), None)
        if subject:
            orphaned.append((folder, subject))

    return orphaned


def labels():
    """Returns the pull request's labels, read from the event payload the workflow passes through."""
    try:
        return set(json.loads(os.environ.get(LABELS_VARIABLE) or "[]"))
    except json.JSONDecodeError:
        return set()


def main():
    """Reports every deleted spec folder whose subject survived the change."""
    if len(sys.argv) < 3:
        print(f"::error::usage: {sys.argv[0]} <base-ref> <head-ref> [repository-root]")
        return 2

    base, head = sys.argv[1], sys.argv[2]
    root = sys.argv[3] if len(sys.argv) > 3 else "."
    orphaned = orphaned_subjects(root, base, head)

    if not orphaned:
        print("No spec folder was deleted while the type it specifies stayed in the tree.")
        return 0

    for folder, subject in orphaned:
        print(f"::error file={subject}::{folder} was deleted in full, but {subject} is still in the tree.")

    print()
    print(f"{len(orphaned)} spec folder(s) were deleted while the code they specify was kept:")
    print()
    for folder, subject in orphaned:
        print(f"  deleted: {folder}/")
        print(f"  kept:    {subject}")
        print()

    print(
        "A spec that is deleted does not fail - it stops being counted, so the suite stays green while\n"
        "the behavior it proved is no longer verified. This is the shape a stale worktree used to revert\n"
        "merged work without any check noticing.\n"
        "\n"
        "Resolve it one of these ways:\n"
        "\n"
        "  - The deletion was accidental, most likely a branch built on a stale base. Merge the current\n"
        "    base branch and restore the specs, then confirm the code they cover is also the current\n"
        "    version - if the specs were reverted, the implementation probably was too.\n"
        "  - The specs moved or were rewritten under a different name. Keep them in a `for_` folder for\n"
        "    the same type so the coverage is still visible, and this check passes on its own.\n"
        f"  - The removal is deliberate, because the behavior is genuinely gone or the specs were\n"
        f"    redundant. Label the pull request `{ESCAPE_HATCH_LABEL}` and re-run this check."
    )

    if ESCAPE_HATCH_LABEL in labels():
        print()
        print(f"Allowed: the pull request carries the `{ESCAPE_HATCH_LABEL}` label.")
        return 0

    return 1


if __name__ == "__main__":
    sys.exit(main())

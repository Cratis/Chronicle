# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Keep local kernel builds from shadowing registry images (#3795).

Docker prefers a local image to a registry image with the same tag. Local builds must use
local-only Chronicle tags, and integration defaults must name an image a local build produces.
"""

import html
import os
from pathlib import Path
import re
import sys

PRUNED = {".git", "node_modules", "bin", "obj", ".ai-work"}
MSBUILD = {".csproj", ".proj", ".props", ".targets"}
BUILD = re.compile(r"docker\s+(?:buildx\s+)?build\b[^\n]*?\s(?:-t|--tag)\s+(\S+)")
DEFAULT = re.compile(r'GetEnvironmentVariable\(\s*"CRATIS_CHRONICLE_LOCAL_IMAGE"\s*\)\s*\?\?\s*"([^"]+)"')


def normalise(reference):
    """Treat Docker Hub's explicit prefixes and implicit latest tag as equivalent."""
    for prefix in ("docker.io/", "index.docker.io/"):
        if reference.startswith(prefix):
            reference = reference[len(prefix):]
            break
    if ":" not in reference.rsplit("/", 1)[-1]:
        reference += ":latest"
    return reference


def published(reference):
    """Only local or local-* tags are safe for the published Chronicle repository."""
    repository, tag = normalise(reference).rsplit(":", 1)
    return repository == "cratis/chronicle" and not (tag == "local" or tag.startswith("local-"))


def build_references(path, content):
    """Read local shell builds and resolve MSBuild properties from same-file literal defaults."""
    if path.suffix == ".sh":
        commands = [line for line in content.splitlines() if not line.lstrip().startswith("#")]
    else:
        commands = [html.unescape(command) for command in re.findall(r'Command="([^"]*)"', content)]
    for command in commands:
        for match in BUILD.finditer(command):
            reference = match[1].strip("\"'")
            if path.suffix in MSBUILD:
                def resolve(property_match):
                    name = re.escape(property_match[1])
                    values = re.findall(rf"<{name}\b[^>]*>([^<]*)</{name}>", content)
                    return next((value.strip() for value in values if value.strip() and "$(" not in value), property_match[0])

                reference = re.sub(r"\$\(([^)]+)\)", resolve, reference)
            yield reference


def scan(root):
    """Count inspected source files and collect producers and runtime defaults, excluding outputs."""
    producers, defaults = [], []
    files = 0

    def walk_error(error):
        raise error

    for directory, directories, names in os.walk(root, onerror=walk_error):
        directories[:] = sorted(name for name in directories if name not in PRUNED)
        for name in sorted(names):
            path = Path(directory) / name
            if path.suffix not in MSBUILD | {".sh", ".cs"}:
                continue
            files += 1
            content = path.read_text(encoding="utf-8")
            relative = path.relative_to(root)
            if path.suffix == ".cs":
                defaults.extend((relative, match[1]) for match in DEFAULT.finditer(content))
            else:
                producers.extend((relative, reference) for reference in build_references(path, content))
    return files, producers, defaults


def main(argv):
    """Return 0 for a verified tree, 1 for violations, or 2 when unable to vouch."""
    root = Path(argv[1]) if len(argv) > 1 else Path(__file__).resolve().parents[2]
    try:
        files, producers, defaults = scan(root)
    except (OSError, UnicodeError) as error:
        print(f"cannot vouch: {error}", file=sys.stderr)
        return 2

    violations = []
    resolved = {normalise(reference) for _, reference in producers if "$(" not in reference}
    for path, reference in producers + defaults:
        if "$(" in reference:
            violations.append((path, reference, "unresolvable image property"))
        elif published(reference):
            violations.append((path, reference, "local Chronicle images must use local or local-* tags"))
    for path, reference in defaults:
        if normalise(reference) not in resolved:
            violations.append((path, reference, "no local build produces it"))
    for path, reference, reason in violations:
        print(f"{path}: {reference}: {reason}", file=sys.stderr)
    if not producers or not defaults:
        print(f"cannot vouch: found {len(producers)} docker build producers and {len(defaults)} runtime defaults", file=sys.stderr)
        return 2
    if violations:
        return 1

    msbuild = sum(path.suffix in MSBUILD for path, _ in producers)
    print(
        f"Checked {files} files: {len(producers)} docker build producers "
        f"({msbuild} MSBuild, {len(producers) - msbuild} shell), "
        f"{len(defaults)} CRATIS_CHRONICLE_LOCAL_IMAGE defaults; 0 claim a published tag."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))

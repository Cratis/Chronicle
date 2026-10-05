#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Guard rails for attaching the canonical Python contracts to an existing Chronicle GitHub release.

Python contracts are not published to PyPI (trusted publishing is deliberately not configured), so the temporary
distribution convention is a wheel and sdist attached to the release of the exact tag they were generated from.
Every subcommand here is a check or a plan; none of them talks to GitHub or changes anything. Exit codes:
0 passed, 1 a check failed, 2 the tool could not run (bad usage, unreadable input).
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
import tarfile
import zipfile
from pathlib import Path

TAG_PATTERN = re.compile(r"^v(?P<version>(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*))$")
DISTRIBUTION_NAME = "cratis_chronicle_contracts"
CHECKSUMS_FILE = "SHA256SUMS"
PROVENANCE_FILE = "provenance.json"
REQUIRED_PACKAGE_PATHS = (
    "eventtypes_pb2.py",
    "eventtypes_pb2_grpc.py",
    "eventsources_pb2.py",
    "eventsources_pb2_grpc.py",
    "sequences_pb2.py",
    "sequences_pb2_grpc.py",
    "protobuf_net/bcl_pb2.py",
    "py.typed",
)


class CheckFailed(Exception):
    """A guard rejected its input."""


def expected_files(version: str) -> tuple[str, str]:
    return f"{DISTRIBUTION_NAME}-{version}-py3-none-any.whl", f"{DISTRIBUTION_NAME}-{version}.tar.gz"


def asset_names(version: str) -> tuple[str, ...]:
    return (*expected_files(version), CHECKSUMS_FILE, PROVENANCE_FILE)


def parse_tag(tag: str) -> str:
    """Return the version for an exact stable release tag, rejecting everything else."""
    match = TAG_PATTERN.fullmatch(tag)
    if match is None:
        raise CheckFailed(f"{tag!r} is not an exact stable release tag of the form vMAJOR.MINOR.PATCH")
    return match.group("version")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1 << 20), b""):
            digest.update(chunk)
    return digest.hexdigest()


def git(directory: Path, *arguments: str) -> str:
    result = subprocess.run(["git", "-C", str(directory), *arguments], capture_output=True, text=True, check=False)
    if result.returncode != 0:
        raise CheckFailed(f"git {' '.join(arguments)} failed: {result.stderr.strip()}")
    return result.stdout.strip()


def check_release(release: dict, tag: str, tag_commit: str, checkout_commit: str) -> None:
    """The release must be the published release of this tag, and the checked out source must be the tag."""
    if release.get("tagName") != tag:
        raise CheckFailed(f"Release tag '{release.get('tagName')}' is not '{tag}'")
    if release.get("isDraft"):
        raise CheckFailed(f"Release {tag} is a draft; only a published release can receive assets")
    if release.get("isPrerelease"):
        raise CheckFailed(f"Release {tag} is a prerelease; only stable releases are supported")
    if not release.get("publishedAt"):
        raise CheckFailed(f"Release {tag} has no publication time")
    if tag_commit != checkout_commit:
        raise CheckFailed(f"Checked out {checkout_commit} but tag {tag} points at {tag_commit}")


def read_wheel_metadata(wheel: Path) -> dict[str, str]:
    with zipfile.ZipFile(wheel) as archive:
        names = [name for name in archive.namelist() if name.endswith(".dist-info/METADATA")]
        if len(names) != 1:
            raise CheckFailed(f"Expected exactly one METADATA in {wheel.name}, found {len(names)}")
        text = archive.read(names[0]).decode()
    metadata: dict[str, str] = {}
    for line in text.split("\n\n", 1)[0].splitlines():
        key, _, value = line.partition(": ")
        metadata.setdefault(key, value)
    return metadata


def verify_distributions(dist: Path, version: str) -> None:
    wheel_name, sdist_name = expected_files(version)
    present = sorted(path.name for path in dist.iterdir()) if dist.is_dir() else []
    if present != sorted((wheel_name, sdist_name)):
        raise CheckFailed(f"Distribution directory holds {present}, expected exactly {[wheel_name, sdist_name]}")

    metadata = read_wheel_metadata(dist / wheel_name)
    if metadata.get("Name", "").replace("-", "_") != DISTRIBUTION_NAME:
        raise CheckFailed(f"Wheel name is '{metadata.get('Name')}'")
    if metadata.get("Version") != version:
        raise CheckFailed(f"Wheel version is '{metadata.get('Version')}', expected '{version}'")

    with zipfile.ZipFile(dist / wheel_name) as archive:
        wheel_paths = set(archive.namelist())
    missing = [p for p in REQUIRED_PACKAGE_PATHS if f"{DISTRIBUTION_NAME}/{p}" not in wheel_paths]
    if missing:
        raise CheckFailed(f"Wheel is missing {missing}")

    with tarfile.open(dist / sdist_name, "r:gz") as archive:
        sdist_paths = archive.getnames()
    prefix = f"{DISTRIBUTION_NAME}-{version}"
    missing = [p for p in REQUIRED_PACKAGE_PATHS if f"{prefix}/src/{DISTRIBUTION_NAME}/{p}" not in sdist_paths]
    if missing:
        raise CheckFailed(f"Source distribution is missing {missing}")


def build_manifest(dist: Path, version: str, tag: str, commit: str, proto_root: Path) -> tuple[str, dict]:
    """Deterministic checksum file and provenance: the same build always yields the same bytes."""
    verify_distributions(dist, version)
    files = {name: sha256(dist / name) for name in expected_files(version)}
    checksums = "".join(f"{digest}  {name}\n" for name, digest in sorted(files.items()))
    provenance = {
        "package": DISTRIBUTION_NAME,
        "version": version,
        "sourceRepository": "https://github.com/Cratis/Chronicle",
        "sourceTag": tag,
        "sourceCommit": commit,
        "protoDescriptorSha256": sha256(proto_root / "chronicle.desc"),
        "generator": "Source/Clients/Python/generate.py from the same tag, on protos regenerated from Source/Kernel/Contracts",
        "files": files,
        "note": "Temporary GitHub release asset distribution. This package is not published to PyPI.",
    }
    return checksums, provenance


def plan_upload(release: dict, dist: Path, version: str) -> dict[str, str]:
    """Decide per asset: 'upload' when absent, 'skip' when already attached with identical content.

    Raises when an asset of that name exists with different content (no overwrite, ever) or when the release
    carries a Python contract asset this version would not produce.
    """
    wanted = {name: f"sha256:{sha256(dist / name)}" for name in asset_names(version)}
    existing = {asset["name"]: asset.get("digest") for asset in release.get("assets", [])}
    unexpected = sorted(name for name in existing if name.startswith(DISTRIBUTION_NAME) and name not in wanted)
    if unexpected:
        raise CheckFailed(f"Release already carries unexpected Python contract assets: {unexpected}")
    plan: dict[str, str] = {}
    for name, digest in wanted.items():
        if name not in existing:
            plan[name] = "upload"
        elif existing[name] == digest:
            plan[name] = "skip"
        else:
            raise CheckFailed(
                f"Asset {name} exists with {existing[name]}; this build is {digest}. Refusing to overwrite."
            )
    return plan


def verify_checksums(directory: Path, version: str) -> None:
    """The SHA256SUMS file must describe exactly the wheel and sdist next to it."""
    lines = (directory / CHECKSUMS_FILE).read_text().splitlines()
    recorded = dict(reversed(line.split("  ", 1)) for line in lines)
    for name in expected_files(version):
        if recorded.get(name) != sha256(directory / name):
            raise CheckFailed(f"{name} does not match {CHECKSUMS_FILE}")
    if set(recorded) != set(expected_files(version)):
        raise CheckFailed(f"{CHECKSUMS_FILE} lists unexpected files: {sorted(recorded)}")


def run(arguments: argparse.Namespace) -> None:
    command = arguments.command
    if command == "resolve-tag":
        print(parse_tag(arguments.tag))
    elif command == "check-release":
        parse_tag(arguments.tag)
        tag_commit = git(arguments.repo, "rev-parse", f"refs/tags/{arguments.tag}^{{commit}}")
        checkout_commit = git(arguments.repo, "rev-parse", "HEAD")
        check_release(json.loads(arguments.release.read_text()), arguments.tag, tag_commit, checkout_commit)
        print(f"Release {arguments.tag} is published and the checkout is its tag commit {tag_commit}")
    elif command == "verify-dist":
        verify_distributions(arguments.dist, arguments.version)
        print(f"Distributions for {arguments.version} are complete")
    elif command == "manifest":
        version = parse_tag(arguments.tag)
        checksums, provenance = build_manifest(
            arguments.dist, version, arguments.tag, arguments.commit, arguments.proto_root
        )
        (arguments.dist / CHECKSUMS_FILE).write_text(checksums)
        (arguments.dist / PROVENANCE_FILE).write_text(json.dumps(provenance, indent=2, sort_keys=True) + "\n")
        print(checksums, end="")
    elif command == "verify-checksums":
        verify_checksums(arguments.dist, arguments.version)
        print("Checksums match")
    elif command == "plan-upload":
        plan = plan_upload(json.loads(arguments.release.read_text()), arguments.dist, arguments.version)
        for name, action in plan.items():
            print(f"{action} {name}")
    else:  # pragma: no cover - argparse guards this
        raise SystemExit(2)


def parser() -> argparse.ArgumentParser:
    root = argparse.ArgumentParser(description=__doc__)
    sub = root.add_subparsers(dest="command", required=True)
    resolve = sub.add_parser("resolve-tag")
    resolve.add_argument("--tag", required=True)
    check = sub.add_parser("check-release")
    check.add_argument("--tag", required=True)
    check.add_argument("--release", type=Path, required=True)
    check.add_argument("--repo", type=Path, required=True)
    for name in ("verify-dist", "verify-checksums", "plan-upload"):
        child = sub.add_parser(name)
        child.add_argument("--dist", type=Path, required=True)
        child.add_argument("--version", required=True)
        if name == "plan-upload":
            child.add_argument("--release", type=Path, required=True)
    manifest = sub.add_parser("manifest")
    manifest.add_argument("--dist", type=Path, required=True)
    manifest.add_argument("--tag", required=True)
    manifest.add_argument("--commit", required=True)
    manifest.add_argument("--proto-root", type=Path, required=True)
    return root


def main() -> int:
    arguments = parser().parse_args()
    try:
        run(arguments)
    except CheckFailed as failure:
        print(f"::error::{failure}", file=sys.stderr)
        return 1
    except OSError as problem:
        print(f"::error::Could not run: {problem}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())

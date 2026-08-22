#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Verify that every Docker tag a Chronicle release just pushed resolves on Docker Hub.

Multi-architecture manifest pushes can take a short time to propagate through the
registry even after the push step returns successfully. This script expands the
set of tags a Chronicle release must publish and verifies each one with
`docker buildx imagetools inspect`, retrying only the references that are still
missing. Stable releases must resolve the four versioned tags and the four
`latest*` tags; prereleases must resolve only the versioned tags.
"""

import argparse
import re
import subprocess
import sys
import time

REPOSITORY = "cratis/chronicle"
VARIANT_SUFFIXES = ("", "-workbench", "-development", "-development-slim")
DOCKER_TAG_COMPONENT = re.compile(r"^[A-Za-z0-9_][A-Za-z0-9_.-]{0,127}$")
DEFAULT_RETRIES = 6
DEFAULT_DELAY_SECONDS = 10


def docker_tag_component(value):
    """Validate a value that becomes one Docker tag component."""
    if not DOCKER_TAG_COMPONENT.fullmatch(value):
        raise argparse.ArgumentTypeError(
            f"{value!r} is not a valid Docker tag component. "
            "Use only ASCII letters, digits, underscores, periods and dashes, "
            "starting with a letter, digit or underscore."
        )
    return value


def prerelease_flag(value):
    """Parse the workflow's prerelease output into a boolean."""
    normalized = value.strip().lower()
    if normalized in ("", "false", "0", "no"):
        return False
    if normalized in ("true", "1", "yes"):
        return True
    raise argparse.ArgumentTypeError("prerelease must be true or false")


def expected_references(version, prerelease):
    """Return the Docker references a release is required to publish."""
    references = [f"{REPOSITORY}:{version}{suffix}" for suffix in VARIANT_SUFFIXES]
    if not prerelease:
        references.extend(f"{REPOSITORY}:latest{suffix}" for suffix in VARIANT_SUFFIXES)
    return references


def inspect_reference(reference, runner=subprocess.run):
    """Return whether the Docker reference resolves, plus the last CLI message."""
    completed = runner(
        ["docker", "buildx", "imagetools", "inspect", reference],
        capture_output=True,
        text=True,
        check=False,
    )
    output = (completed.stderr or completed.stdout).strip()
    return completed.returncode == 0, output


def verify_references(references, retries=DEFAULT_RETRIES, delay_seconds=DEFAULT_DELAY_SECONDS, inspect=inspect_reference, sleep=time.sleep, log=print):
    """Verify that every reference resolves, retrying only the ones still missing."""
    pending = list(references)
    attempts = retries + 1
    last_errors = {}

    for attempt in range(1, attempts + 1):
        log(f"Verification attempt {attempt}/{attempts}: {len(pending)} reference(s) pending")
        unresolved = []

        for reference in pending:
            resolved, detail = inspect(reference)
            if resolved:
                log(f"Resolved {reference}")
                continue

            unresolved.append(reference)
            last_errors[reference] = detail or "docker buildx imagetools inspect failed"
            log(f"Waiting on {reference}")

        if not unresolved:
            log("All expected Docker references resolved.")
            return

        pending = unresolved
        if attempt < attempts:
            log(f"Sleeping {delay_seconds} second(s) before retrying {len(unresolved)} unresolved reference(s)")
            sleep(delay_seconds)

    details = "\n".join(f"- {reference}: {last_errors[reference]}" for reference in pending)
    raise RuntimeError(
        f"Docker Hub did not resolve every expected Chronicle tag after {attempts} attempt(s):\n{details}"
    )


def main(argv=None):
    """Parse arguments and fail the workflow if any expected tag never appears."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", required=True, type=docker_tag_component, help="Release version tag component")
    parser.add_argument("--prerelease", required=True, type=prerelease_flag, help="Whether the release is a prerelease")
    parser.add_argument("--retries", type=int, default=DEFAULT_RETRIES, help="Additional attempts after the first inspection")
    parser.add_argument("--delay-seconds", type=int, default=DEFAULT_DELAY_SECONDS, help="Seconds to wait between attempts")
    args = parser.parse_args(argv)

    if args.retries < 0:
        parser.error("--retries must be zero or greater")
    if args.delay_seconds < 0:
        parser.error("--delay-seconds must be zero or greater")

    references = expected_references(args.version, args.prerelease)
    print(f"Verifying {len(references)} Docker reference(s) for {args.version}")

    try:
        verify_references(references, retries=args.retries, delay_seconds=args.delay_seconds)
    except RuntimeError as error:
        print(f"::error::{error}")
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())

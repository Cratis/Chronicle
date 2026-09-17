#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Verify that every package a Chronicle release just pushed is actually being served.

A publish step reporting success is not evidence that anything was published. On
2026-09-17 `@cratis/chronicle@6.0.0` printed `+ @cratis/chronicle@6.0.0` with a
sigstore provenance statement and was absent from the registry for hours, and the
release shipped green because nothing checked. See #4050.

Each publish job writes a manifest naming exactly what it pushed, and this script
verifies every entry against the registry it went to. Two checks per package, because
today's failure showed that one is not enough: the version must be *listed* (that is
what a resolver reads) and the artifact must be *downloadable* (that is what an
install fetches). `@cratis/chronicle@6.0.0` spent a window listed but with its tarball
still 404, which an artifact-blind check would have passed.

Manifest lines are `registry<TAB>package<TAB>version`; `#` comments and blank lines are
ignored. Maven packages are `group:artifact`.
"""

import argparse
import json
import os
import sys
import time
import urllib.error
import urllib.request

DEFAULT_RETRIES = 29
DEFAULT_DELAY_SECONDS = 30
DEFAULT_TIMEOUT_SECONDS = 30

NUGET_FLAT_CONTAINER = "https://api.nuget.org/v3-flatcontainer"
NPM_REGISTRY = "https://registry.npmjs.org"
MAVEN_CENTRAL = "https://repo1.maven.org/maven2"
HEX_API = "https://hex.pm/api"
HEX_REPO = "https://repo.hex.pm"


class Check:
    """One assertion about one package: a URL, and what makes the answer acceptable."""

    def __init__(self, description, url, version=None, listing=None):
        self.description = description
        self.url = url
        self.version = version
        self.listing = listing

    def __repr__(self):
        return f"Check({self.description!r}, {self.url!r})"


class Entry:
    """One package a publish job claims to have pushed."""

    def __init__(self, registry, package, version):
        self.registry = registry
        self.package = package
        self.version = version

    @property
    def name(self):
        return f"{self.registry}:{self.package}@{self.version}"

    def __eq__(self, other):
        return (self.registry, self.package, self.version) == (other.registry, other.package, other.version)

    def __hash__(self):
        return hash((self.registry, self.package, self.version))

    def __repr__(self):
        return f"Entry({self.name})"


def parse_manifest(text):
    """Parse manifest text into entries, rejecting a line that is not three fields."""
    entries = []

    for number, raw in enumerate(text.splitlines(), start=1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue

        fields = [field.strip() for field in line.split("\t")]
        if len(fields) != 3 or not all(fields):
            raise ValueError(
                f"Line {number} is not a valid manifest entry: {raw!r}. "
                "Expected three tab-separated fields: registry, package, version."
            )

        registry, package, version = fields
        if registry not in CHECK_BUILDERS:
            raise ValueError(
                f"Line {number} names unknown registry {registry!r}. "
                f"Known registries: {', '.join(sorted(CHECK_BUILDERS))}."
            )

        entries.append(Entry(registry, package, version))

    return entries


def read_manifests(paths, read=None):
    """Read every manifest file and return the de-duplicated entries in first-seen order."""
    reader = read or (lambda path: open(path, encoding="utf-8").read())
    entries = []
    seen = set()

    for path in paths:
        try:
            text = reader(path)
        except OSError as error:
            raise RuntimeError(f"Could not read manifest {path}: {error}") from error

        try:
            parsed = parse_manifest(text)
        except ValueError as error:
            raise RuntimeError(f"{path}: {error}") from error

        for entry in parsed:
            if entry in seen:
                continue
            seen.add(entry)
            entries.append(entry)

    return entries


def nuget_checks(package, version):
    """NuGet resolves from the flat container listing and restores the nupkg."""
    identifier = package.lower()
    normalized = version.lower()
    return [
        Check(
            f"NuGet lists {package} {version}",
            f"{NUGET_FLAT_CONTAINER}/{identifier}/index.json",
            version=normalized,
            listing=lambda document: [str(value).lower() for value in document.get("versions", [])],
        ),
        Check(
            f"NuGet serves the {package} {version} package",
            f"{NUGET_FLAT_CONTAINER}/{identifier}/{normalized}/{identifier}.{normalized}.nupkg",
        ),
    ]


def npm_checks(package, version):
    """npm resolves from the packument and installs the tarball."""
    basename = package.rsplit("/", 1)[-1]
    return [
        Check(
            f"npm lists {package} {version}",
            f"{NPM_REGISTRY}/{package}",
            version=version,
            listing=lambda document: list(document.get("versions", {})),
        ),
        Check(
            f"npm serves the {package} {version} tarball",
            f"{NPM_REGISTRY}/{package}/-/{basename}-{version}.tgz",
        ),
    ]


def maven_checks(package, version):
    """Maven Central resolves the POM and downloads the jar."""
    if ":" not in package:
        raise ValueError(f"Maven package {package!r} must be group:artifact")

    group, artifact = package.split(":", 1)
    base = f"{MAVEN_CENTRAL}/{group.replace('.', '/')}/{artifact}/{version}"
    return [
        Check(f"Maven Central serves the {package} {version} POM", f"{base}/{artifact}-{version}.pom"),
        Check(f"Maven Central serves the {package} {version} jar", f"{base}/{artifact}-{version}.jar"),
    ]


def hex_checks(package, version):
    """Hex resolves from the package releases and fetches the tarball."""
    return [
        Check(
            f"Hex lists {package} {version}",
            f"{HEX_API}/packages/{package}",
            version=version,
            listing=lambda document: [release.get("version") for release in document.get("releases", [])],
        ),
        Check(
            f"Hex serves the {package} {version} tarball",
            f"{HEX_REPO}/tarballs/{package}-{version}.tar",
        ),
    ]


CHECK_BUILDERS = {
    "nuget": nuget_checks,
    "npm": npm_checks,
    "maven": maven_checks,
    "hex": hex_checks,
}


def checks_for(entry):
    """Return every check one manifest entry must pass."""
    return CHECK_BUILDERS[entry.registry](entry.package, entry.version)


def run_check(check, timeout_seconds=DEFAULT_TIMEOUT_SECONDS):
    """Return whether the check passes, plus a message explaining a failure."""
    request = urllib.request.Request(check.url, headers={"User-Agent": "cratis-chronicle-verify-published"})

    try:
        with urllib.request.urlopen(request, timeout=timeout_seconds) as response:
            if check.listing is None:
                return True, ""

            body = response.read()
    except urllib.error.HTTPError as error:
        return False, f"HTTP {error.code} from {check.url}"
    except (urllib.error.URLError, TimeoutError, OSError) as error:
        return False, f"{type(error).__name__} from {check.url}: {error}"

    try:
        document = json.loads(body)
    except json.JSONDecodeError as error:
        return False, f"{check.url} did not return JSON: {error}"

    versions = check.listing(document)
    if check.version in versions:
        return True, ""

    return False, f"{check.url} does not list {check.version}"


def verify_entries(
    entries,
    retries=DEFAULT_RETRIES,
    delay_seconds=DEFAULT_DELAY_SECONDS,
    check_runner=run_check,
    sleep=time.sleep,
    log=print,
):
    """Verify every entry, retrying only the checks that have not passed yet."""
    pending = []
    for entry in entries:
        for check in checks_for(entry):
            pending.append((entry, check))

    total = len(pending)
    attempts = retries + 1
    last_errors = {}

    for attempt in range(1, attempts + 1):
        log(f"Verification attempt {attempt}/{attempts}: {len(pending)} check(s) pending of {total}")
        unresolved = []

        for entry, check in pending:
            passed, detail = check_runner(check)
            if passed:
                log(f"Confirmed {check.description}")
                continue

            unresolved.append((entry, check))
            last_errors[check.description] = detail or "check failed"
            log(f"Waiting on {check.description}")

        if not unresolved:
            log(f"All {total} check(s) passed across {len(entries)} package(s).")
            return

        pending = unresolved
        if attempt < attempts:
            log(f"Sleeping {delay_seconds} second(s) before retrying {len(unresolved)} unresolved check(s)")
            sleep(delay_seconds)

    waited = retries * delay_seconds
    details = "\n".join(f"- {check.description}: {last_errors[check.description]}" for _, check in pending)
    raise RuntimeError(
        f"{len(pending)} of {total} check(s) still failing after {attempts} attempt(s) over {waited} second(s):\n"
        f"{details}\n"
        "The push reported success, so this is either registry propagation that is slower than the budget above "
        "or a publish that did not land. Confirm against the registry before re-publishing - a version that is "
        "merely late is immutable once it appears, and re-publishing it will be refused."
    )


def require_registries(entries, required, log=print):
    """Fail when a registry that was expected to publish contributed no entry.

    Without this the job passes when a manifest never arrives, which is the same
    vacuous green the gate exists to remove.
    """
    present = {entry.registry for entry in entries}
    missing = [registry for registry in required if registry not in present]

    if missing:
        raise RuntimeError(
            f"No published package was reported for: {', '.join(missing)}. "
            "Either the publish job did not run, or it did not write its manifest. "
            "A release cannot be confirmed from a manifest that is absent."
        )

    for registry in sorted(present):
        count = len([entry for entry in entries if entry.registry == registry])
        log(f"{registry}: {count} package(s) reported")


def manifest_paths(directory, walk=os.walk):
    """Return every manifest file under the directory, sorted for stable output."""
    paths = []

    for root, _, files in walk(directory):
        for name in sorted(files):
            if name.endswith(".manifest"):
                paths.append(os.path.join(root, name))

    return sorted(paths)


def main(argv=None):
    """Parse arguments and fail the workflow if any published package is not served."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest-directory", required=True, help="Directory holding the *.manifest files")
    parser.add_argument(
        "--require-registry",
        action="append",
        default=[],
        help="Registry that must contribute at least one package. Repeatable.",
    )
    parser.add_argument("--retries", type=int, default=DEFAULT_RETRIES, help="Additional attempts after the first")
    parser.add_argument("--delay-seconds", type=int, default=DEFAULT_DELAY_SECONDS, help="Seconds between attempts")
    args = parser.parse_args(argv)

    if args.retries < 0:
        parser.error("--retries must be zero or greater")
    if args.delay_seconds < 0:
        parser.error("--delay-seconds must be zero or greater")

    unknown = [registry for registry in args.require_registry if registry not in CHECK_BUILDERS]
    if unknown:
        parser.error(f"--require-registry names unknown registries: {', '.join(unknown)}")

    if not os.path.isdir(args.manifest_directory):
        print(f"::error::Manifest directory {args.manifest_directory} does not exist, so nothing can be verified.")
        return 1

    paths = manifest_paths(args.manifest_directory)
    if not paths:
        print(
            f"::error::No manifests found under {args.manifest_directory}. Every publish job writes one, so an "
            "empty set means the release published nothing or the manifests were never uploaded."
        )
        return 1

    print(f"Reading {len(paths)} manifest(s): {', '.join(os.path.basename(path) for path in paths)}")

    try:
        entries = read_manifests(paths)
        if not entries:
            raise RuntimeError("The manifests are present but name no packages.")

        require_registries(entries, args.require_registry)
        print(f"Verifying {len(entries)} published package(s)")
        verify_entries(entries, retries=args.retries, delay_seconds=args.delay_seconds)
    except RuntimeError as error:
        print(f"::error::{error}")
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())

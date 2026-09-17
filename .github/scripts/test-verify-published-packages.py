#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Specs for verify-published-packages.py.

These pin the two properties that make the gate worth having. It must check the
artifact and not only the listing - `@cratis/chronicle@6.0.0` was listed while its
tarball still returned 404, and an artifact-blind check would have called that
published. And it must refuse to pass when a manifest never arrives, because a
verifier with nothing to verify reports the same green as one that checked
everything. See #4050.

Run with: python3 .github/scripts/test-verify-published-packages.py
"""

import importlib.util
import os
import subprocess
import sys
import tempfile
import unittest

SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
SCRIPT = os.path.join(SCRIPT_DIRECTORY, "verify-published-packages.py")
REPOSITORY_ROOT = os.path.dirname(os.path.dirname(SCRIPT_DIRECTORY))


def _load():
    """Import the verifier, whose filename is not a valid module name."""
    spec = importlib.util.spec_from_file_location("verify_published_packages", SCRIPT)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Could not load module spec for {SCRIPT}")

    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


verifier = _load()


def urls_for(registry, package, version):
    """The URLs the verifier would request for one package."""
    return [check.url for check in verifier.checks_for(verifier.Entry(registry, package, version))]


class for_parsing_a_manifest(unittest.TestCase):
    """The contract between a publish job and the verifier."""

    def test_reads_every_entry_and_ignores_comments_and_blank_lines(self):
        entries = verifier.parse_manifest(
            "# written by dotnet-pack-and-publish\n"
            "nuget\tCratis.Chronicle\t18.4.1\n"
            "\n"
            "npm\t@cratis/chronicle.contracts\t18.4.1\n"
        )

        self.assertEqual(
            [("nuget", "Cratis.Chronicle", "18.4.1"), ("npm", "@cratis/chronicle.contracts", "18.4.1")],
            [(entry.registry, entry.package, entry.version) for entry in entries],
        )

    def test_a_line_that_is_not_three_fields_is_rejected(self):
        with self.assertRaises(ValueError) as error:
            verifier.parse_manifest("nuget\tCratis.Chronicle\n")

        self.assertIn("three tab-separated fields", str(error.exception))

    def test_an_empty_field_is_rejected_rather_than_verified_as_blank(self):
        with self.assertRaises(ValueError):
            verifier.parse_manifest("nuget\t\t18.4.1\n")

    def test_an_unknown_registry_is_rejected_rather_than_skipped(self):
        with self.assertRaises(ValueError) as error:
            verifier.parse_manifest("pypi\tcratis-chronicle\t18.4.1\n")

        self.assertIn("unknown registry", str(error.exception))

    def test_the_same_package_reported_twice_is_verified_once(self):
        entries = verifier.read_manifests(
            ["a", "b"],
            read=lambda _: "nuget\tCratis.Chronicle\t18.4.1\n",
        )

        self.assertEqual(1, len(entries))


class for_the_urls_each_registry_is_checked_on(unittest.TestCase):
    """Both the listing a resolver reads and the artifact an install downloads."""

    def test_nuget_checks_the_listing_and_the_nupkg_in_lower_case(self):
        self.assertEqual(
            [
                "https://api.nuget.org/v3-flatcontainer/cratis.chronicle/index.json",
                "https://api.nuget.org/v3-flatcontainer/cratis.chronicle/18.4.1/cratis.chronicle.18.4.1.nupkg",
            ],
            urls_for("nuget", "Cratis.Chronicle", "18.4.1"),
        )

    def test_npm_checks_the_packument_and_the_tarball_of_a_scoped_package(self):
        self.assertEqual(
            [
                "https://registry.npmjs.org/@cratis/chronicle.contracts",
                "https://registry.npmjs.org/@cratis/chronicle.contracts/-/chronicle.contracts-18.4.1.tgz",
            ],
            urls_for("npm", "@cratis/chronicle.contracts", "18.4.1"),
        )

    def test_maven_checks_the_pom_and_the_jar(self):
        self.assertEqual(
            [
                "https://repo1.maven.org/maven2/io/cratis/chronicle-contracts/18.4.1/chronicle-contracts-18.4.1.pom",
                "https://repo1.maven.org/maven2/io/cratis/chronicle-contracts/18.4.1/chronicle-contracts-18.4.1.jar",
            ],
            urls_for("maven", "io.cratis:chronicle-contracts", "18.4.1"),
        )

    def test_maven_requires_a_group_and_artifact(self):
        with self.assertRaises(ValueError):
            urls_for("maven", "chronicle-contracts", "18.4.1")

    def test_hex_checks_the_releases_and_the_tarball(self):
        self.assertEqual(
            [
                "https://hex.pm/api/packages/cratis_chronicle_contracts",
                "https://repo.hex.pm/tarballs/cratis_chronicle_contracts-18.4.1.tar",
            ],
            urls_for("hex", "cratis_chronicle_contracts", "18.4.1"),
        )


class for_reading_a_listing(unittest.TestCase):
    """A listing that answers 200 without naming the version has not published it."""

    def test_a_packument_served_without_the_version_does_not_count_as_listed(self):
        listing = verifier.checks_for(verifier.Entry("npm", "@cratis/chronicle", "6.0.0"))[0]

        # The packument as it stood while 6.0.0 was absent: 200, but latest still 5.1.0.
        self.assertNotIn("6.0.0", listing.listing({"versions": {"5.1.0": {}}}))
        self.assertIn("6.0.0", listing.listing({"versions": {"5.1.0": {}, "6.0.0": {}}}))

    def test_nuget_listings_compare_case_insensitively(self):
        listing = verifier.checks_for(verifier.Entry("nuget", "Cratis.Chronicle", "18.4.1"))[0]

        self.assertIn("18.4.1", listing.listing({"versions": ["18.4.0", "18.4.1"]}))

    def test_hex_listings_read_the_release_versions(self):
        listing = verifier.checks_for(verifier.Entry("hex", "cratis_chronicle", "3.2.0"))[0]

        self.assertIn("3.2.0", listing.listing({"releases": [{"version": "3.2.0"}, {"version": "3.1.0"}]}))


class for_verifying_entries(unittest.TestCase):
    """Retry behavior, and the failure the gate exists to catch."""

    def test_a_listed_package_whose_artifact_is_missing_fails(self):
        # The exact state @cratis/chronicle@6.0.0 was in: packument served it, tarball 404.
        def check_runner(check):
            if check.listing is not None:
                return True, ""
            return False, f"HTTP 404 from {check.url}"

        with self.assertRaises(RuntimeError) as error:
            verifier.verify_entries(
                [verifier.Entry("npm", "@cratis/chronicle", "6.0.0")],
                retries=0,
                delay_seconds=0,
                check_runner=check_runner,
                sleep=lambda _: None,
                log=lambda *_: None,
            )

        self.assertIn("tarball", str(error.exception))

    def test_retries_only_the_checks_still_failing(self):
        attempted = []
        responses = {
            "https://registry.npmjs.org/@cratis/chronicle.contracts": [(False, "stale"), (True, "")],
            "https://registry.npmjs.org/@cratis/chronicle.contracts/-/chronicle.contracts-18.4.1.tgz": [(True, "")],
        }

        def check_runner(check):
            attempted.append(check.url)
            return responses[check.url].pop(0)

        sleeps = []
        verifier.verify_entries(
            [verifier.Entry("npm", "@cratis/chronicle.contracts", "18.4.1")],
            retries=1,
            delay_seconds=7,
            check_runner=check_runner,
            sleep=sleeps.append,
            log=lambda *_: None,
        )

        self.assertEqual(3, len(attempted))
        self.assertEqual([7], sleeps)

    def test_eventual_success_allows_registry_propagation(self):
        # The listing lands first and the tarball trails it, which is the order observed on npm.
        responses = {
            "https://hex.pm/api/packages/cratis_chronicle": [(False, "not yet"), (True, "")],
            "https://repo.hex.pm/tarballs/cratis_chronicle-3.2.0.tar": [(False, "not yet"), (False, "not yet"), (True, "")],
        }
        sleeps = []

        verifier.verify_entries(
            [verifier.Entry("hex", "cratis_chronicle", "3.2.0")],
            retries=3,
            delay_seconds=2,
            check_runner=lambda check: responses[check.url].pop(0),
            sleep=sleeps.append,
            log=lambda *_: None,
        )

        self.assertEqual([2, 2], sleeps)
        self.assertEqual([[], []], [list(remaining) for remaining in responses.values()])

    def test_permanent_failure_names_every_failing_check_and_warns_against_republishing(self):
        with self.assertRaises(RuntimeError) as error:
            verifier.verify_entries(
                [verifier.Entry("nuget", "Cratis.Chronicle", "18.4.1")],
                retries=1,
                delay_seconds=0,
                check_runner=lambda check: (False, f"HTTP 404 from {check.url}"),
                sleep=lambda _: None,
                log=lambda *_: None,
            )

        message = str(error.exception)
        self.assertIn("NuGet lists Cratis.Chronicle 18.4.1", message)
        self.assertIn("NuGet serves the Cratis.Chronicle 18.4.1 package", message)
        self.assertIn("immutable", message)


class for_requiring_registries(unittest.TestCase):
    """A manifest that never arrives must not pass as nothing to check."""

    def test_a_registry_with_no_reported_package_fails(self):
        with self.assertRaises(RuntimeError) as error:
            verifier.require_registries(
                [verifier.Entry("nuget", "Cratis.Chronicle", "18.4.1")],
                ["nuget", "npm"],
                log=lambda *_: None,
            )

        self.assertIn("npm", str(error.exception))

    def test_every_required_registry_present_passes(self):
        verifier.require_registries(
            [
                verifier.Entry("nuget", "Cratis.Chronicle", "18.4.1"),
                verifier.Entry("npm", "@cratis/chronicle.contracts", "18.4.1"),
            ],
            ["nuget", "npm"],
            log=lambda *_: None,
        )


class for_the_command_line(unittest.TestCase):
    """Input validation and the vacuous-pass guards at the workflow boundary."""

    def _run(self, *arguments):
        return subprocess.run(
            [sys.executable, SCRIPT, *arguments],
            cwd=REPOSITORY_ROOT,
            capture_output=True,
            text=True,
            check=False,
        )

    def test_a_missing_manifest_directory_fails(self):
        completed = self._run("--manifest-directory", os.path.join(tempfile.gettempdir(), "no-such-manifests"))

        self.assertNotEqual(0, completed.returncode)
        self.assertIn("does not exist", completed.stdout)

    def test_an_empty_manifest_directory_fails_rather_than_passing_vacuously(self):
        with tempfile.TemporaryDirectory() as directory:
            completed = self._run("--manifest-directory", directory)

        self.assertNotEqual(0, completed.returncode)
        self.assertIn("No manifests found", completed.stdout)

    def test_a_manifest_naming_no_packages_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            with open(os.path.join(directory, "nuget.manifest"), "w", encoding="utf-8") as file:
                file.write("# nothing was pushed\n")

            completed = self._run("--manifest-directory", directory)

        self.assertNotEqual(0, completed.returncode)
        self.assertIn("name no packages", completed.stdout)

    def test_an_unknown_required_registry_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            completed = self._run("--manifest-directory", directory, "--require-registry", "pypi")

        self.assertNotEqual(0, completed.returncode)
        self.assertIn("unknown registries", completed.stderr)


if __name__ == "__main__":
    unittest.main()

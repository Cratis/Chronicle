#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Specs for verify-published-docker-tags.py.

The publish workflow now hard-fails if Docker Hub never resolves a tag after
push. These specs pin the tag expansion and retry behavior so a future change
cannot silently stop checking one of the image variants or stop failing when a
manifest never appears.

Run with: python3 .github/scripts/test-verify-published-docker-tags.py
"""

import importlib.util
import os
import subprocess
import sys
import unittest

SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
SCRIPT = os.path.join(SCRIPT_DIRECTORY, "verify-published-docker-tags.py")
REPOSITORY_ROOT = os.path.dirname(os.path.dirname(SCRIPT_DIRECTORY))


def _load():
    """Import the verifier, whose filename is not a valid module name."""
    spec = importlib.util.spec_from_file_location("verify_published_docker_tags", SCRIPT)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Could not load module spec for {SCRIPT}")

    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


verifier = _load()


class for_expected_references(unittest.TestCase):
    """The release shapes the verifier must insist on."""

    def test_stable_releases_check_the_four_versioned_and_four_latest_tags(self):
        self.assertEqual(
            [
                "cratis/chronicle:1.2.3",
                "cratis/chronicle:1.2.3-workbench",
                "cratis/chronicle:1.2.3-development",
                "cratis/chronicle:1.2.3-development-slim",
                "cratis/chronicle:latest",
                "cratis/chronicle:latest-workbench",
                "cratis/chronicle:latest-development",
                "cratis/chronicle:latest-development-slim",
            ],
            verifier.expected_references("1.2.3", False),
        )

    def test_prereleases_check_only_the_four_versioned_tags(self):
        self.assertEqual(
            [
                "cratis/chronicle:1.2.3-rc.1",
                "cratis/chronicle:1.2.3-rc.1-workbench",
                "cratis/chronicle:1.2.3-rc.1-development",
                "cratis/chronicle:1.2.3-rc.1-development-slim",
            ],
            verifier.expected_references("1.2.3-rc.1", True),
        )


class for_verifying_references(unittest.TestCase):
    """Retry behavior and failure reporting."""

    def test_retries_only_the_references_still_missing(self):
        references = [
            "cratis/chronicle:1.2.3",
            "cratis/chronicle:1.2.3-workbench",
        ]
        calls = []
        responses = {
            "cratis/chronicle:1.2.3": [(False, "not found"), (True, "")],
            "cratis/chronicle:1.2.3-workbench": [(True, "")],
        }

        def inspect(reference):
            calls.append(reference)
            return responses[reference].pop(0)

        sleeps = []
        verifier.verify_references(
            references,
            retries=1,
            delay_seconds=5,
            inspect=inspect,
            sleep=sleeps.append,
            log=lambda *_: None,
        )

        self.assertEqual(
            [
                "cratis/chronicle:1.2.3",
                "cratis/chronicle:1.2.3-workbench",
                "cratis/chronicle:1.2.3",
            ],
            calls,
        )
        self.assertEqual([5], sleeps)

    def test_eventual_success_allows_short_registry_propagation(self):
        attempts = [
            (False, "missing on attempt one"),
            (False, "missing on attempt two"),
            (True, ""),
        ]
        sleeps = []

        verifier.verify_references(
            ["cratis/chronicle:1.2.3"],
            retries=2,
            delay_seconds=3,
            inspect=lambda _: attempts.pop(0),
            sleep=sleeps.append,
            log=lambda *_: None,
        )

        self.assertEqual([3, 3], sleeps)
        self.assertEqual([], attempts)

    def test_permanent_failure_names_every_missing_reference(self):
        with self.assertRaises(RuntimeError) as error:
            verifier.verify_references(
                [
                    "cratis/chronicle:1.2.3",
                    "cratis/chronicle:1.2.3-workbench",
                ],
                retries=1,
                delay_seconds=0,
                inspect=lambda reference: (False, f"missing {reference}"),
                sleep=lambda _: None,
                log=lambda *_: None,
            )

        message = str(error.exception)
        self.assertIn("cratis/chronicle:1.2.3", message)
        self.assertIn("cratis/chronicle:1.2.3-workbench", message)


class for_the_command_line(unittest.TestCase):
    """Input validation at the workflow boundary."""

    def test_invalid_versions_are_rejected_before_any_registry_call(self):
        completed = subprocess.run(
            [sys.executable, SCRIPT, "--version", "1.2.3+build", "--prerelease", "false"],
            cwd=REPOSITORY_ROOT,
            capture_output=True,
            text=True,
            check=False,
        )

        self.assertNotEqual(0, completed.returncode)
        self.assertIn("not a valid Docker tag component", completed.stderr)


if __name__ == "__main__":
    unittest.main()

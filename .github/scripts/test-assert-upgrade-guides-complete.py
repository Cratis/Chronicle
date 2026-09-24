#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Specs for assert-upgrade-guides-complete.py.

The point of the checker is that it cannot pass vacuously: a guide with no boundaries, or a run
that could not determine the released majors, must not read as a clean result. These pin that,
and pin that a newly released major is reported as missing rather than ignored.

Run with: python3 .github/scripts/test-assert-upgrade-guides-complete.py
"""

import importlib.util
import os
import subprocess
import sys
import tempfile
import unittest

SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
SCRIPT = os.path.join(SCRIPT_DIRECTORY, "assert-upgrade-guides-complete.py")
REPOSITORY_ROOT = os.path.dirname(os.path.dirname(SCRIPT_DIRECTORY))


def _load():
    """Import the checker, whose filename is not a valid module name."""
    spec = importlib.util.spec_from_file_location("assert_upgrade_guides_complete", SCRIPT)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Could not load module spec for {SCRIPT}")

    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


checker = _load()


class for_reading_boundaries(unittest.TestCase):
    """What counts as a documented boundary."""

    def test_reads_every_n_to_m_heading(self):
        text = "### 6 to 7\n\nprose\n\n### 18 to 19\n\nprose\n"

        self.assertEqual({(6, 7), (18, 19)}, checker.boundaries_in(text))

    def test_ignores_headings_that_are_not_boundaries(self):
        text = "### At a glance\n\n### Reading the Touches column\n\n### 6 to 7\n"

        self.assertEqual({(6, 7)}, checker.boundaries_in(text))


class for_reading_majors(unittest.TestCase):
    """Turning released tags into majors."""

    def test_takes_the_major_from_each_release_tag(self):
        self.assertEqual({16, 17, 18}, checker.majors_from_tags(["v16.0.0", "v17.0.1", "v18.5.1"]))

    def test_ignores_anything_that_is_not_a_release_tag(self):
        self.assertEqual({18}, checker.majors_from_tags(["v18.0.0", "nightly", "v18.0", "18.0.0.1"]))

    def test_accepts_tags_with_or_without_the_v_prefix(self):
        self.assertEqual({19}, checker.majors_from_tags(["19.0.0"]))


class for_finding_missing_boundaries(unittest.TestCase):
    """The gap the checker exists to report."""

    def test_a_newly_released_major_with_no_entry_is_reported(self):
        missing = checker.missing_boundaries({18, 19, 20}, {(18, 19)}, oldest=6)

        self.assertEqual([(19, 20)], missing)

    def test_a_fully_documented_history_reports_nothing(self):
        missing = checker.missing_boundaries({6, 7, 8}, {(6, 7), (7, 8)}, oldest=6)

        self.assertEqual([], missing)

    def test_majors_older_than_the_recorded_history_are_not_demanded(self):
        # Majors 1-5 predate the recorded history and must not be reported as gaps.
        missing = checker.missing_boundaries({1, 2, 5, 6, 7}, {(6, 7)}, oldest=6)

        self.assertEqual([], missing)

    def test_a_gap_in_the_middle_is_reported(self):
        missing = checker.missing_boundaries({6, 7, 8, 9}, {(6, 7), (8, 9)}, oldest=6)

        self.assertEqual([(7, 8)], missing)


class for_the_command_line(unittest.TestCase):
    """The vacuous-pass guards at the workflow boundary."""

    def _run(self, *arguments):
        return subprocess.run(
            [sys.executable, SCRIPT, *arguments],
            cwd=REPOSITORY_ROOT,
            capture_output=True,
            text=True,
            check=False,
        )

    def test_a_missing_guide_is_could_not_run_rather_than_clean(self):
        completed = self._run("--guide", os.path.join(tempfile.gettempdir(), "no-such-guide.md"))

        self.assertEqual(2, completed.returncode)
        self.assertIn("does not exist", completed.stdout)

    def test_a_guide_documenting_no_boundary_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            guide = os.path.join(directory, "guide.md")
            with open(guide, "w", encoding="utf-8") as file:
                file.write("# Upgrading\n\nNo boundaries here.\n")

            completed = self._run("--guide", guide, "--majors", "18,19")

        self.assertEqual(1, completed.returncode)
        self.assertIn("documents no", completed.stdout)

    def test_the_repositorys_own_guide_covers_every_released_boundary(self):
        completed = self._run()

        self.assertEqual(0, completed.returncode, completed.stdout + completed.stderr)
        self.assertIn("every boundary", completed.stdout)


if __name__ == "__main__":
    unittest.main()

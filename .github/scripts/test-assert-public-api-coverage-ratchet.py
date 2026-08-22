#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Specs for assert-public-api-coverage-ratchet.py and report-public-api-surface.py.

The ratchet decides whether a shipped public type may exist with no spec touching it, so a silent
change in what it measures is a silent hole in the surface it guards. These specs pin the rules that
actually caused trouble while it was written: that an `internal` type must not be demanded of
consumers, that a type covered only through fluent chaining counts as covered, and - most
importantly - that unreadable input fails loudly instead of reading as zero coverage.

Run with: python3 .github/scripts/test-assert-public-api-coverage-ratchet.py
"""

import importlib.util
import os
import subprocess
import sys
import tempfile
import textwrap
import unittest
from xml.sax.saxutils import escape

SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
RATCHET = os.path.join(SCRIPT_DIRECTORY, "assert-public-api-coverage-ratchet.py")
SURFACE = os.path.join(SCRIPT_DIRECTORY, "report-public-api-surface.py")
REPOSITORY_ROOT = os.path.dirname(os.path.dirname(SCRIPT_DIRECTORY))


def _load(path, name):
    """Imports a check by path, since its filename is not an importable module name."""
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise ImportError(f"Could not load `{path}` - the specs cannot run without it.")

    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


ratchet = _load(RATCHET, "assert_public_api_coverage_ratchet")


def cobertura(classes):
    """Returns a cobertura document covering the given (name, [hits, ...]) pairs.

    Type names are XML-escaped, because a compiler-generated state machine is reported as
    `Type/<Method>d__12` and writing that raw produces a document no parser accepts - the fixture
    would then be testing the malformed-XML path while claiming to test name normalization.
    """
    body = []
    for name, hits in classes:
        lines = "".join(f'<line number="{number}" hits="{hit}" />' for number, hit in enumerate(hits, 1))
        body.append(f'<class name="{escape(name)}" filename="x.cs"><lines>{lines}</lines></class>')

    return f'<?xml version="1.0"?><coverage><packages><package name="p"><classes>{"".join(body)}</classes></package></packages></coverage>'


def write(path, text):
    """Writes a fixture file, reporting a broken environment as such rather than as a failed spec."""
    try:
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w", encoding="utf-8") as file:
            file.write(text)
    except OSError as error:
        raise unittest.SkipTest(f"Could not lay down the fixture at `{path}` ({error}).") from error


def run(classes, baseline, surface, report=None):
    """Runs the ratchet against a synthetic tree and returns its exit code."""
    with tempfile.TemporaryDirectory() as root:
        write(
            os.path.join(root, "coverage-results", "guid", "coverage.cobertura.xml"),
            report if report is not None else cobertura(classes))
        write(os.path.join(root, ".github", "public-api-coverage-baseline.txt"), "\n".join(baseline))
        write(os.path.join(root, ".github", "public-api-surface.txt"), "\n".join(surface))

        original = sys.argv
        sys.argv = ["assert-public-api-coverage-ratchet.py", root]
        try:
            return ratchet.main()
        finally:
            sys.argv = original


PUBLIC = "Cratis.Chronicle.Testing.EventSequences.AppendResultShouldExtensions"
INTERNAL = "Cratis.Chronicle.Testing.EventSequences.InMemoryEventCursor"


class when_a_shipped_public_type_has_no_covered_line(unittest.TestCase):
    """The defect the ratchet exists for."""

    def test_it_fails(self):
        self.assertEqual(run([(PUBLIC, [0, 0, 0])], [], [PUBLIC]), 1)

    def test_a_baselined_type_passes(self):
        self.assertEqual(run([(PUBLIC, [0, 0, 0])], [PUBLIC], [PUBLIC]), 0)

    def test_a_covered_type_passes(self):
        self.assertEqual(run([(PUBLIC, [1, 0, 0])], [], [PUBLIC]), 0)


class when_the_type_is_internal(unittest.TestCase):
    """Cobertura reports `internal` types under the same namespace; consumers cannot call them.

    `InMemoryEventCursor` and `NoOpEventTypesCacheClient` are both `internal sealed` and both appear
    in the real report. Demanding consumer-facing specs for them is the false positive that made a
    namespace-only filter unusable.
    """

    def test_an_uncovered_internal_type_is_ignored(self):
        self.assertEqual(run([(INTERNAL, [0, 0, 0])], [], [PUBLIC]), 0)


class when_coverage_is_reported_under_a_generated_name(unittest.TestCase):
    """A nested state machine or generic arity must collapse onto the declaring type."""

    def test_a_state_machine_counts_towards_its_declaring_type(self):
        self.assertEqual(run([(f"{PUBLIC}/<Handle>d__12", [1, 1])], [], [PUBLIC]), 0)

    def test_generic_arity_is_normalized(self):
        generic = "Cratis.Chronicle.Testing.ReadModels.ReadModelScenario"
        self.assertEqual(run([(f"{generic}`1", [0, 0])], [], [generic]), 1)


class when_the_input_cannot_be_read(unittest.TestCase):
    """Unreadable input is never the same as zero coverage - it must fail, not accuse."""

    def test_a_missing_coverage_directory_fails(self):
        with tempfile.TemporaryDirectory() as root:
            original = sys.argv
            sys.argv = ["assert-public-api-coverage-ratchet.py", root]
            try:
                self.assertEqual(ratchet.main(), 1)
            finally:
                sys.argv = original

    def test_malformed_cobertura_fails(self):
        self.assertEqual(run([], [], [PUBLIC], report="<coverage><not-closed>"), 1)

    def test_a_non_numeric_hits_attribute_fails(self):
        report = f'<?xml version="1.0"?><coverage><packages><package name="p"><classes>' \
                 f'<class name="{PUBLIC}" filename="x.cs"><lines>' \
                 f'<line number="1" hits="lots" /></lines></class></classes></package></packages></coverage>'
        self.assertEqual(run([], [], [PUBLIC], report=report), 1)

    def test_an_empty_surface_fails(self):
        self.assertEqual(run([(PUBLIC, [0])], [], []), 1)

    def test_a_report_naming_no_shipped_type_fails(self):
        self.assertEqual(run([("Some.Other.Assembly.Thing", [0])], [], [PUBLIC]), 1)


class when_reporting_the_public_surface(unittest.TestCase):
    """The surface list is what separates public from internal, so it has to be right and current."""

    def test_it_matches_the_committed_surface(self):
        # A surface that has drifted from source silently stops the ratchet demanding coverage for a
        # newly public type, which is the whole failure this check exists to prevent.
        produced = subprocess.run(
            [sys.executable, SURFACE], cwd=REPOSITORY_ROOT,
            capture_output=True, text=True, check=True).stdout.strip()

        committed = os.path.join(REPOSITORY_ROOT, ".github", "public-api-surface.txt")
        try:
            with open(committed, encoding="utf-8") as file:
                current = file.read().strip()
        except OSError as error:
            self.fail(f"`{committed}` could not be read ({error}) - the ratchet cannot run without it.")

        self.assertEqual(produced, current)

    def test_it_finds_a_public_type_and_skips_an_internal_one(self):
        surface = _load(SURFACE, "report_public_api_surface")
        with tempfile.NamedTemporaryFile("w", suffix=".cs", delete=False, encoding="utf-8") as file:
            file.write(textwrap.dedent("""
                namespace Cratis.Chronicle.Testing.Sample;

                public sealed class VisibleToConsumers { }
                internal sealed class HiddenFromConsumers { }
            """))
            path = file.name

        try:
            found = surface.public_types(path)
            self.assertIn("Cratis.Chronicle.Testing.Sample.VisibleToConsumers", found)
            self.assertNotIn("Cratis.Chronicle.Testing.Sample.HiddenFromConsumers", found)
        finally:
            os.unlink(path)


if __name__ == "__main__":
    unittest.main(verbosity=2)

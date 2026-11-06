#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Specs for the assert-tests-ran action script.

Run with: python3 .github/scripts/test-assert-tests-ran.py
"""

import contextlib
import importlib.util
import io
import os
import sys
import tempfile
import unittest
from unittest.mock import patch

SCRIPT = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "actions", "assert-tests-ran", "assert-tests-ran.py")


def _load():
    spec = importlib.util.spec_from_file_location("assert_tests_ran", SCRIPT)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


# Loading must not leave bytecode beside the action: the runner-contract scan reads every file under .github/actions.
sys.dont_write_bytecode = True
guard = _load()
NS = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"


def _trx(results):
    """Build a TRX from (className, outcome) pairs."""
    definitions = "".join(
        f'<UnitTest id="t{i}"><TestMethod className="{class_name}" name="m{i}" /></UnitTest>'
        for i, (class_name, _) in enumerate(results))
    outcomes = "".join(
        f'<UnitTestResult testId="t{i}" testName="m{i}" outcome="{outcome}" />'
        for i, (_, outcome) in enumerate(results))
    executed = sum(1 for _, outcome in results if outcome != "NotExecuted")
    return (f'<TestRun xmlns="{NS}"><Results>{outcomes}</Results><TestDefinitions>{definitions}</TestDefinitions>'
            f'<ResultSummary><Counters total="{len(results)}" executed="{executed}" /></ResultSummary></TestRun>')


def _run(results_by_file, *extra):
    """Run main() against TRX files and return (exit code, output)."""
    with tempfile.TemporaryDirectory() as directory:
        for index, results in enumerate(results_by_file):
            with open(os.path.join(directory, f"attempt-{index}.trx"), "w", encoding="utf-8") as handle:
                handle.write(_trx(results))
        output = io.StringIO()
        with patch.object(sys, "argv", ["assert-tests-ran.py", directory, "This shard", *extra]), \
                contextlib.redirect_stdout(output):
            code = guard.main()
        return code, output.getvalue()


class for_asserting_tests_ran(unittest.TestCase):
    def test_without_prefixes_any_executed_test_passes(self):
        self.assertEqual(0, _run([[("A.B", "Passed")]])[0])
        self.assertEqual(0, _run([[("A.B", "Passed")]], "")[0])

    def test_without_prefixes_zero_executed_tests_fails(self):
        self.assertEqual(1, _run([[("A.B", "NotExecuted")]])[0])
        self.assertEqual(1, _run([])[0])

    def test_every_prefix_with_an_executed_result_passes(self):
        results = [[("Ns.One.spec", "Passed"), ("Ns.Two.spec", "Failed")]]
        self.assertEqual(0, _run(results, "Ns.One,Ns.Two")[0])

    def test_a_prefix_is_a_namespace_not_a_substring(self):
        code, _ = _run([[("Ns.OneMore.spec", "Passed")]], "Ns.One")
        self.assertEqual(1, code)

    def test_results_in_another_attempt_file_count(self):
        results = [[("Ns.One.spec", "Passed")], [("Ns.Two.spec", "Passed")]]
        self.assertEqual(0, _run(results, "Ns.One,Ns.Two")[0])

    def test_not_executed_results_do_not_count_for_a_member(self):
        code, _ = _run([[("Ns.One.spec", "Passed"), ("Ns.Two.spec", "NotExecuted")]], "Ns.One,Ns.Two")
        self.assertEqual(1, code)

    def test_the_failure_names_the_empty_member(self):
        code, output = _run([[("Ns.One.spec", "Passed")]], "Ns.One, Ns.Two")
        self.assertEqual(1, code)
        self.assertIn("Ns.Two", output)
        self.assertNotIn("executed no test for Ns.One", output)


if __name__ == "__main__":
    unittest.main()

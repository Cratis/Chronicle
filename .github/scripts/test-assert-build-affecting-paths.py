# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Guard central-file coverage on both snippet triggers without weakening the build contract."""

import contextlib
import importlib.util
import io
from pathlib import Path
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).with_name("assert-build-affecting-paths.py")
spec = importlib.util.spec_from_file_location("assert_build_affecting_paths", SCRIPT)
checker = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checker)
ROOT = str(SCRIPT.parents[2])
CENTRAL_FILES = (
    "Directory.Packages.props",
    "Directory.Packages.NET8.props",
    "Directory.Packages.NET9.props",
    "Directory.Build.props",
    "Directory.Build.targets",
    "global.json",
    ".globalconfig",
    ".editorconfig",
)


class BuildAffectingPaths(unittest.TestCase):
    def run_check(self):
        output = io.StringIO()
        with patch.object(checker.sys, "argv", [str(SCRIPT), ROOT]), contextlib.redirect_stdout(output):
            result = checker.main()
        return result, output.getvalue()

    def test_committed_filters_cover_the_contract(self):
        result, output = self.run_check()
        self.assertEqual(0, result, output)
        self.assertIn(f"{checker.SNIPPET_WORKFLOW} (pull_request)", output)
        self.assertIn(f"{checker.SNIPPET_WORKFLOW} (push)", output)

    def test_every_central_file_is_required_on_each_snippet_trigger(self):
        read_paths = checker.read_trigger_paths
        for trigger in ("pull_request", "push"):
            for entry in CENTRAL_FILES:
                with self.subTest(trigger=trigger, entry=entry):
                    def narrowed(root, workflow, event="pull_request"):
                        paths, ignored = read_paths(root, workflow, event)
                        if workflow == checker.SNIPPET_WORKFLOW and event == trigger:
                            paths = [pattern for pattern in paths if pattern != entry]
                        return paths, ignored

                    with patch.object(checker, "read_trigger_paths", side_effect=narrowed):
                        result, output = self.run_check()
                    self.assertEqual(1, result, output)
                    self.assertIn(f"`{entry}` is required to trigger snippet verification", output)

    def test_build_contract_is_still_enforced(self):
        read_paths = checker.read_trigger_paths

        def narrowed(root, workflow, trigger="pull_request"):
            paths, ignored = read_paths(root, workflow, trigger)
            if workflow == checker.WORKFLOW:
                paths = [pattern for pattern in paths if pattern != "Integration/**"]
            return paths, ignored

        with patch.object(checker, "read_trigger_paths", side_effect=narrowed):
            result, output = self.run_check()
        self.assertEqual(1, result, output)
        self.assertIn("`Integration/**` is required to trigger a build and a test run", output)


if __name__ == "__main__":
    unittest.main()
